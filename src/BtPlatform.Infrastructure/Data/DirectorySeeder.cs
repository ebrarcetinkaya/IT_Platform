using BtPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Infrastructure.Data;

public static class DirectorySeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.DirectoryUsers.Any()) return;

        var domainUsers = new DirectoryGroup { Name = "Domain Users", Description = "Tüm çalışanlar" };
        var btSupport = new DirectoryGroup { Name = "BT-Destek", Description = "BT destek ekibi" };
        var vpn = new DirectoryGroup { Name = "VPN-Kullanicilari", Description = "Uzaktan erişim izni" };
        var muh = new DirectoryGroup { Name = "Muhasebe-Paylasim", Description = "Muhasebe ortak klasörü" };
        var ik = new DirectoryGroup { Name = "IK-Paylasim", Description = "İK ortak klasörü" };
        var sat = new DirectoryGroup { Name = "Satis-Paylasim", Description = "Satış ortak klasörü" };
        db.DirectoryGroups.AddRange(domainUsers, btSupport, vpn, muh, ik, sat);

        // Envanterde bilgisayarı olan kullanıcılardan AD hesapları üretiyoruz,
        // böylece envanter ile dizin birbiriyle tutarlı olur.
        var people = db.Computers.AsNoTracking()
            .Where(c => c.AssignedUser != "")
            .Select(c => new { c.AssignedUser, c.Department })
            .Distinct()
            .OrderBy(c => c.AssignedUser)
            .ToList();

        var now = DateTime.UtcNow;
        var rnd = new Random(11);

        for (var i = 0; i < people.Count; i++)
        {
            var p = people[i];
            var username = ToUsername(p.AssignedUser);
            var user = new DirectoryUser
            {
                Username = username,
                DisplayName = p.AssignedUser,
                Department = p.Department,
                Email = $"{username}@sirket.local",
                PasswordLastSet = now.AddDays(-rnd.Next(5, 120)),
                LastLogonAt = now.AddHours(-rnd.Next(1, 72))
            };

            user.Groups.Add(domainUsers);

            // Departmana göre ortak klasör grubu
            var d = Ascii(p.Department);
            if (d.Contains("muhasebe")) user.Groups.Add(muh);
            else if (d.Contains("insan") || d == "ik") user.Groups.Add(ik);
            else if (d.Contains("satis")) user.Groups.Add(sat);
            else if (d.Contains("bilgi") || d == "bt") user.Groups.Add(btSupport);

            if (rnd.Next(100) < 30) user.Groups.Add(vpn);

            // Gösterim için birkaç özel durum
            if (i == 2)   // kilitli hesap
            {
                user.IsLocked = true;
                user.FailedLogons = 5;
                user.LockedAt = now.AddMinutes(-30);
            }
            if (i == 4) user.MustChangePassword = true;
            if (i == 8)   // işten ayrılmış çalışan
            {
                user.IsEnabled = false;
                user.LastLogonAt = now.AddDays(-45);
            }

            db.DirectoryUsers.Add(user);
        }

        db.SaveChanges();
    }

    // "Ali Yılmaz" -> "ali.yilmaz"
    private static string ToUsername(string fullName) =>
        Ascii(fullName).Trim().Replace(' ', '.');

    // Türkçe karakterleri sadeleştirip küçük harfe çevirir
    private static string Ascii(string text)
    {
        var chars = text.Select(ch => ch switch
        {
            'ı' or 'İ' => 'i',
            'ş' or 'Ş' => 's',
            'ğ' or 'Ğ' => 'g',
            'ü' or 'Ü' => 'u',
            'ö' or 'Ö' => 'o',
            'ç' or 'Ç' => 'c',
            _ => char.ToLowerInvariant(ch)
        });
        return new string(chars.ToArray());
    }
}