namespace BtPlatform.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Kullanıcıya foreign key vermiyoruz, sadece adını yazıyoruz.
    // Kullanıcı silinse bile "kim yaptı" bilgisi kayıtta kalmalı.
    public string Username { get; set; } = "";

    public string Action { get; set; } = "";   // örn: "Giriş", "Ticket durumu değişti"
    public string Details { get; set; } = "";  // örn: "Ticket #4: Open -> InProgress"
}