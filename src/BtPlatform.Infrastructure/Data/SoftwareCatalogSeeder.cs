using BtPlatform.Core.Entities;
using BtPlatform.Core.Security;
using BtPlatform.Core.Services;

namespace BtPlatform.Infrastructure.Data;

public static class SoftwareCatalogSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.SoftwareCatalog.Any()) return;

        // Katalog, envanterde gerçekten var olan uygulamalardan üretilir: tutarlı veri
        var apps = db.InstalledApps.ToList();
        var rnd = new Random(23);
        var now = DateTime.UtcNow;

        foreach (var group in apps.GroupBy(a => a.Name))
        {
            var name = group.Key;
            var newest = group.Select(a => a.Version)
                              .Aggregate((x, y) => SoftwareVersion.Compare(x, y) >= 0 ? x : y);
            var isChrome = name.Contains("chrome", StringComparison.OrdinalIgnoreCase);

            // Chrome için yeni bir güvenlik sürümü çıkmış gibi davran: bütün kopyalar bir sürüm geride.
            // Diğerlerinde güncel sürüm = envanterdeki en yüksek sürüm.
            var latest = isChrome ? SoftwareVersion.ShiftLast(newest, +1) : newest;
            var packageId = PackageIdFor(name);

            db.SoftwareCatalog.Add(new SoftwareCatalogItem
            {
                Name = name,
                PackageId = packageId,
                LatestVersion = latest,
                ReleasedAt = now.AddDays(-(isChrome ? 3 : rnd.Next(10, 60))),
                IsSecurityUpdate = isChrome,
                ReleaseNotes = isChrome
                    ? "Güvenlik açıklarını kapatır. Güncellenmesi önerilir."
                    : "Hata düzeltmeleri ve iyileştirmeler.",
                // Simülasyon: gerçekte bu değeri üretici yayımlar
                InstallerSha256 = Sha256Hasher.Compute($"installer|{packageId}|{latest}")
            });

            // Diğer uygulamalarda bazı bilgisayarlar geride kalmış olsun
            if (!isChrome)
            {
                foreach (var app in group)
                {
                    if (rnd.Next(100) < 40)
                    {
                        app.Version = SoftwareVersion.ShiftLast(app.Version, -rnd.Next(1, 4));
                        app.FileHash = Sha256Hasher.Compute($"{app.Name}|{app.Version}|{app.Publisher}");
                    }
                }
            }
        }

        db.SaveChanges();
    }

    // Gerçek winget paket kimliklerine benzer eşleme
    private static string PackageIdFor(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("chrome")) return "Google.Chrome";
        if (n.Contains("7-zip") || n.Contains("7zip")) return "7zip.7zip";
        if (n.Contains("office")) return "Microsoft.Office";
        if (n.Contains("anydesk")) return "AnyDeskSoftwareGmbH.AnyDesk";
        if (n.Contains("torrent")) return "BitTorrent.uTorrent";
        return new string(name.Where(char.IsLetterOrDigit).ToArray());
    }
}