namespace BtPlatform.Core.Entities;

// Active Directory'deki bir kullanıcı hesabının simülasyonu.
// Not: Bu, BT platformuna giriş yapan "AppUser"dan ayrıdır. DirectoryUser şirket çalışanıdır.
public class DirectoryUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";        // sAMAccountName, örn. ali.yilmaz
    public string DisplayName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Email { get; set; } = "";

    public bool IsEnabled { get; set; } = true;       // false = hesap devre dışı
    public bool IsLocked { get; set; }                // true = çok sayıda yanlış parola
    public int FailedLogons { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime? LastLogonAt { get; set; }

    public DateTime PasswordLastSet { get; set; } = DateTime.UtcNow;
    public bool MustChangePassword { get; set; }     // ilk girişte parola değiştirmeye zorla

    public List<DirectoryGroup> Groups { get; set; } = new();
}