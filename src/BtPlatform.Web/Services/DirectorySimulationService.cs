using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Gerçek hayatta bu bilgi AD'nin güvenlik olay günlüğünden (örn. 4740 "hesap kilitlendi")
// ya da bir izleme aracından gelir. Biz rastgele "yanlış parola" olayları üretiyoruz.
public class DirectorySimulationService : BackgroundService
{
    private const int LockThreshold = 5;      // AD'deki "hesap kilitleme eşiği"
    private const int MaxLockedAtOnce = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DirectorySimulationService> _log;
    private readonly Random _rnd = new();

    public DirectorySimulationService(IServiceScopeFactory scopeFactory,
                                      ILogger<DirectorySimulationService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(8), ct);   // seed bitsin
        while (!ct.IsCancellationRequested)
        {
            try { await Tick(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "AD simülasyonu hatası");
            }
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }

    private async Task Tick(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var users = await db.DirectoryUsers.Where(u => u.IsEnabled && !u.IsLocked).ToListAsync(ct);
        var lockedCount = await db.DirectoryUsers.CountAsync(u => u.IsLocked, ct);

        // Hatalı denemesi olan kullanıcılar zamanla doğru parolayı girer, sayaç sıfırlanır
        foreach (var u in users.Where(u => u.FailedLogons > 0))
        {
            if (_rnd.Next(100) < 25)
            {
                u.FailedLogons = 0;
                u.LastLogonAt = now;
            }
        }

        // Ara sıra bir kullanıcı art arda yanlış parola girer (Caps Lock açık, parola değişmiş...)
        if (users.Count > 0 && lockedCount < MaxLockedAtOnce && _rnd.Next(100) < 4)
        {
            var user = users[_rnd.Next(users.Count)];
            user.FailedLogons += _rnd.Next(3, 7);

            if (user.FailedLogons >= LockThreshold)
            {
                user.IsLocked = true;
                user.LockedAt = now;
                db.Alerts.Add(new Alert
                {
                    Title = DirectoryConstants.LockedTitle,
                    Message = DirectoryConstants.LockedMessage(user.Username),
                    Severity = AlertSeverity.Warning
                    // SwitchId boş: bu alarm bir switch'e değil dizin hesabına ait
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}