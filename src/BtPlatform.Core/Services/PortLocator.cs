using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

// Bilgisayarın bağlı olduğu switch portu
public record PortLocation(string SwitchName, int PortNumber, int Vlan, PortStatus Status)
{
    public string Label => $"{SwitchName} Gi1/0/{PortNumber}";
}

// Gerçekte switch'in MAC adres tablosu bilgisayarın MAC'ini bir porta bağlar.
// Burada aynı işi MAC özetleri (HMAC) üzerinden yapıyoruz: ham MAC hiç görünmez.
public static class PortLocator
{
    private const int MaxUserPort = 22;   // 23-24 uplink

    public static PortLocation? Find(string? computerMacHash, IEnumerable<SwitchPort> ports)
    {
        if (string.IsNullOrWhiteSpace(computerMacHash)) return null;

        var matches = ports
            .Where(p => !string.IsNullOrEmpty(p.LearnedMacHash)
                        && string.Equals(p.LearnedMacHash, computerMacHash, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0) return null;

        // Kullanıcı portu uplink'e tercih edilir
        var best = matches.Where(p => p.Number <= MaxUserPort)
                          .OrderBy(p => p.Switch?.Name ?? "").ThenBy(p => p.Number)
                          .FirstOrDefault()
                   ?? matches.OrderBy(p => p.Switch?.Name ?? "").ThenBy(p => p.Number).First();

        return new PortLocation(best.Switch?.Name ?? "?", best.Number, best.Vlan, best.Status);
    }
}