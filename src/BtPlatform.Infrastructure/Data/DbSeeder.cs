using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;

namespace BtPlatform.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db, HmacHasher hmac)
    {
        if (db.Computers.Any()) return;   // zaten doluysa tekrar ekleme

        var users = new[]
        {
            ("Ali Yılmaz", "Muhasebe", "MUH"), ("Ayşe Demir", "İnsan Kaynakları", "IK"),
            ("Mehmet Kaya", "Bilgi Teknolojileri", "BT"), ("Zeynep Çelik", "Satış", "SAT"),
            ("Can Öztürk", "Muhasebe", "MUH"), ("Elif Şahin", "Yönetim", "YON"),
            ("Burak Arslan", "Satış", "SAT"), ("Selin Aydın", "İnsan Kaynakları", "IK"),
            ("Emre Koç", "Bilgi Teknolojileri", "BT"), ("Deniz Polat", "Muhasebe", "MUH"),
        };
        var vlans = new Dictionary<string, int>
        { ["MUH"] = 20, ["IK"] = 30, ["BT"] = 10, ["SAT"] = 40, ["YON"] = 50 };
        var models = new[]
        {
            ("Dell", "Latitude 5440", "Intel Core i5-1335U", 16, 512),
            ("HP", "ProBook 450 G10", "Intel Core i7-1355U", 16, 512),
            ("Lenovo", "ThinkPad E14", "Intel Core i5-1235U", 8, 256),
        };
        string[] os = { "Windows 11 Pro", "Windows 11 Pro", "Windows 10 Pro" };
        string[] badApps = { "AnyDesk", "uTorrent" };

        var rnd = new Random(42);   // sabit tohum: her seferinde aynı veri
        var computers = new List<Computer>();

        for (int i = 0; i < users.Length; i++)
        {
            var (name, dept, code) = users[i];
            var m = models[i % models.Length];
            var vlan = vlans[code];
            var ip = $"192.168.{vlan}.{10 + i}";
            var mac = string.Join(":", Enumerable.Range(0, 6)
                .Select(_ => rnd.Next(0, 256).ToString("X2")));

            var pc = new Computer
            {
                AssetTag = $"BT-{100 + i:D4}",
                Hostname = $"PC-{code}-{i + 1:D3}",
                Manufacturer = m.Item1,
                Model = m.Item2,
                SerialNumber = $"SN{rnd.Next(100000, 999999)}",
                Processor = m.Item3,
                RamGb = m.Item4,
                DiskGb = m.Item5,
                OsNameHash = hmac.HashOs(os[i % os.Length]),
                IpAddressHash = hmac.HashIp(ip),
                MacAddressHash = hmac.HashMac(mac),
                Vlan = vlan,
                AssignedUser = name,
                Department = dept,
                Location = $"{dept} Katı",
                WarrantyEnd = DateTime.UtcNow.AddDays(rnd.Next(-90, 900)),
                Status = DeviceStatus.InUse,
            };

            pc.Apps.Add(NewApp("Google Chrome", "126.0", "Google", pc.Hostname));
            pc.Apps.Add(NewApp("7-Zip", i % 3 == 0 ? "19.00" : "23.01", "Igor Pavlov", pc.Hostname));
            pc.Apps.Add(NewApp("Microsoft Office", "2021", "Microsoft", pc.Hostname));
            if (i % 4 == 1)
                pc.Apps.Add(NewApp(badApps[i % 2], "1.0", "Bilinmiyor", pc.Hostname, authorized: false));

            computers.Add(pc);
        }

        db.Computers.AddRange(computers);
        db.SaveChanges();

        db.Tickets.AddRange(
            NewTicket("Yazıcıdan çıktı alınamıyor", TicketCategory.Printer, TicketPriority.Medium, TicketStatus.Open, "Ali Yılmaz", "", computers[0].Id),
            NewTicket("Kullanıcı hesabı kilitlendi", TicketCategory.ActiveDirectory, TicketPriority.High, TicketStatus.InProgress, "Ayşe Demir", "Ebrar", computers[1].Id),
            NewTicket("İnternet çok yavaş", TicketCategory.Network, TicketPriority.Medium, TicketStatus.Open, "Zeynep Çelik", "", computers[3].Id),
            NewTicket("Bilgisayar açılmıyor", TicketCategory.Hardware, TicketPriority.Critical, TicketStatus.InProgress, "Can Öztürk", "Mehmet Kaya", computers[4].Id),
            NewTicket("Office lisans uyarısı", TicketCategory.Software, TicketPriority.Low, TicketStatus.Resolved, "Elif Şahin", "Ebrar", computers[5].Id),
            NewTicket("Yeni çalışan hesabı açılması", TicketCategory.ActiveDirectory, TicketPriority.Medium, TicketStatus.Closed, "Selin Aydın", "Emre Koç", null));
        db.SaveChanges();
    }

    private static InstalledApp NewApp(string name, string version, string publisher, string host, bool authorized = true) =>
        new()
        {
            Name = name,
            Version = version,
            Publisher = publisher,
            IsAuthorized = authorized,
            FileHash = Sha256Hasher.Compute($"{name}|{version}|{publisher}")
        };

    private static Ticket NewTicket(string title, TicketCategory cat, TicketPriority pri,
        TicketStatus st, string by, string to, int? computerId) =>
        new()
        {
            Title = title,
            Description = title + " (örnek kayıt)",
            Category = cat,
            Priority = pri,
            Status = st,
            RequestedBy = by,
            AssignedTo = to,
            ComputerId = computerId,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 20)),
            ResolvedAt = st is TicketStatus.Resolved or TicketStatus.Closed ? DateTime.UtcNow : null
        };
}