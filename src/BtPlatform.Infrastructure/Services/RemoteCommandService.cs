using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BtPlatform.Infrastructure.Services;

public class RemoteCommandService : IRemoteCommandService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RemoteCommandService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<RemoteCommandResult> CreateAsync(string requestedBy, RemoteCommandRequest request)
    {
        // 1) Gerekçe
        var reason = RemoteCommandValidator.ValidateReason(request.Reason);
        if (!reason.Ok) return RemoteCommandResult.Fail(reason.Error!);

        // Katalog ve serbest komut aynı anda ya da hiçbiri olamaz
        var hasCatalog = !string.IsNullOrWhiteSpace(request.CatalogKey);
        var hasFree = !string.IsNullOrWhiteSpace(request.FreeCommand);
        if (hasCatalog == hasFree)
            return RemoteCommandResult.Fail("Katalogdan bir komut seç ya da serbest komut yaz (ikisi birden olmaz).");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 2) Hedef: gerçek mi simüle mi, İSTEMCİ DEĞİL VERİTABANI söyler
        var pc = await db.Computers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ComputerId);
        if (pc is null) return RemoteCommandResult.Fail("Hedef bilgisayar bulunamadı.");

        // 3) Komut metni
        string commandText;
        string? catalogKey = null;
        if (hasCatalog)
        {
            var built = RemoteCommandCatalog.Build(request.CatalogKey, request.Parameter, pc.IsReal);
            if (!built.Ok) return RemoteCommandResult.Fail(built.Error!);
            commandText = built.Command!;
            catalogKey = request.CatalogKey!.Trim().ToLowerInvariant();
        }
        else
        {
            var free = RemoteCommandValidator.ValidateFree(request.FreeCommand, pc.IsReal);
            if (!free.Ok) return RemoteCommandResult.Fail(free.Error!);
            commandText = request.FreeCommand!.Trim();
        }

        // 4) Aynı bilgisayarda aynı anda tek aktif komut
        var active = await db.RemoteCommandJobs.AnyAsync(j => j.ComputerId == pc.Id
            && (j.Status == RemoteCommandStatus.Pending || j.Status == RemoteCommandStatus.Running));
        if (active)
            return RemoteCommandResult.Fail("Bu bilgisayarda zaten bekleyen ya da çalışan bir komut var.");

        var job = new RemoteCommandJob
        {
            ComputerId = pc.Id,
            Hostname = pc.Hostname,
            IsRealTarget = pc.IsReal,
            CatalogKey = catalogKey,
            CommandText = commandText,
            Reason = request.Reason.Trim(),
            RequestedBy = requestedBy,
            Status = RemoteCommandStatus.Pending
        };
        db.RemoteCommandJobs.Add(job);
        await db.SaveChangesAsync();
        return new RemoteCommandResult(job, null);
    }

    public async Task<List<RemoteCommandJob>> GetJobsAsync(int computerId, int take = 20)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.RemoteCommandJobs.AsNoTracking()
            .Where(j => j.ComputerId == computerId)
            .OrderByDescending(j => j.RequestedAt).Take(take).ToListAsync();
    }
}