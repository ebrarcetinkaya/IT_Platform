using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BtPlatform.Core.Services;

namespace BtPlatform.Infrastructure.Services;

// Projeye gömülü SABİT olay betiğini çalıştırır. Girdi yok, komuta dışarıdan metin karışmaz.
public class PowerShellEventCollector : IEventCollector
{
    private const string ScriptResource = "collect-events.ps1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(40);

    public async Task<EventCollectResult> CollectAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            return EventCollectResult.Fail("Olay toplama sadece Windows'ta çalışır.");

        string script;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource))
        {
            if (stream is null)
                return EventCollectResult.Fail("Olay betiği programa gömülmemiş (csproj ayarını kontrol et).");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            script = await reader.ReadToEndAsync(ct);
        }

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
            if (process is null) return EventCollectResult.Fail("PowerShell başlatılamadı.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return EventCollectResult.Fail(ct.IsCancellationRequested
                    ? "Olay toplama iptal edildi."
                    : "Olay toplama zaman aşımına uğradı (40 sn).");
            }

            var stdout = await stdoutTask;
            var stderr = (await stderrTask).Trim();

            if (process.ExitCode != 0)
            {
                if (stderr.Length > 200) stderr = stderr[..200];
                return EventCollectResult.Fail($"Olay betiği hata verdi: {(stderr == "" ? "bilinmeyen hata" : stderr)}");
            }

            return EventJsonParser.Parse(stdout, DateTime.UtcNow);
        }
        catch (Win32Exception) { return EventCollectResult.Fail("powershell.exe bulunamadı ya da çalıştırılamadı."); }
        catch (InvalidOperationException ex) { return EventCollectResult.Fail($"Olay toplama çalıştırılamadı: {ex.Message}"); }
    }
}