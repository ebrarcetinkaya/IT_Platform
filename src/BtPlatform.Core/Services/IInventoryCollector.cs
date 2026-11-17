namespace BtPlatform.Core.Services;

// Toplayıcının okuduğu yazılım kaydı
public record InventoryApp(string Name, string Version, string Publisher);

// Toplayıcının bu bilgisayardan okuduğu ham bilgiler.
// DİKKAT: IP, MAC ve OS adı burada ham halde durur, sadece bellekte yaşar.
// Veritabanına yazılmadan önce HmacHasher ile özetlenecek (14c).
public record InventorySnapshot(
    string Hostname,
    string Manufacturer,
    string Model,
    string SerialNumber,
    string Processor,
    int RamGb,
    int DiskGb,
    string OsName,
    string IpAddress,
    string MacAddress,
    IReadOnlyList<InventoryApp> Apps);

// İstisna yerine sonuç nesnesi (projedeki genel kural)
public record InventoryResult(InventorySnapshot? Snapshot, string? Error)
{
    public bool Ok => Snapshot is not null;
    public static InventoryResult Success(InventorySnapshot s) => new(s, null);
    public static InventoryResult Fail(string error) => new(null, error);
}

public interface IInventoryCollector
{
    // Çalıştığı bilgisayarın envanterini toplar. Hata olursa Error doludur.
    Task<InventoryResult> CollectAsync(CancellationToken ct = default);
}