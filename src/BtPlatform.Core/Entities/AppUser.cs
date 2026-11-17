namespace BtPlatform.Core.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";

    // Parola ASLA düz metin saklanmaz. Burada PBKDF2 çıktısı (salt + hash) durur.
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Viewer;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}