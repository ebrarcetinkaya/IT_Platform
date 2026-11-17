namespace BtPlatform.Core.Entities;

public class Computer
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = "";      // BT-0142
    public string Hostname { get; set; } = "";      // PC-MUH-014
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string Processor { get; set; } = "";
    public int RamGb { get; set; }
    public int DiskGb { get; set; }
    public string OsNameHash { get; set; } = "";       // HMAC-SHA256
    public string IpAddressHash { get; set; } = "";    // HMAC-SHA256
    public string MacAddressHash { get; set; } = "";   // HMAC-SHA256
    public int Vlan { get; set; }
    public string AssignedUser { get; set; } = "";
    public string Department { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime WarrantyEnd { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.InUse;
    public List<InstalledApp> Apps { get; set; } = new();
    // Ajan "kalp atışı": bilgisayar şu an ulaşılabilir mi, en son ne zaman görüldü
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }

    // Bu kayıt gerçek makineden toplandı (simülasyon dokunmaz, güncelleme işi açılamaz)
    public bool IsReal { get; set; }

    // Gerçek kayıt için son canlı ölçüm (null: henüz ölçülmedi)
    public DateTime? MetricsAt { get; set; }
    public double? CpuPercent { get; set; }
    public double? RamTotalGb { get; set; }
    public double? RamUsedGb { get; set; }
    public double? DiskTotalGb { get; set; }
    public double? DiskFreeGb { get; set; }
    public double? UptimeHours { get; set; }

    // Son winget taramasının zamanı (null: henüz taranmadı) ve varsa hata metni
    public DateTime? ScanAt { get; set; }
    public string? ScanError { get; set; }
}
