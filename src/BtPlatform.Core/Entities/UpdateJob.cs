namespace BtPlatform.Core.Entities;

public class UpdateJob
{
    public int Id { get; set; }

    // Bilgisayar ya da uygulama sonradan silinse bile iş geçmişi kalsın diye FK'ler boş olabilir
    public int? ComputerId { get; set; }
    public Computer? Computer { get; set; }
    public int? InstalledAppId { get; set; }
    public InstalledApp? InstalledApp { get; set; }

    // Kayıt anındaki bilgilerin kopyası: geçmiş sonradan değişen verilerden etkilenmesin
    public string Hostname { get; set; } = "";
    public string AppName { get; set; } = "";
    public string FromVersion { get; set; } = "";
    public string ToVersion { get; set; } = "";
    public string PackageId { get; set; } = "";
    public string Command { get; set; } = "";           // şablondan üretilmiş komut
    public string InstallerSha256 { get; set; } = "";   // ajanın doğrulayacağı yükleyici özeti

    public UpdateJobStatus Status { get; set; } = UpdateJobStatus.Pending;
    public string RequestedBy { get; set; } = "";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Output { get; set; } = "";

    // Aynı istekte (örn. 5 bilgisayara toplu güncelleme) oluşan işleri gruplar
    public Guid BatchId { get; set; }
}