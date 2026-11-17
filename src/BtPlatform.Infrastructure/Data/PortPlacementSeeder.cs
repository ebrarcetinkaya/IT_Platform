using BtPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Infrastructure.Data;

// Simüle bilgisayarları VLAN'larına uyan kullanıcı portlarına yerleştirir
// (gerçekte switch'in MAC adres tablosu bunu kendiliğinden öğrenir).
// Tekrar çalıştırılabilir: zaten yerleşmiş bilgisayara dokunmaz. Gerçek kayıt (IsReal) yerleştirilmez.
public static class PortPlacementSeeder
{
    public static int Seed(AppDbContext db)
    {
        var switches = db.Switches.Include(s => s.Ports)
            .Where(s => s.Name.StartsWith("SW-KAT")).OrderBy(s => s.Name).ToList();
        if (switches.Count == 0) return 0;

        var allPorts = switches.SelectMany(s => s.Ports).ToList();
        var placedHashes = db.SwitchPorts.Where(p => p.LearnedMacHash != null)
            .Select(p => p.LearnedMacHash!).ToHashSet();

        var computers = db.Computers.Where(c => !c.IsReal).OrderBy(c => c.Hostname).ToList();
        var placed = 0;
        var index = 0;

        foreach (var c in computers)
        {
            index++;
            if (string.IsNullOrEmpty(c.MacAddressHash) || placedHashes.Contains(c.MacAddressHash)) continue;

            // Boş kullanıcı portu (uplink 23-24 hariç), bilgisayarın VLAN'ında
            List<SwitchPort> Free() => allPorts
                .Where(p => p.Number <= 22 && p.Vlan == c.Vlan && p.LearnedMacHash == null)
                .OrderBy(p => p.Switch!.Name).ThenBy(p => p.Number).ToList();

            var candidates = Free();

            // Bu VLAN için hiç boş port yoksa: SW-KAT3'ün boştaki bir VLAN 30 portunu bu VLAN'a çevir
            if (candidates.Count == 0)
            {
                var spare = allPorts
                    .Where(p => p.Switch!.Name == "SW-KAT3" && p.Number <= 22 && p.Vlan == 30 && p.LearnedMacHash == null)
                    .OrderByDescending(p => p.Number).FirstOrDefault();
                if (spare is null) continue;
                spare.Vlan = c.Vlan;
                candidates = Free();
            }
            if (candidates.Count == 0) continue;

            // Bilgisayarları switch'lere dağıt (hepsi aynı porta ya da aynı switch'e yığılmasın)
            var port = candidates[(index - 1) % candidates.Count];
            port.LearnedMacHash = c.MacAddressHash;
            placedHashes.Add(c.MacAddressHash);
            placed++;
        }

        if (placed > 0) db.SaveChanges();
        return placed;
    }
}