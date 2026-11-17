namespace BtPlatform.Core.Entities;

public enum PcEventLevel { Warning, Error, Critical }

// Gerçek bilgisayardan toplanan Windows olay günlüğü kaydı (yalnızca hata ve uyarılar).
public class PcEvent
{
    public int Id { get; set; }
    public int ComputerId { get; set; }
    public Computer? Computer { get; set; }

    public string LogName { get; set; } = "";      // System | Application
    public PcEventLevel Level { get; set; }
    public int EventId { get; set; }
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime OccurredAt { get; set; }       // UTC
    public long RecordId { get; set; }             // aynı olayın tekrar yazılmasını önler
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}