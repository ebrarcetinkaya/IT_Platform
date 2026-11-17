using System.Text.RegularExpressions;

namespace BtPlatform.Core.Services;

public record CatalogCommand(string Key, string Title, string Description, string Template,
    string? ParameterName, bool SimulatedOnly);

public record CommandBuildResult(string? Command, string? Error)
{
    public bool Ok => Command is not null;
}

// Hazır komut kütüphanesi (runbook). Komut serbest metin değil, şablondan üretilir;
// parametre varsa sıkı doğrulanır. Güncelleme sistemindeki "şablon komut" mantığının aynısı.
public static class RemoteCommandCatalog
{
    private static readonly Regex TargetPattern = new(@"^[A-Za-z0-9][A-Za-z0-9.\-]{0,62}$", RegexOptions.Compiled);

    public static readonly IReadOnlyList<CatalogCommand> All = new List<CatalogCommand>
    {
        new("processes", "Çalışan süreçleri listele", "CPU'ya göre ilk 15 süreç",
            "Get-Process | Sort-Object CPU -Descending | Select-Object -First 15 Name, Id, CPU", null, false),
        new("services", "Çalışan servisleri listele", "Çalışır durumdaki ilk 30 servis",
            "Get-Service | Where-Object Status -eq 'Running' | Select-Object -First 30 Name, Status", null, false),
        new("hotfix", "Yüklü güncellemeler", "Son 15 Windows güncellemesi",
            "Get-HotFix | Sort-Object InstalledOn -Descending | Select-Object -First 15 HotFixID, Description, InstalledOn",
            null, false),
        new("ipconfig", "Ağ ayarları", "IP yapılandırması",
            "ipconfig /all", null, false),
        new("ping", "Ağ testi", "Hedefe 2 deneme (hedef adı ya da IP)",
            "Test-Connection -ComputerName {target} -Count 2", "target", false),
        new("disk", "Disk özeti", "Sürücü boyutu ve boş alan",
            "Get-CimInstance Win32_LogicalDisk -Filter 'DriveType=3' | Select-Object DeviceID, Size, FreeSpace", null, false),
        new("memory", "Bellek özeti", "Toplam ve boş fiziksel bellek (KB)",
            "Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory", null, false),
        new("gpupdate", "Grup ilkesini güncelle", "Yalnızca simüle bilgisayarlarda",
            "gpupdate /force", null, true),
        new("spooler", "Yazıcı servisini yeniden başlat", "Yalnızca simüle bilgisayarlarda",
            "Restart-Service Spooler", null, true),
    };

    public static CatalogCommand? Find(string? key) =>
        All.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    public static CommandBuildResult Build(string? key, string? parameter, bool isRealTarget)
    {
        var item = Find(key);
        if (item is null) return new CommandBuildResult(null, "Katalogda böyle bir komut yok.");
        if (item.SimulatedOnly && isRealTarget)
            return new CommandBuildResult(null, "Bu komut gerçek makinede kapalı (yalnızca simüle bilgisayarlarda).");

        var command = item.Template;
        if (item.ParameterName is not null)
        {
            var value = (parameter ?? "").Trim();
            if (!TargetPattern.IsMatch(value))
                return new CommandBuildResult(null, "Hedef geçersiz: yalnızca harf, rakam, nokta ve tire (en fazla 63 karakter).");
            command = command.Replace("{" + item.ParameterName + "}", value);
        }

        return new CommandBuildResult(command, null);
    }
}