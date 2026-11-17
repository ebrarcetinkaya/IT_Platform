using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BtPlatform.Infrastructure.Services;

public class UpdateService : IUpdateService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task<T> WithDb<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public Task<List<AppUpdateInfo>> GetAppUpdatesAsync(int computerId) => WithDb(async db =>
    {
        var apps = await db.InstalledApps.AsNoTracking()
            .Where(a => a.ComputerId == computerId).OrderBy(a => a.Name).ToListAsync();
        var catalog = await db.SoftwareCatalog.AsNoTracking().ToDictionaryAsync(c => c.Name);

        // YENİ (14c): bu bilgisayar gerçek makineden mi toplandı?
        var isReal = await db.Computers.AsNoTracking()
            .Where(c => c.Id == computerId).Select(c => c.IsReal).FirstOrDefaultAsync();

        var activeIds = (await db.UpdateJobs.AsNoTracking()
                .Where(j => j.ComputerId == computerId
                            && j.Status != UpdateJobStatus.Succeeded && j.Status != UpdateJobStatus.Failed)
                .Select(j => j.InstalledAppId).ToListAsync())
            .Where(id => id != null).Select(id => id!.Value).ToHashSet();

        return apps.Select(a =>
        {
            catalog.TryGetValue(a.Name, out var item);
            // YENİ (14c): dördüncü parametre isReal
            return new AppUpdateInfo(a, item, UpdateRules.Evaluate(a, item, activeIds.Contains(a.Id), isReal));
        }).ToList();
    });

    // Departman listesi (filtre açılır menüsü için)
    public Task<List<string>> GetDepartmentsAsync() => WithDb(db =>
        db.Computers.AsNoTracking()
            .Select(c => c.Department).Distinct().OrderBy(d => d).ToListAsync());

    // Tüm bilgisayarlardaki uygulamaların güncelleme durumu (isteğe bağlı departman filtresiyle)
    public Task<List<ComputerAppUpdate>> GetSoftwareOverviewAsync(string? department) => WithDb(async db =>
    {
        var query = db.InstalledApps.AsNoTracking().Include(a => a.Computer).AsQueryable();
        if (!string.IsNullOrEmpty(department))
            query = query.Where(a => a.Computer!.Department == department);

        var apps = await query.ToListAsync();
        var catalog = await db.SoftwareCatalog.AsNoTracking().ToDictionaryAsync(c => c.Name);

        // Aktif işi olan uygulamalar (aynı uygulama için ikinci iş açılmaz kuralı)
        var activeIds = (await db.UpdateJobs.AsNoTracking()
                .Where(j => j.Status != UpdateJobStatus.Succeeded && j.Status != UpdateJobStatus.Failed)
                .Select(j => j.InstalledAppId).ToListAsync())
            .Where(id => id != null).Select(id => id!.Value).ToHashSet();

        return apps
            .Where(a => a.Computer is not null)
            .OrderBy(a => a.Computer!.Hostname).ThenBy(a => a.Name)
            .Select(a =>
            {
                catalog.TryGetValue(a.Name, out var item);
                // YENİ (14c): dördüncü parametre a.Computer!.IsReal
                var info = new AppUpdateInfo(a, item,
                    UpdateRules.Evaluate(a, item, activeIds.Contains(a.Id), a.Computer!.IsReal));
                return new ComputerAppUpdate(a.Computer!, info);
            }).ToList();
    });

    public Task<UpdateRequestResult> CreateJobsAsync(string requestedBy, IReadOnlyCollection<int> installedAppIds)
        => WithDb(async db =>
    {
        var ids = installedAppIds.Distinct().ToList();

        // Seçimi İSTEMCİYE GÜVENMEDEN yeniden doğruluyoruz: her uygulama için kurallar tekrar çalışır
        var apps = await db.InstalledApps.Include(a => a.Computer)
            .Where(a => ids.Contains(a.Id)).ToListAsync();
        var catalog = await db.SoftwareCatalog.AsNoTracking().ToDictionaryAsync(c => c.Name);

        var activeIds = (await db.UpdateJobs.AsNoTracking()
                .Where(j => j.Status != UpdateJobStatus.Succeeded && j.Status != UpdateJobStatus.Failed)
                .Select(j => j.InstalledAppId).ToListAsync())
            .Where(id => id != null).Select(id => id!.Value).ToHashSet();

        var batch = Guid.NewGuid();
        var created = new List<UpdateJob>();
        var skipped = new List<string>();

        foreach (var app in apps)
        {
            var host = app.Computer?.Hostname ?? "?";
            catalog.TryGetValue(app.Name, out var item);

            // YENİ (14c): gerçek cihaz kontrolü sunucuda da yapılır
            var state = UpdateRules.Evaluate(app, item, activeIds.Contains(app.Id), app.Computer?.IsReal == true);
            if (state != UpdateState.UpdateAvailable)
            {
                skipped.Add($"{host} / {app.Name}: {Reason(state)}");
                continue;
            }

            string command;
            try { command = UpdateCommandBuilder.BuildUpgradeCommand(item!.PackageId); }
            catch (ArgumentException)
            {
                skipped.Add($"{host} / {app.Name}: geçersiz paket kimliği");
                continue;
            }

            created.Add(new UpdateJob
            {
                ComputerId = app.ComputerId,
                InstalledAppId = app.Id,
                Hostname = host,
                AppName = app.Name,
                FromVersion = app.Version,
                ToVersion = item.LatestVersion,
                PackageId = item.PackageId,
                Command = command,
                InstallerSha256 = item.InstallerSha256,
                RequestedBy = requestedBy,
                BatchId = batch
            });
        }

        foreach (var missing in ids.Except(apps.Select(a => a.Id)))
            skipped.Add($"Uygulama kaydı bulunamadı (#{missing})");

        db.UpdateJobs.AddRange(created);
        await db.SaveChangesAsync();
        return new UpdateRequestResult(created, skipped, batch);
    });

    public Task<List<UpdateJob>> GetJobsAsync(int? computerId, int take = 50) => WithDb(db =>
    {
        var q = db.UpdateJobs.AsNoTracking().AsQueryable();
        if (computerId is not null) q = q.Where(j => j.ComputerId == computerId);
        return q.OrderByDescending(j => j.RequestedAt).Take(take).ToListAsync();
    });

    public async Task<UpdateRequestResult> RetryAsync(string requestedBy, int jobId)
    {
        var appId = await WithDb(db => db.UpdateJobs.AsNoTracking()
            .Where(j => j.Id == jobId && j.Status == UpdateJobStatus.Failed)
            .Select(j => j.InstalledAppId).FirstOrDefaultAsync());

        if (appId is null)
            return new UpdateRequestResult(new(), new() { "Yeniden denenecek başarısız iş bulunamadı" }, Guid.NewGuid());

        // Yeni iş, normal kurallardan geçer (uygulama hâlâ eski mi? aktif iş var mı?)
        return await CreateJobsAsync(requestedBy, new[] { appId.Value });
    }

    private static string Reason(UpdateState state) => state switch
    {
        UpdateState.UpToDate => "zaten güncel",
        UpdateState.JobActive => "zaten bir güncelleme işi sürüyor",
        UpdateState.NotInCatalog => "katalogda yok",
        UpdateState.Unauthorized => "yetkisiz yazılım, güncellenmez",
        UpdateState.RealComputer => "gerçek cihaz, güncelleme kapalı",   // YENİ (14c)
        _ => "güncellenemez"
    };
}