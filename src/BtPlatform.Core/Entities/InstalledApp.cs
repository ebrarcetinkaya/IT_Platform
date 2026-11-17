namespace BtPlatform.Core.Entities;

public class InstalledApp
{
    public int Id { get; set; }
    public int ComputerId { get; set; }
    public Computer? Computer { get; set; }
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Publisher { get; set; } = "";
    public bool IsAuthorized { get; set; } = true;   // yetkisiz yazılım işareti
    public string FileHash { get; set; } = "";   // kurulum dosyasının SHA-256 parmak izi
}