using System.Net;
using System.Text.Json;
using BtPlatform.Core.Security;

namespace BtPlatform.Core.Services;

// Toplayıcının ürettiği JSON'u doğrular ve InventorySnapshot'a çevirir.
// Toplayıcıya güvenmeyiz: eksik, bozuk ya da aşırı büyük çıktı reddedilir.
public static class InventoryJsonParser
{
    public const int MaxApps = 15;
    public const int MaxJsonChars = 256 * 1024;

    public static InventoryResult Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return InventoryResult.Fail("Toplayıcı boş çıktı verdi.");
        if (json.Length > MaxJsonChars)
            return InventoryResult.Fail("Toplayıcı çıktısı çok büyük, reddedildi.");

        json = json.Trim().TrimStart('\uFEFF');   // bazı araçlar başa görünmez BOM karakteri koyar

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return InventoryResult.Fail("Çıktı beklenen biçimde değil.");

            var hostname = Str(root, "hostname", 63);
            var osName = Str(root, "osName", 100);
            var ip = Str(root, "ipAddress", 45);
            var mac = Str(root, "macAddress", 17);

            if (hostname == "") return InventoryResult.Fail("Bilgisayar adı (hostname) okunamadı.");
            if (osName == "") return InventoryResult.Fail("İşletim sistemi adı okunamadı.");
            if (!IPAddress.TryParse(ip, out _)) return InventoryResult.Fail("IP adresi geçersiz.");
            if (HmacHasher.NormalizeMac(mac).Length != 12) return InventoryResult.Fail("MAC adresi geçersiz.");

            var ram = Math.Round(Num(root, "ramGb"));
            var disk = Math.Round(Num(root, "diskGb"));
            if (ram < 1 || ram > 65536) return InventoryResult.Fail("RAM değeri geçersiz.");
            if (disk < 1 || disk > 1_000_000) return InventoryResult.Fail("Disk değeri geçersiz.");

            var apps = ParseApps(root);

            return InventoryResult.Success(new InventorySnapshot(
                hostname,
                Str(root, "manufacturer", 100),
                Str(root, "model", 100),
                Str(root, "serialNumber", 100),
                Str(root, "processor", 150),
                (int)ram, (int)disk,
                osName, ip, mac,
                apps));
        }
        catch (JsonException)
        {
            return InventoryResult.Fail("Toplayıcı çıktısı okunamadı (geçersiz JSON).");
        }
    }

    private static List<InventoryApp> ParseApps(JsonElement root)
    {
        var items = new List<JsonElement>();
        if (root.TryGetProperty("apps", out var appsEl))
        {
            // PowerShell tek kayıtta diziyi açıp düz nesne yazabilir; ikisini de kabul ederiz
            if (appsEl.ValueKind == JsonValueKind.Array) items.AddRange(appsEl.EnumerateArray());
            else if (appsEl.ValueKind == JsonValueKind.Object) items.Add(appsEl);
        }

        var apps = new List<InventoryApp>();
        foreach (var it in items)
        {
            var name = Str(it, "name", 100);
            if (name == "") continue;   // adsız kayıt işe yaramaz
            apps.Add(new InventoryApp(name, Str(it, "version", 50), Str(it, "publisher", 100)));
        }

        // Aynı ad bir kez (katalog ada göre eşleşiyor), ada göre sıralı, ilk 15
        return apps
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxApps)
            .ToList();
    }

    private static string Str(JsonElement e, string name, int max)
    {
        if (e.ValueKind != JsonValueKind.Object
            || !e.TryGetProperty(name, out var p)
            || p.ValueKind != JsonValueKind.String) return "";
        var s = (p.GetString() ?? "").Trim();
        return s.Length > max ? s[..max] : s;
    }

    private static double Num(JsonElement e, string name)
    {
        if (e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var d))
            return d;
        return 0;
    }
}