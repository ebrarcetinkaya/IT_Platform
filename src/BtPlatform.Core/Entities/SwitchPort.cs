namespace BtPlatform.Core.Entities;

public class SwitchPort
{
    public int Id { get; set; }
    public int SwitchId { get; set; }
    public NetworkSwitch? Switch { get; set; }
    public int Number { get; set; }                 // 1..24
    public string Description { get; set; } = "";
    public PortStatus Status { get; set; } = PortStatus.Up;
    public int Vlan { get; set; }
    public int SpeedMbps { get; set; } = 1000;
    public double TrafficInMbps { get; set; }
    public double TrafficOutMbps { get; set; }

    // Portun öğrendiği cihazın MAC adresinin HMAC özeti (switch'in MAC adres tablosu gibi).
    // Ham MAC saklanmaz. Boşsa portta bilinen cihaz yok.
    public string? LearnedMacHash { get; set; }
}