using BtPlatform.Core.Entities;
using BtPlatform.Infrastructure.Data;

namespace BtPlatform.Web.Services;

// Her yerden tek satırla denetim kaydı yazmak için yardımcı sınıf.
// Her kayıt kendi kısa ömürlü DbContext'iyle yazılır: çağıran sayfanın
// yarım kalmış değişikliklerini yanlışlıkla kaydetmez ve çakışmaz.
public class AuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    public AuditLogger(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task LogAsync(string username, string action, string details = "")
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AuditLogs.Add(new AuditLog { Username = username, Action = action, Details = details });
        await db.SaveChangesAsync();
    }
}