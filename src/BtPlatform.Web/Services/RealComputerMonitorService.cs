using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Gerçek bilgisayar kaydı için SALT OKUNUR izleme: canlı ölçüm (3 dk) ve winget taraması (30 dk).
// Gerçek kayıt yoksa hiçbir şey çalıştırmaz. Hiçbir uygulamayı kurmaz ya da güncellemez.
public class RealComputerMonitorService : BackgroundService
{
    private static readonly TimeSpan MetricsEvery = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ScanEvery = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILiveMetricsCollector _metrics;
    private readonly IWingetScanner _scanner;
    private readonly ILogger<RealComputerMonitorService> _logger;

    public RealComputerMonitorService(IServiceScopeFactory scopeFactory, ILiveMetricsCollector metrics,
        IWingetScanner scanner, ILogger<RealComputerMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _scanner = scanner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }   // açılış yavaşlamasın
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Gerçek bilgisayar izleme döngü hatası"); }

            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        // 1) Kısa kapsam: gerçek kayıt var mı, ne zaman ölçüldü/tarandı?
        int id; DateTime? metricsAt, scanAt;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var real = await db.Computers.AsNoTracking().Where(c => c.IsReal)
                .Select(c => new { c.Id, c.MetricsAt, c.ScanAt }).FirstOrDefaultAsync(ct);
            if (real is null) return;   // gerçek kayıt yok: hiçbir şey çalıştırma
            id = real.Id; metricsAt = real.MetricsAt; scanAt = real.ScanAt;
        }

        var now = DateTime.UtcNow;

        // 2) Toplama (veritabanı bağlantısı açık değil)
        LiveMetricsResult? m = null;
        if (metricsAt is null || now - metricsAt >= MetricsEvery)
            m = await _metrics.CollectAsync(ct);

        WingetScanResult? s = null;
        if (scanAt is null || now - scanAt >= ScanEvery)
            s = await _scanner.ScanAsync(ct);

        if (m is null && s is null) return;

        // 3) Kısa kapsam: sonuçları yaz
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pc = await db.Computers.FirstOrDefaultAsync(c => c.Id == id && c.IsReal, ct);
            if (pc is null) return;   // toplarken kayıt silinmiş olabilir

            if (m is not null)
            {
                if (m.Ok)
                {
                    var x = m.Metrics!;
                    pc.CpuPercent = x.CpuPercent;
                    pc.RamTotalGb = x.RamTotalGb;
                    pc.RamUsedGb = x.RamUsedGb;
                    pc.DiskTotalGb = x.DiskTotalGb;
                    pc.DiskFreeGb = x.DiskFreeGb;
                    pc.UptimeHours = x.UptimeHours;
                    pc.MetricsAt = DateTime.UtcNow;
                }
                else
                {
                    _logger.LogWarning("Canlı ölçüm başarısız: {Error}", m.Error);
                    pc.MetricsAt = DateTime.UtcNow;   // hata durumunda da 3 dk bekle, sürekli deneme
                }
            }

            if (s is not null)
            {
                pc.ScanAt = DateTime.UtcNow;
                if (s.Ok)
                {
                    pc.ScanError = null;
                    var old = await db.RealUpgradeFindings.Where(f => f.ComputerId == id).ToListAsync(ct);
                    db.RealUpgradeFindings.RemoveRange(old);
                    db.RealUpgradeFindings.AddRange(s.Items.Select(i => new RealUpgradeFinding
                    {
                        ComputerId = id,
                        Name = i.Name,
                        PackageId = i.Id,
                        CurrentVersion = i.CurrentVersion,
                        AvailableVersion = i.AvailableVersion,
                        Source = i.Source,
                        FoundAt = DateTime.UtcNow
                    }));
                }
                else
                {
                    // Hata: eski bulgular kalır, sadece hata metni yazılır
                    pc.ScanError = s.Error;
                    _logger.LogWarning("Winget taraması başarısız: {Error}", s.Error);
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}