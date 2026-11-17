using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Gerçek hedefli komutları (yalnızca onaylı, salt okunur) çalıştırır. Aynı anda tek komut.
// RemoteCommands:RealExecutionEnabled=false ise hiçbir şey çalıştırmaz.
public class RealCommandRunnerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRealCommandExecutor _executor;
    private readonly IConfiguration _config;
    private readonly ILogger<RealCommandRunnerService> _logger;

    public RealCommandRunnerService(IServiceScopeFactory scopeFactory, IRealCommandExecutor executor,
        IConfiguration config, ILogger<RealCommandRunnerService> logger)
    {
        _scopeFactory = scopeFactory;
        _executor = executor;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_config.GetValue("RemoteCommands:RealExecutionEnabled", false))
                    await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Gerçek komut çalıştırıcı hatası"); }

            try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        int jobId; string command;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;

            // Sunucu yeniden başladıysa yarım kalan iş TEKRAR ÇALIŞTIRILMAZ, başarısız sayılır
            var stale = await db.RemoteCommandJobs
                .Where(j => j.IsRealTarget && j.Status == RemoteCommandStatus.Running
                            && j.StartedAt != null && j.StartedAt < now.AddMinutes(-2))
                .ToListAsync(ct);
            foreach (var s in stale)
            {
                s.Status = RemoteCommandStatus.Failed;
                s.Output = "Komut kesintiye uğradı (sunucu yeniden başladı), tekrar çalıştırılmadı.";
                s.CompletedAt = now;
            }

            var job = await db.RemoteCommandJobs.Include(j => j.Computer)
                .Where(j => j.IsRealTarget && j.Status == RemoteCommandStatus.Pending)
                .OrderBy(j => j.RequestedAt).FirstOrDefaultAsync(ct);

            if (job is null)
            {
                if (stale.Count > 0) await db.SaveChangesAsync(ct);
                return;
            }

            // Çalıştırmadan hemen önce kuralları tekrar uygula
            string? reject = null;
            if (job.Computer is null || !job.Computer.IsReal) reject = "Hedef artık gerçek kayıt değil.";
            else
            {
                var check = RemoteCommandValidator.ValidateFree(job.CommandText, isRealTarget: true);
                if (!check.Ok) reject = "Reddedildi: " + check.Error;
            }

            if (reject is not null)
            {
                job.Status = RemoteCommandStatus.Failed;
                job.Output = reject;
                job.CompletedAt = now;
                await db.SaveChangesAsync(ct);
                return;
            }

            job.Status = RemoteCommandStatus.Running;
            job.StartedAt = now;
            await db.SaveChangesAsync(ct);
            jobId = job.Id; command = job.CommandText;
        }

        // Çalıştırma sırasında veritabanı bağlantısı açık değil
        var result = await _executor.ExecuteAsync(command, ct);

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = await db.RemoteCommandJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
            if (job is null) return;
            job.Output = "[GERÇEK ÇALIŞTIRMA: komut bu bilgisayarda çalıştırıldı]\n" + result.Output;
            job.Status = result.Ok ? RemoteCommandStatus.Succeeded : RemoteCommandStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}