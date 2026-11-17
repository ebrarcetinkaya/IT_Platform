using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BtPlatform.Infrastructure.Services;

public class RealComputerService : IRealComputerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IInventoryCollector _collector;
    private readonly HmacHasher _hmac;

    public RealComputerService(IServiceScopeFactory scopeFactory, IInventoryCollector collector, HmacHasher hmac)
    {
        _scopeFactory = scopeFactory;
        _collector = collector;
        _hmac = hmac;
    }

    public async Task<Computer?> GetRealAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Computers.AsNoTracking().FirstOrDefaultAsync(c => c.IsReal);
    }

    public async Task<RealComputerResult> CollectAndSaveAsync(RealComputerForm form, CancellationToken ct = default)
    {
        // 1) Önce form doğrulanır: hatalıysa PowerShell hiç çalışmaz
        var errors = RealComputerFormValidator.Validate(form, DateTime.UtcNow);
        if (errors.Count > 0) return new RealComputerResult(null, 0, false, errors);

        // 2) Toplama (çıktıyı InventoryJsonParser zaten doğruladı)
        var collected = await _collector.CollectAsync(ct);
        if (!collected.Ok) return RealComputerResult.Fail(collected.Error ?? "Envanter toplanamadı.");
        var snap = collected.Snapshot!;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.Computers.Include(c => c.Apps).FirstOrDefaultAsync(c => c.IsReal, ct);
        var tag = form.AssetTag.Trim();

        // 3) Simüle kayıtlarla çakışma kontrolü
        var hostLower = snap.Hostname.ToLower();
        if (await db.Computers.AnyAsync(c => !c.IsReal && c.Hostname.ToLower() == hostLower, ct))
            return RealComputerResult.Fail($"'{snap.Hostname}' adlı başka bir kayıt zaten var, eklenmedi.");
        if (await db.Computers.AnyAsync(c => c.AssetTag == tag && (existing == null || c.Id != existing.Id), ct))
            return RealComputerResult.Fail($"'{tag}' zimmet etiketi başka bir kayıtta kullanılıyor.");

        var created = existing is null;
        var pc = existing ?? new Computer();
        if (created) db.Computers.Add(pc);

        pc.AssetTag = tag;
        pc.Hostname = snap.Hostname;
        pc.Manufacturer = snap.Manufacturer;
        pc.Model = snap.Model;
        pc.SerialNumber = snap.SerialNumber;
        pc.Processor = snap.Processor;
        pc.RamGb = snap.RamGb;
        pc.DiskGb = snap.DiskGb;
        // Ham IP/MAC/OS adı burada hash'lenir, hiçbir yere yazılmaz
        pc.OsNameHash = _hmac.HashOs(snap.OsName);
        pc.IpAddressHash = _hmac.HashIp(snap.IpAddress);
        pc.MacAddressHash = _hmac.HashMac(snap.MacAddress);
        pc.Vlan = form.Vlan;
        pc.AssignedUser = form.AssignedUser.Trim();
        pc.Department = form.Department.Trim();
        pc.Location = (form.Location ?? "").Trim();
        pc.WarrantyEnd = form.WarrantyEnd;
        pc.Status = DeviceStatus.InUse;
        pc.IsReal = true;
        pc.IsOnline = true;
        pc.LastSeenAt = DateTime.UtcNow;

        // Yazılım listesi her toplamada baştan yazılır (gerçek kayıtta iş geçmişi yok, güncelleme kapalı)
        if (!created) db.InstalledApps.RemoveRange(pc.Apps);
        pc.Apps = snap.Apps.Select(a => new InstalledApp
        {
            Name = a.Name,
            Version = a.Version,
            Publisher = a.Publisher,
            IsAuthorized = true,
            FileHash = Sha256Hasher.Compute($"{a.Name}|{a.Version}|{a.Publisher}")
        }).ToList();

        await db.SaveChangesAsync(ct);
        return new RealComputerResult(pc, pc.Apps.Count, created, new());
    }
}