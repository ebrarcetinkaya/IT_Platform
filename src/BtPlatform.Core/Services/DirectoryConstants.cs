namespace BtPlatform.Core.Services;

public static class DirectoryConstants
{
    public const string DomainUsersGroup = "Domain Users";

    // Hesap kilitlenince üretilen alarmın başlığı ve mesajı.
    // Kilit açılınca aynı alarmı bulup kapatabilmek için tek yerde tutuyoruz.
    public const string LockedTitle = "AD hesabı kilitlendi";

    public static string LockedMessage(string username) =>
        $"{username} hesabı çok sayıda başarısız parola denemesi nedeniyle kilitlendi";
}