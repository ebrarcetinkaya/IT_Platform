namespace BtPlatform.Core.Entities;

// "Bu uygulamanın bilinen en güncel sürümü ne?" sorusunun cevabı.
// Gerçekte winget kaynağından, üretici sitelerinden ya da zafiyet beslemelerinden gelir.
public class SoftwareCatalogItem
{
    public int Id { get; set; }

    public string Name { get; set; } = "";             // InstalledApp.Name ile eşleşir
    public string PackageId { get; set; } = "";        // winget paket kimliği (komut şablonunda kullanılacak)
    public string LatestVersion { get; set; } = "";
    public DateTime ReleasedAt { get; set; }

    public bool IsSecurityUpdate { get; set; }         // güvenlik açığı kapatan sürüm mü?
    public string ReleaseNotes { get; set; } = "";

    // Yükleyici dosyasının SHA-256'sı: ajan indirdiği dosyayı bununla karşılaştırır,
    // tutmuyorsa kurulumu durdurur (değiştirilmiş yükleyiciye karşı koruma)
    public string InstallerSha256 { get; set; } = "";
}