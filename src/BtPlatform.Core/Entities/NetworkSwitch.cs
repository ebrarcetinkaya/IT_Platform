namespace BtPlatform.Core.Entities;

public class NetworkSwitch
{
    public int Id { get; set; }
    public string Name { get; set; } = "";          // SW-KAT1
    public string Model { get; set; } = "";
    public string Location { get; set; } = "";
    public string ManagementIp { get; set; } = "";
    public int CpuPercent { get; set; }
    public int MemoryPercent { get; set; }
    public int UptimeHours { get; set; }
    public DateTime LastPolledAt { get; set; } = DateTime.UtcNow;

    public string ConfigText { get; set; } = "";    // yapılandırma yedeği
    public string ConfigHash { get; set; } = "";    // SHA-256 parmak izi
    public DateTime ConfigChangedAt { get; set; } = DateTime.UtcNow;

    public List<SwitchPort> Ports { get; set; } = new();
}