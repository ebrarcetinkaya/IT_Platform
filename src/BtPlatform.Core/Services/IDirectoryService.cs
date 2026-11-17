using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

// İşlemin sonucu: istisna fırlatmak yerine "oldu/olmadı + mesaj" döneriz
// (kullanıcı hatası beklenen bir durumdur, çökme sebebi değildir).
public record DirectoryResult(bool Success, string Message, string? TemporaryPassword = null);

public interface IDirectoryService
{
    Task<List<DirectoryUser>> SearchAsync(string? query);
    Task<DirectoryUser?> GetAsync(int id);
    Task<List<DirectoryGroup>> GetGroupsAsync();

    Task<DirectoryResult> UnlockAsync(int userId);
    Task<DirectoryResult> ResetPasswordAsync(int userId);
    Task<DirectoryResult> SetEnabledAsync(int userId, bool enabled);
    Task<DirectoryResult> AddToGroupAsync(int userId, int groupId);
    Task<DirectoryResult> RemoveFromGroupAsync(int userId, int groupId);
}