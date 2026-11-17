using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;


namespace BtPlatform.Infrastructure.Data;

public static class NetworkSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Switches.Any()) return;

        var defs = new[]
        {
            ("SW-CORE-1", "Cisco Catalyst 9200L-24", "Sunucu odası", "192.168.99.1"),
            ("SW-KAT1",   "Cisco Catalyst 2960X-24", "1. kat dolabı", "192.168.99.11"),
            ("SW-KAT2",   "Cisco Catalyst 2960X-24", "2. kat dolabı", "192.168.99.12"),
            ("SW-KAT3",   "Cisco Catalyst 2960X-24", "3. kat dolabı", "192.168.99.13"),
        };

        var rnd = new Random(7);
        var switches = new List<NetworkSwitch>();

        foreach (var (name, model, location, ip) in defs)
        {
            var config = BuildConfig(name);
            var sw = new NetworkSwitch
            {
                Name = name, Model = model, Location = location, ManagementIp = ip,
                CpuPercent = rnd.Next(5, 30), MemoryPercent = rnd.Next(30, 60),
                UptimeHours = rnd.Next(100, 5000),
                ConfigText = config,
                ConfigHash = Sha256Hasher.Compute(config),
                ConfigChangedAt = DateTime.UtcNow.AddDays(-rnd.Next(5, 60)),
            };

            for (int n = 1; n <= 24; n++)
            {
                var uplink = n >= 23;
                var down = !uplink && n % 7 == 0;
                sw.Ports.Add(new SwitchPort
                {
                    Number = n,
                    Description = uplink ? "Uplink" : $"Kullanıcı portu {n}",
                    Status = down ? PortStatus.Down : PortStatus.Up,
                    Vlan = uplink ? 1 : n <= 8 ? 10 : n <= 16 ? 20 : 30,
                    SpeedMbps = uplink ? 10000 : 1000,
                    TrafficInMbps = down ? 0 : Math.Round(rnd.NextDouble() * (uplink ? 3000 : 300), 1),
                    TrafficOutMbps = down ? 0 : Math.Round(rnd.NextDouble() * (uplink ? 3000 : 300), 1),
                });
            }
            switches.Add(sw);
        }

        db.Switches.AddRange(switches);
        db.SaveChanges();

        db.Alerts.Add(new Alert
        {
            Title = "Port bağlantısı koptu",
            Message = "SW-KAT2 Gi1/0/14 link down (örnek alarm)",
            Severity = AlertSeverity.Warning,
            SwitchId = switches[2].Id,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
        });
        db.SaveChanges();
    }

    private static string BuildConfig(string name) => string.Join("\n", new[]
    {
        $"hostname {name}",
        "!",
        "vlan 10", " name BT",
        "vlan 20", " name MUHASEBE",
        "vlan 30", " name IK",
        "!",
        "interface range Gi1/0/1-8", " switchport access vlan 10",
        "interface range Gi1/0/9-16", " switchport access vlan 20",
        "interface range Gi1/0/17-22", " switchport access vlan 30",
        "interface range Gi1/0/23-24", " switchport mode trunk",
        "!",
        "snmp-server community ******** RO",
        "end",
    });
}