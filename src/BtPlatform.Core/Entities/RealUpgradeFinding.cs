namespace BtPlatform.Core.Entities;

// Gerçek bilgisayarda winget'in bulduğu eski yazılım. Her taramada baştan yazılır.
// Bu bir "bulgu"dur: güncelleme işi değildir, hiçbir şeyi değiştirmez.
public class RealUpgradeFinding
{
    public int Id { get; set; }
    public int ComputerId { get; set; }
    public Computer? Computer { get; set; }

    public string Name { get; set; } = "";
    public string PackageId { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string AvailableVersion { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime FoundAt { get; set; } = DateTime.UtcNow;
}