using System.Security.Cryptography;
using System.Text;
using BtPlatform.Core.Entities;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Sanal ajan: gerçek bilgisayarlardaki ajanın yaptığı işi taklit eder.
// Bekleyen işleri alır, aşama aşama ilerletir, günlük (log) yazar, sonucu veritabanına işler.
public class UpdateAgentSimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateAgentSimulationService> _logger;
    private readonly Random _rng = new();

    // İşin bellekteki planı: sıradaki adım ne zaman, başarısız olacak mı, özet bozuk mu
    private record JobPlan(DateTime NextStepAt, bool WillFail, bool HashMismatch);
    private readonly Dictionary<int, JobPlan> _plans = new();

    public UpdateAgentSimulationService(IServiceScopeFactory scopeFactory, ILogger<UpdateAgentSimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Güncelleme ajanı döngü hatası"); }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // Aynı bağlamda yüklendiği için bilgisayar nesneleri iş ve kalp atışı arasında ortaktır
        var computers = await db.Computers.ToListAsync(ct);
        Heartbeat(computers, now);

        var jobs = await db.UpdateJobs
            .Include(j => j.Computer)
            .Include(j => j.InstalledApp)
            .Where(j => j.Status == UpdateJobStatus.Pending
                     || j.Status == UpdateJobStatus.Downloading
                     || j.Status == UpdateJobStatus.Installing)
            .ToListAsync(ct);

        foreach (var job in jobs) Step(db, job, now);   // YENİ: db parametresi eklendi

        await db.SaveChangesAsync(ct);
    }

    // Bilgisayarlar zaman zaman kapanır/açılır. Aynı anda en fazla %20'si çevrimdışı olur.
    // Bilgisayarlar zaman zaman kapanır/açılır. Aynı anda en fazla %20'si çevrimdışı olur.
    // Gerçek kayıt simülasyona katılmaz: sunucu bu bilgisayarda çalışıyor, o yüzden hep çevrimiçidir.
    private void Heartbeat(List<Computer> computers, DateTime now)
    {
        if (computers.Count == 0) return;

        foreach (var real in computers.Where(c => c.IsReal))
        {
            real.IsOnline = true;
            real.LastSeenAt = now;
        }

        var simulated = computers.Where(c => !c.IsReal).ToList();
        if (simulated.Count == 0) return;

        foreach (var c in simulated.Where(c => c.LastSeenAt == null))
        {
            c.IsOnline = true;
            c.LastSeenAt = now;
        }

        if (_rng.NextDouble() < 0.15)
        {
            var pick = simulated[_rng.Next(simulated.Count)];
            var maxOffline = Math.Max(1, simulated.Count / 5);
            var offlineCount = simulated.Count(c => !c.IsOnline);

            if (pick.IsOnline && offlineCount < maxOffline) pick.IsOnline = false;
            else if (!pick.IsOnline) { pick.IsOnline = true; pick.LastSeenAt = now; }
        }

        foreach (var c in simulated.Where(c => c.IsOnline)) c.LastSeenAt = now;
    }

    private void Step(AppDbContext db, UpdateJob job, DateTime now)   // YENİ: db parametresi eklendi
    {
        // Çevrimdışı bilgisayarda hiçbir şey ilerlemez; açılınca kaldığı yerden devam eder
        if (job.Computer is null || !job.Computer.IsOnline) return;

        // Gerçek cihaz için iş açılmamalı; açılmışsa sahte kurulum yapmadan başarısız say
        if (job.Computer.IsReal)
        {
            Fail(db, job, now, "Gerçek cihazda güncelleme kapalı.", "REAL");
            return;
        }

        switch (job.Status)
        {
            case UpdateJobStatus.Pending:
                job.Status = UpdateJobStatus.Downloading;
                job.StartedAt ??= now;
                Log(job, $"Ajan işi aldı ({job.Hostname}).");
                Log(job, $"Komut: {job.Command}");
                Log(job, $"{job.AppName} {job.ToVersion} yükleyicisi indiriliyor...");
                _plans[job.Id] = new JobPlan(
                    now.AddSeconds(_rng.Next(4, 9)),
                    WillFail: _rng.NextDouble() < 0.10,
                    HashMismatch: _rng.NextDouble() < 0.03);
                break;

            case UpdateJobStatus.Downloading:
                {
                    var plan = GetPlan(job, now);
                    if (now < plan.NextStepAt) return;

                    Log(job, "İndirme tamamlandı.");
                    var computed = plan.HashMismatch ? Hash("tampered|" + job.InstallerSha256) : job.InstallerSha256;
                    Log(job, $"SHA-256 beklenen  : {job.InstallerSha256}");
                    Log(job, $"SHA-256 hesaplanan: {computed}");

                    if (computed != job.InstallerSha256)
                    {
                        Fail(db, job, now, "Yükleyici özeti uyuşmuyor, dosya değiştirilmiş olabilir. Kurulum iptal edildi.", "HASH");
                        return;
                    }

                    Log(job, "Özet doğrulandı. Kurulum başlıyor...");
                    job.Status = UpdateJobStatus.Installing;
                    _plans[job.Id] = plan with { NextStepAt = now.AddSeconds(_rng.Next(4, 9)) };
                    break;
                }

            case UpdateJobStatus.Installing:
                {
                    var plan = GetPlan(job, now);
                    if (now < plan.NextStepAt) return;

                    if (plan.WillFail)
                    {
                        var errors = new[]
                        {
                        ("1603", "Kurulum sırasında ölümcül hata"),
                        ("1618", "Başka bir kurulum zaten devam ediyor"),
                        ("1602", "Kurulum iptal edildi")
                    };
                        var (code, text) = errors[_rng.Next(errors.Length)];
                        Fail(db, job, now, text, code);
                        return;
                    }

                    if (job.InstalledApp is not null)
                    {
                        job.InstalledApp.Version = job.ToVersion;
                        job.InstalledApp.FileHash = Hash($"{job.InstalledApp.Name}|{job.ToVersion}|{job.InstalledApp.Publisher}");
                    }

                    Log(job, "Kurulum tamamlandı. Çıkış kodu: 0");
                    Log(job, $"{job.AppName}: {job.FromVersion} → {job.ToVersion}");
                    job.Status = UpdateJobStatus.Succeeded;
                    job.CompletedAt = now;
                    _plans.Remove(job.Id);

                    ResolveAlerts(db, job, now);   // YENİ: başarı olunca bu bilgisayar+uygulama alarmını kapat
                    break;
                }
        }
    }

    // Sunucu yeniden başlarsa bellekteki plan kaybolur; yarım kalan iş yeni plan alır
    private JobPlan GetPlan(UpdateJob job, DateTime now)
    {
        if (_plans.TryGetValue(job.Id, out var plan)) return plan;
        plan = new JobPlan(now.AddSeconds(2), false, false);
        _plans[job.Id] = plan;
        return plan;
    }

    private void Fail(AppDbContext db, UpdateJob job, DateTime now, string reason, string code)   // YENİ: db parametresi
    {
        Log(job, $"HATA (kod {code}): {reason}");
        job.Status = UpdateJobStatus.Failed;
        job.CompletedAt = now;
        _plans.Remove(job.Id);

        RaiseAlert(db, job, now, reason, code);   // YENİ: alarm aç
    }

    // YENİ: Başarısız işi Alarmlar sayfasına düşürür.
    // Aynı bilgisayar+uygulama için zaten açık alarm varsa ikincisini açmaz (alarm seli olmasın).
    private static void RaiseAlert(AppDbContext db, UpdateJob job, DateTime now, string reason, string code)
    {
        if (job.ComputerId is null || job.InstalledAppId is null) return;

        var alreadyOpen = db.Alerts.Any(a =>
            a.ResolvedAt == null
            && a.UpdateComputerId == job.ComputerId
            && a.UpdateInstalledAppId == job.InstalledAppId);
        if (alreadyOpen) return;

        // Özet uyuşmazlığı olası bir güvenlik sorunudur, bu yüzden Critical
        var isHash = code == "HASH";

        db.Alerts.Add(new Alert
        {
            Title = isHash
                ? $"Güvenlik: yükleyici özeti uyuşmadı ({job.Hostname})"
                : $"Güncelleme başarısız ({job.Hostname})",
            Message = $"{job.AppName} {job.FromVersion} → {job.ToVersion}: {reason} (kod {code}, iş #{job.Id})",
            Severity = isHash ? AlertSeverity.Critical : AlertSeverity.Warning,
            CreatedAt = now,
            UpdateComputerId = job.ComputerId,
            UpdateInstalledAppId = job.InstalledAppId
        });
    }

    // YENİ: Güncelleme başarılı olunca aynı bilgisayar+uygulamanın açık alarmlarını kapatır.
    private static void ResolveAlerts(AppDbContext db, UpdateJob job, DateTime now)
    {
        if (job.ComputerId is null || job.InstalledAppId is null) return;

        var open = db.Alerts
            .Where(a => a.ResolvedAt == null
                     && a.UpdateComputerId == job.ComputerId
                     && a.UpdateInstalledAppId == job.InstalledAppId)
            .ToList();

        foreach (var a in open) a.ResolvedAt = now;
    }

    private static void Log(UpdateJob job, string message)
        => job.Output += $"[{DateTime.Now:HH:mm:ss}] {message}\n";

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}