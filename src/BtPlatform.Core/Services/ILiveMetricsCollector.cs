using System.Text.Json;

namespace BtPlatform.Core.Services;

public record LiveMetrics(double CpuPercent, double RamTotalGb, double RamUsedGb,
    double DiskTotalGb, double DiskFreeGb, double UptimeHours);

public record LiveMetricsResult(LiveMetrics? Metrics, string? Error)
{
    public bool Ok => Metrics is not null;
    public static LiveMetricsResult Success(LiveMetrics m) => new(m, null);
    public static LiveMetricsResult Fail(string error) => new(null, error);
}

public interface ILiveMetricsCollector
{
    // Çalıştığı bilgisayarın anlık ölçümlerini SALT OKUNUR toplar.
    Task<LiveMetricsResult> CollectAsync(CancellationToken ct = default);
}

// Toplayıcının JSON çıktısını doğrular. Eksik, mantıksız ya da aşırı büyük çıktı reddedilir.
public static class LiveMetricsParser
{
    public const int MaxJsonChars = 4 * 1024;

    public static LiveMetricsResult Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return LiveMetricsResult.Fail("Ölçüm betiği boş çıktı verdi.");
        if (json.Length > MaxJsonChars) return LiveMetricsResult.Fail("Ölçüm çıktısı çok büyük, reddedildi.");
        json = json.Trim().TrimStart('\uFEFF');

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return LiveMetricsResult.Fail("Ölçüm çıktısı beklenen biçimde değil.");

            if (!Num(root, "cpuPercent", out var cpu) || !Num(root, "ramTotalGb", out var ramTotal)
                || !Num(root, "ramUsedGb", out var ramUsed) || !Num(root, "diskTotalGb", out var diskTotal)
                || !Num(root, "diskFreeGb", out var diskFree) || !Num(root, "uptimeHours", out var uptime))
                return LiveMetricsResult.Fail("Ölçüm çıktısında eksik ya da sayı olmayan alan var.");

            if (cpu < 0 || cpu > 100) return LiveMetricsResult.Fail("CPU değeri geçersiz.");
            if (ramTotal <= 0 || ramUsed < 0 || ramUsed > ramTotal + 0.1) return LiveMetricsResult.Fail("RAM değerleri geçersiz.");
            if (diskTotal <= 0 || diskFree < 0 || diskFree > diskTotal + 0.1) return LiveMetricsResult.Fail("Disk değerleri geçersiz.");
            if (uptime < 0 || uptime > 100_000) return LiveMetricsResult.Fail("Çalışma süresi geçersiz.");

            return LiveMetricsResult.Success(new LiveMetrics(cpu, ramTotal, ramUsed, diskTotal, diskFree, uptime));
        }
        catch (JsonException)
        {
            return LiveMetricsResult.Fail("Ölçüm çıktısı okunamadı (geçersiz JSON).");
        }
    }

    private static bool Num(JsonElement e, string name, out double value)
    {
        value = 0;
        return e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out value);
    }
}