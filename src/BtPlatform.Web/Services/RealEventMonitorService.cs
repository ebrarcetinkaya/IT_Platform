using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Gerçek bilgisayarın olay günlüğünü SALT OKUNUR toplar (10 dk). Gerçek kayıt yoksa hiçbir şey çalıştırmaz.
public class RealEventMonitorService : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(10);
    private const int KeepPerComputer = 500;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventCollector _collector;
    private readonly ILogger<RealEventMonitorService> _logger;
    private DateTime _lastRun = DateTime.MinValue;

    public RealEventMonitorService(IServiceScopeFactory scopeFactory, IEventCollector collector,
        ILogger<RealEventMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _collector = collector;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow - _lastRun >= Every)
                    await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Olay izleme döngü hatası"); }

            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        // 1) Gerçek kayıt var mı?
        int id; string host;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var real = await db.Computers.AsNoTracking().Where(c => c.IsReal)
                .Select(c => new { c.Id, c.Hostname }).FirstOrDefaultAsync(ct);
            if (real is null) return;   // gerçek kayıt yok: hiçbir şey çalıştırma
            id = real.Id; host = real.Hostname;
        }

        // 2) Toplama (veritabanı bağlantısı açık değil)
        _lastRun = DateTime.UtcNow;
        var result = await _collector.CollectAsync(ct);
        if (!result.Ok)
        {
            _logger.LogWarning("Olay toplama başarısız: {Error}", result.Error);
            return;
        }
        if (result.Events.Count == 0) return;

        // 3) Yaz
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Computers.AnyAsync(c => c.Id == id && c.IsReal, ct)) return;

            var keys = result.Events.Select(e => e.RecordId).Distinct().ToList();
            var existing = (await db.PcEvents.Where(p => p.ComputerId == id && keys.Contains(p.RecordId))
                .Select(p => new { p.LogName, p.RecordId }).ToListAsync(ct))
                .Select(p => (p.LogName, p.RecordId)).ToHashSet();

            var added = new List<PcEvent>();
            foreach (var e in result.Events)
            {
                if (existing.Contains((e.LogName, e.RecordId))) continue;
                if (added.Any(a => a.LogName == e.LogName && a.RecordId == e.RecordId)) continue;
                added.Add(new PcEvent
                {
                    ComputerId = id,
                    LogName = e.LogName,
                    Level = e.Level,
                    EventId = e.EventId,
                    Source = e.Source,
                    Message = e.Message,
                    OccurredAt = e.OccurredAtUtc,
                    RecordId = e.RecordId
                });
            }

            if (added.Count > 0)
            {
                db.PcEvents.AddRange(added);

                // Critical olay: alarm (aynı olay için tekrar açılmaz)
                foreach (var c in added.Where(a => a.Level == PcEventLevel.Critical))
                {
                    var title = $"Kritik olay ({host})";
                    var message = $"{c.LogName} {c.Source} olay {c.EventId} (kayıt {c.RecordId}): {Cut(c.Message, 200)}";
                    var marker = $"kayıt {c.RecordId}):";
                    var dup = await db.Alerts.AnyAsync(a => a.Title == title && a.Message.Contains(marker), ct);
                    if (dup) continue;
                    db.Alerts.Add(new Alert
                    {
                        Title = title,
                        Message = message,
                        Severity = AlertSeverity.Critical,
                        CreatedAt = DateTime.UtcNow,
                        UpdateComputerId = id
                    });
                }

                await db.SaveChangesAsync(ct);
                _logger.LogInformation("{Count} yeni olay kaydedildi ({Host})", added.Count, host);
            }

            // Sınır: en yeni 500 olay kalır
            var total = await db.PcEvents.CountAsync(p => p.ComputerId == id, ct);
            if (total > KeepPerComputer)
            {
                var cutoff = await db.PcEvents.Where(p => p.ComputerId == id)
                    .OrderByDescending(p => p.OccurredAt).Skip(KeepPerComputer - 1)
                    .Select(p => p.OccurredAt).FirstAsync(ct);
                await db.PcEvents.Where(p => p.ComputerId == id && p.OccurredAt < cutoff).ExecuteDeleteAsync(ct);
            }
        }
    }

    private static string Cut(string s, int max) => s.Length > max ? s[..max] : s;
}