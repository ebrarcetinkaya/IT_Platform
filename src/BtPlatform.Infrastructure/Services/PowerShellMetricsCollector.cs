using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BtPlatform.Core.Services;

namespace BtPlatform.Infrastructure.Services;

// Projeye gömülü SABİT ölçüm betiğini çalıştırır. Girdi yok, komuta dışarıdan metin karışmaz.
public class PowerShellMetricsCollector : ILiveMetricsCollector
{
    private const string ScriptResource = "collect-metrics.ps1";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    public async Task<LiveMetricsResult> CollectAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            return LiveMetricsResult.Fail("Canlı ölçüm sadece Windows'ta çalışır.");

        string script;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource))
        {
            if (stream is null)
                return LiveMetricsResult.Fail("Ölçüm betiği programa gömülmemiş (csproj ayarını kontrol et).");
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
            if (process is null) return LiveMetricsResult.Fail("PowerShell başlatılamadı.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return LiveMetricsResult.Fail(ct.IsCancellationRequested
                    ? "Canlı ölçüm iptal edildi."
                    : "Canlı ölçüm zaman aşımına uğradı (20 sn).");
            }

            var stdout = await stdoutTask;
            var stderr = (await stderrTask).Trim();

            if (process.ExitCode != 0)
            {
                if (stderr.Length > 200) stderr = stderr[..200];
                return LiveMetricsResult.Fail($"Ölçüm betiği hata verdi: {(stderr == "" ? "bilinmeyen hata" : stderr)}");
            }

            return LiveMetricsParser.Parse(stdout);
        }
        catch (Win32Exception)
        {
            return LiveMetricsResult.Fail("powershell.exe bulunamadı ya da çalıştırılamadı.");
        }
        catch (InvalidOperationException ex)
        {
            return LiveMetricsResult.Fail($"Ölçüm çalıştırılamadı: {ex.Message}");
        }
    }
}