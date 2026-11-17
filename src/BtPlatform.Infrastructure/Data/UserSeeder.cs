using System.Security.Cryptography;
using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;
using Microsoft.Extensions.Configuration;

namespace BtPlatform.Infrastructure.Data;

public static class UserSeeder
{
    // Parolalar kodda yazılmaz. Sırayla: user-secrets (Seed:AdminPassword vb.), yoksa rastgele üretilir
    // ve terminalde BİR KEZ gösterilir. Hiçbir yere kaydedilmez, veritabanına yalnızca hash'i gider.
    public static void Seed(AppDbContext db, IConfiguration config)
    {
        if (db.Users.Any()) return;

        var generated = new List<(string User, string Password)>();

        string Pick(string key, string user)
        {
            var configured = config[key];
            if (!string.IsNullOrWhiteSpace(configured)) return configured;
            var pw = RandomPassword();
            generated.Add((user, pw));
            return pw;
        }

        db.Users.AddRange(
            new AppUser
            {
                Username = "admin",
                DisplayName = "Sistem Yöneticisi",
                Role = UserRole.Admin,
                PasswordHash = PasswordHasher.Hash(Pick("Seed:AdminPassword", "admin"))
            },
            new AppUser
            {
                Username = "teknisyen",
                DisplayName = "BT Teknisyeni",
                Role = UserRole.Technician,
                PasswordHash = PasswordHasher.Hash(Pick("Seed:TechnicianPassword", "teknisyen"))
            },
            new AppUser
            {
                Username = "izleyici",
                DisplayName = "İzleyici",
                Role = UserRole.Viewer,
                PasswordHash = PasswordHasher.Hash(Pick("Seed:ViewerPassword", "izleyici"))
            });

        db.SaveChanges();

        if (generated.Count > 0)
        {
            Console.WriteLine("=== İLK GİRİŞ ŞİFRELERİ (yalnızca bir kez gösterilir, not al) ===");
            foreach (var (user, pw) in generated) Console.WriteLine($"  {user} : {pw}");
            Console.WriteLine("================================================================");
        }
    }

    // 16 karakter, karışık harf, rakam ve sembol; benzer görünen karakterler (O/0, l/1) yok
    private static string RandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz",
                     digits = "23456789", symbols = "!@#$%*?-_";
        var all = upper + lower + digits + symbols;
        var chars = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
        };
        while (chars.Count < 16) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        // karıştır
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }
}