using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BtPlatform.Core.Services;

namespace BtPlatform.Infrastructure.Services;

// Bu bilgisayarın envanterini, projeye gömülü SABİT bir PowerShell betiğiyle toplar.
// Betik girdi almaz; komuta dışarıdan hiçbir metin karışmaz.
public class PowerShellInventoryCollector : IInventoryCollector
{
    private const string ScriptResource = "collect-inventory.ps1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public async Task<InventoryResult> CollectAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            return InventoryResult.Fail("Envanter toplama sadece Windows'ta çalışır.");

        string script;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource))
        {
            if (stream is null)
                return InventoryResult.Fail("Envanter betiği programa gömülmemiş (csproj ayarını kontrol et).");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            script = await reader.ReadToEndAsync(ct);
        }

        // Betik, -EncodedCommand ile verilir (UTF-16 + Base64): geçici dosya ve yol sorunu olmaz.
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        var psi = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", encoded })
            psi.ArgumentList.Add(arg);

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
                return InventoryResult.Fail("PowerShell başlatılamadı.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return InventoryResult.Fail(ct.IsCancellationRequested
                    ? "Envanter toplama iptal edildi."
                    : "Envanter toplama zaman aşımına uğradı (30 sn).");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                var msg = stderr.Trim();
                if (msg.Length > 200) msg = msg[..200];
                return InventoryResult.Fail($"Toplayıcı hata verdi: {(msg == "" ? "bilinmeyen hata" : msg)}");
            }

            // Çıktıyı doğrulayan asıl kontrol 14a'daki ayrıştırıcıda
            return InventoryJsonParser.Parse(stdout);
        }
        catch (Win32Exception)
        {
            return InventoryResult.Fail("powershell.exe bulunamadı ya da çalıştırılamadı.");
        }
        catch (InvalidOperationException ex)
        {
            return InventoryResult.Fail($"Toplayıcı çalıştırılamadı: {ex.Message}");
        }
    }
}