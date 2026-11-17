using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;
using BtPlatform.Core.Services;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BtPlatform.Infrastructure.Services;

// Gerçek AD yerine veritabanındaki DirectoryUsers/DirectoryGroups tablolarıyla çalışır.
// Her çağrı kendi kısa ömürlü DbContext'ini açar: sayfaların bağlantısıyla çakışmaz.
public class SimulatedDirectoryService : IDirectoryService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SimulatedDirectoryService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task<T> WithDb<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public Task<List<DirectoryUser>> SearchAsync(string? query) => WithDb(async db =>
    {
        var q = db.DirectoryUsers.AsNoTracking().Include(u => u.Groups).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = $"%{query.Trim()}%";
            q = q.Where(u => EF.Functions.Like(u.Username, like)
                          || EF.Functions.Like(u.DisplayName, like)
                          || EF.Functions.Like(u.Department, like)
                          || EF.Functions.Like(u.Email, like));
        }

        return await q.OrderBy(u => u.DisplayName).ToListAsync();
    });

    public Task<DirectoryUser?> GetAsync(int id) => WithDb(db =>
        db.DirectoryUsers.AsNoTracking().Include(u => u.Groups)
          .FirstOrDefaultAsync(u => u.Id == id));

    public Task<List<DirectoryGroup>> GetGroupsAsync() => WithDb(db =>
        db.DirectoryGroups.AsNoTracking().OrderBy(g => g.Name).ToListAsync());

    public Task<DirectoryResult> UnlockAsync(int userId) => WithDb(async db =>
    {
        var user = await db.DirectoryUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return new DirectoryResult(false, "Kullanıcı bulunamadı.");
        if (!user.IsLocked) return new DirectoryResult(false, $"{user.Username} hesabı zaten kilitli değil.");

        user.IsLocked = false;
        user.FailedLogons = 0;
        user.LockedAt = null;

        // Bu hesap için açık "kilitlendi" alarmlarını da kapat
        var prefix = user.Username + " ";
        var alerts = await db.Alerts
            .Where(a => a.ResolvedAt == null
                        && a.Title == DirectoryConstants.LockedTitle
                        && a.Message.StartsWith(prefix))
            .ToListAsync();
        foreach (var a in alerts) a.ResolvedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return new DirectoryResult(true, $"{user.Username} hesabının kilidi açıldı.");
    });

    public Task<DirectoryResult> ResetPasswordAsync(int userId) => WithDb(async db =>
    {
        var user = await db.DirectoryUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return new DirectoryResult(false, "Kullanıcı bulunamadı.");
        if (!user.IsEnabled)
            return new DirectoryResult(false, "Devre dışı bir hesabın parolası sıfırlanamaz. Önce hesabı etkinleştirin.");

        // Geçici parola SADECE sonuçla geri döner: veritabanına yazılmaz, loglanmaz.
        // (Gerçek AD'de bu komut parolayı AD'ye iletir, yönetim aracı saklamaz.)
        var temp = TempPasswordGenerator.Generate();
        user.PasswordLastSet = DateTime.UtcNow;
        user.MustChangePassword = true;

        await db.SaveChangesAsync();
        return new DirectoryResult(true,
            $"{user.Username} için geçici parola oluşturuldu. Kullanıcı ilk girişte değiştirmek zorunda.", temp);
    });

    public Task<DirectoryResult> SetEnabledAsync(int userId, bool enabled) => WithDb(async db =>
    {
        var user = await db.DirectoryUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return new DirectoryResult(false, "Kullanıcı bulunamadı.");
        if (user.IsEnabled == enabled)
            return new DirectoryResult(false, $"{user.Username} hesabı zaten {(enabled ? "etkin" : "devre dışı")}.");

        user.IsEnabled = enabled;
        await db.SaveChangesAsync();
        return new DirectoryResult(true,
            $"{user.Username} hesabı {(enabled ? "etkinleştirildi" : "devre dışı bırakıldı")}.");
    });

    public Task<DirectoryResult> AddToGroupAsync(int userId, int groupId) => WithDb(async db =>
    {
        var user = await db.DirectoryUsers.Include(u => u.Groups).FirstOrDefaultAsync(u => u.Id == userId);
        var group = await db.DirectoryGroups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (user is null || group is null) return new DirectoryResult(false, "Kullanıcı ya da grup bulunamadı.");
        if (user.Groups.Any(g => g.Id == group.Id))
            return new DirectoryResult(false, $"{user.Username} zaten {group.Name} grubunda.");

        user.Groups.Add(group);
        await db.SaveChangesAsync();
        return new DirectoryResult(true, $"{user.Username}, {group.Name} grubuna eklendi.");
    });

    public Task<DirectoryResult> RemoveFromGroupAsync(int userId, int groupId) => WithDb(async db =>
    {
        var user = await db.DirectoryUsers.Include(u => u.Groups).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return new DirectoryResult(false, "Kullanıcı bulunamadı.");

        var group = user.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group is null) return new DirectoryResult(false, "Kullanıcı bu grubun üyesi değil.");
        if (group.Name == DirectoryConstants.DomainUsersGroup)
            return new DirectoryResult(false, $"{DirectoryConstants.DomainUsersGroup} grubundan çıkarılamaz (varsayılan grup).");

        user.Groups.Remove(group);
        await db.SaveChangesAsync();
        return new DirectoryResult(true, $"{user.Username}, {group.Name} grubundan çıkarıldı.");
    });
}