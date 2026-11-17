using System.Text;
using BtPlatform.Core.Entities;
using BtPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

// Sanal ajan: simüle bilgisayarlardaki bekleyen komutları alır ve SAHTE çıktı üretir.
// Hiçbir komut gerçekten çalışmaz. Gerçek hedefli işlere dokunmaz (onlar 17e'de ayrı çalıştırıcıyla).
public class RemoteCommandAgentSimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RemoteCommandAgentSimulationService> _logger;
    private readonly Random _rng = new();

    // İşin bellekteki planı: ne zaman tamamlanacak
    private readonly Dictionary<int, DateTime> _finishAt = new();

    public RemoteCommandAgentSimulationService(IServiceScopeFactory scopeFactory,
        ILogger<RemoteCommandAgentSimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Komut ajanı döngü hatası"); }

            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // Yalnızca simüle hedefler
        var jobs = await db.RemoteCommandJobs.Include(j => j.Computer)
            .Where(j => !j.IsRealTarget
                     && (j.Status == RemoteCommandStatus.Pending || j.Status == RemoteCommandStatus.Running))
            .ToListAsync(ct);

        foreach (var job in jobs)
        {
            // Çevrimdışı bilgisayarda iş bekler
            if (job.Computer is null || !job.Computer.IsOnline) continue;

            if (job.Status == RemoteCommandStatus.Pending)
            {
                job.Status = RemoteCommandStatus.Running;
                job.StartedAt = now;
                _finishAt[job.Id] = now.AddSeconds(_rng.Next(2, 5));
                continue;
            }

            // Running
            if (!_finishAt.TryGetValue(job.Id, out var finish))
            {
                // Sunucu yeniden başladı, plan kayboldu
                if (job.StartedAt is not null && now - job.StartedAt > TimeSpan.FromMinutes(2))
                {
                    job.Status = RemoteCommandStatus.Failed;
                    job.Output = "Komut kesintiye uğradı (sunucu yeniden başladı).";
                    job.CompletedAt = now;
                }
                else _finishAt[job.Id] = now.AddSeconds(2);
                continue;
            }
            if (now < finish) continue;

            var (ok, output) = Fake(job);
            job.Output = "[SİMÜLASYON: komut gerçekte çalıştırılmadı]\n" + output;
            job.Status = ok ? RemoteCommandStatus.Succeeded : RemoteCommandStatus.Failed;
            job.CompletedAt = now;
            _finishAt.Remove(job.Id);
        }

        await db.SaveChangesAsync(ct);
    }

    // Komut metnine bakıp gerçekçi görünen sahte çıktı üretir
    private (bool Ok, string Output) Fake(RemoteCommandJob job)
    {
        var cmd = job.CommandText.Trim();
        var lower = cmd.ToLowerInvariant();
        var seed = Math.Abs(job.Hostname.GetHashCode());
        var r = new Random(seed);

        if (lower.StartsWith("get-process"))
        {
            var names = new[] { "chrome", "Teams", "explorer", "svchost", "OUTLOOK", "Code", "msedge", "lsass" };
            var sb = new StringBuilder("Name        Id      CPU\n----        --      ---\n");
            foreach (var n in names.OrderBy(_ => r.Next()).Take(8))
                sb.AppendLine($"{n,-11} {r.Next(400, 9000),-7} {r.Next(1, 900)}.{r.Next(10, 99)}");
            return (true, sb.ToString());
        }
        if (lower.StartsWith("get-service"))
        {
            var names = new[] { "Spooler", "Dnscache", "EventLog", "W32Time", "wuauserv", "Winmgmt", "LanmanServer" };
            var sb = new StringBuilder("Name           Status\n----           ------\n");
            foreach (var n in names) sb.AppendLine($"{n,-14} Running");
            return (true, sb.ToString());
        }
        if (lower.StartsWith("get-hotfix"))
        {
            var sb = new StringBuilder("HotFixID    Description       InstalledOn\n----------  -----------       -----------\n");
            for (var i = 0; i < 5; i++)
                sb.AppendLine($"KB50{r.Next(10000, 99999)}  Security Update   {DateTime.Today.AddDays(-(i * 30 + r.Next(1, 20))):dd.MM.yyyy}");
            return (true, sb.ToString());
        }
        if (lower.StartsWith("ipconfig"))
            return (true, $"Windows IP Configuration\n\n   Host Name . . . : {job.Hostname}\n   Primary Dns Suffix . : bt.local\n" +
                          $"\nEthernet adapter Ethernet:\n   IPv4 Address. . . : 192.168.{job.Computer?.Vlan ?? 10}.{r.Next(10, 200)}\n" +
                          $"   Subnet Mask . . . : 255.255.255.0\n   Default Gateway . : 192.168.{job.Computer?.Vlan ?? 10}.1\n");
        if (lower.StartsWith("test-connection"))
            return (true, "Source        Destination   Bytes  Time(ms)\n------        -----------   -----  --------\n" +
                          $"{job.Hostname,-13} hedef          32     {r.Next(1, 6)}\n{job.Hostname,-13} hedef          32     {r.Next(1, 6)}\n");
        if (lower.Contains("win32_logicaldisk"))
            return (true, "DeviceID  Size          FreeSpace\n--------  ----          ---------\n" +
                          $"C:        {(job.Computer?.DiskGb ?? 256) * 1073741824L}   {r.Next(40, 200) * 1073741824L}\n");
        if (lower.Contains("win32_operatingsystem"))
            return (true, "TotalVisibleMemorySize  FreePhysicalMemory\n----------------------  ------------------\n" +
                          $"{(job.Computer?.RamGb ?? 8) * 1048576}                {r.Next(2, 6) * 1048576}\n");
        if (lower.StartsWith("gpupdate"))
            return (true, "Updating policy...\n\nComputer Policy update has completed successfully.\nUser Policy update has completed successfully.\n");
        if (lower.StartsWith("restart-service spooler"))
            return (true, "WARNING: Waiting for service 'Print Spooler (Spooler)' to stop...\nWARNING: Waiting for service 'Print Spooler (Spooler)' to start...\n");

        return (false, "Simülasyon bu komutu desteklemiyor. Desteklenenler: Get-Process, Get-Service, Get-HotFix, ipconfig, " +
                       "Test-Connection, Win32_LogicalDisk, Win32_OperatingSystem, gpupdate, Restart-Service Spooler.");
    }
}