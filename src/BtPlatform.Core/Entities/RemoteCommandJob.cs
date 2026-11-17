namespace BtPlatform.Core.Entities;

public enum RemoteCommandStatus { Pending, Running, Succeeded, Failed }

// Uzaktan komut işi. Güncelleme işi gibi silinmez, denetim izi olarak kalır.
public class RemoteCommandJob
{
    public int Id { get; set; }

    // Bilgisayar sonradan silinse bile iş geçmişi kalsın diye boş olabilir
    public int? ComputerId { get; set; }
    public Computer? Computer { get; set; }

    // Kayıt anındaki bilgilerin kopyası
    public string Hostname { get; set; } = "";
    public bool IsRealTarget { get; set; }          // gerçek makine mi (kurallar daha sıkı)

    public string? CatalogKey { get; set; }          // katalogdan mı geldi (boşsa serbest komut)
    public string CommandText { get; set; } = "";
    public string Reason { get; set; } = "";         // zorunlu gerekçe

    public RemoteCommandStatus Status { get; set; } = RemoteCommandStatus.Pending;
    public string RequestedBy { get; set; } = "";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Output { get; set; } = "";
}