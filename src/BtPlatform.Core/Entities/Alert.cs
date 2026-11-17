namespace BtPlatform.Core.Entities;

public class Alert
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }       // boşsa alarm hâlâ açık
    public int? SwitchId { get; set; }
    public NetworkSwitch? Switch { get; set; }

    // Güncelleme alarmları için: hangi bilgisayar ve hangi uygulama.
    // Bilerek sadece sayı tutuyoruz (gezinme özelliği yok): bilgisayar silinse bile alarm geçmişi kalır.
    public int? UpdateComputerId { get; set; }
    public int? UpdateInstalledAppId { get; set; }
}