using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Gerçek hayatta burada SNMP ile switch'lerden veri okunur (polling).
// Biz değerleri rastgele değiştirerek aynı davranışı simüle ediyoruz.
public class SwitchSimulationService : BackgroundService
{
    private const string CpuTitle = "Yüksek CPU";
    private const string PortDownTitle = "Port bağlantısı koptu";
    private const string ErrDisabledTitle = "Port err-disabled";
    private const string ConfigTitle = "Yapılandırma değişti";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SwitchSimulationService> _log;
    private readonly Random _rnd = new();

    public SwitchSimulationService(IServiceScopeFactory scopeFactory,
                                   ILogger<SwitchSimulationService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), ct);   // seed bitsin
        while (!ct.IsCancellationRequested)
        {
            try { await PollOnce(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Switch simülasyonu hatası");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task PollOnce(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var switches = await db.Switches.Include(s => s.Ports).ToListAsync(ct);
        var openAlerts = await db.Alerts.Where(a => a.ResolvedAt == null).ToListAsync(ct);

        foreach (var sw in switches)
        {
            // --- CPU ve bellek ---
            if (_rnd.Next(100) < 3) sw.CpuPercent = _rnd.Next(85, 99);          // ani yükselme
            else if (sw.CpuPercent > 70) sw.CpuPercent -= _rnd.Next(5, 16);     // sakinleşme
            else sw.CpuPercent = Math.Clamp(sw.CpuPercent + _rnd.Next(-4, 5), 3, 70);
            sw.MemoryPercent = Math.Clamp(sw.MemoryPercent + _rnd.Next(-2, 3), 20, 90);
            sw.LastPolledAt = now;

            var cpuAlert = openAlerts.FirstOrDefault(a => a.SwitchId == sw.Id && a.Title == CpuTitle);
            if (sw.CpuPercent >= 85 && cpuAlert is null)
                AddAlert(db, sw, CpuTitle, $"{sw.Name} CPU kullanımı %{sw.CpuPercent}", AlertSeverity.Critical);
            else if (sw.CpuPercent < 60 && cpuAlert is not null)
                cpuAlert.ResolvedAt = now;

            // --- Portlar ---
            foreach (var p in sw.Ports)
            {
                var uplink = p.Number >= 23;
                var max = uplink ? 3000.0 : 300.0;

                if (p.Status == PortStatus.Up)
                {
                    p.TrafficInMbps = Walk(p.TrafficInMbps, max);
                    p.TrafficOutMbps = Walk(p.TrafficOutMbps, max);

                    var roll = _rnd.Next(10000);
                    if (!uplink && roll < 8)
                    {
                        p.Status = PortStatus.Down;
                        p.TrafficInMbps = p.TrafficOutMbps = 0;
                        AddAlert(db, sw, PortDownTitle, $"{sw.Name} Gi1/0/{p.Number} link down", AlertSeverity.Warning);
                    }
                    else if (!uplink && roll < 10)
                    {
                        p.Status = PortStatus.ErrDisabled;
                        p.TrafficInMbps = p.TrafficOutMbps = 0;
                        AddAlert(db, sw, ErrDisabledTitle, $"{sw.Name} Gi1/0/{p.Number} err-disabled", AlertSeverity.Critical);
                    }
                }
                else if (p.Status == PortStatus.Down && _rnd.Next(100) < 10)
                {
                    p.Status = PortStatus.Up;       // kablo takıldı, link geri geldi
                    var prefix = $"{sw.Name} Gi1/0/{p.Number} ";
                    foreach (var a in openAlerts.Where(a => a.SwitchId == sw.Id && a.Message.StartsWith(prefix)))
                        a.ResolvedAt = now;
                }
                // ErrDisabled: gerçekte olduğu gibi elle müdahale gerekir (switch sayfasındaki buton)
            }

            // --- Yapılandırma değişikliği tespiti (SHA-256) ---
            var actualHash = Sha256Hasher.Compute(sw.ConfigText);
            if (actualHash != sw.ConfigHash)
            {
                AddAlert(db, sw, ConfigTitle,
                    $"{sw.Name} yapılandırması değişti (eski {sw.ConfigHash[..8]}… yeni {actualHash[..8]}…)",
                    AlertSeverity.Warning);
                sw.ConfigHash = actualHash;
                sw.ConfigChangedAt = now;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private double Walk(double current, double max) =>
        Math.Round(Math.Clamp(current + (_rnd.NextDouble() - 0.5) * max * 0.3, 0, max), 1);

    private static void AddAlert(AppDbContext db, NetworkSwitch sw, string title, string message, AlertSeverity sev) =>
        db.Alerts.Add(new Alert { Title = title, Message = message, Severity = sev, SwitchId = sw.Id });
}
