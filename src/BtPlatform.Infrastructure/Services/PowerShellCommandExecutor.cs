using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using BtPlatform.Core.Services;

namespace BtPlatform.Infrastructure.Services;

// Gömülü SABİT betiği çalıştırır; komut metni ortam değişkeniyle geçer, betiğe gömülmez.
public class PowerShellCommandExecutor : IRealCommandExecutor
{
    private const string ScriptResource = "run-command.ps1";
    private const int MaxOutputChars = 20_000;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public async Task<RealCommandResult> ExecuteAsync(string command, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            return new RealCommandResult(false, "Komut çalıştırma sadece Windows'ta çalışır.");
        if (string.IsNullOrWhiteSpace(command) || command.Length > RemoteCommandValidator.MaxCommandLength)
            return new RealCommandResult(false, "Komut geçersiz.");

        string script;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource))
        {
            if (stream is null)
                return new RealCommandResult(false, "Komut betiği programa gömülmemiş (csproj ayarını kontrol et).");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            script = await reader.ReadToEndAsync(ct);
        }

        var psi = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["BT_CMD"] = command;
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", encoded })
            psi.ArgumentList.Add(arg);

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return new RealCommandResult(false, "PowerShell başlatılamadı.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            try { await process.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return new RealCommandResult(false, ct.IsCancellationRequested
                    ? "Komut iptal edildi." : "Komut zaman aşımına uğradı (30 sn).");
            }

            var stdout = await stdoutTask;
            var stderr = (await stderrTask).Trim();

            if (process.ExitCode != 0)
            {
                if (stderr.Length > 500) stderr = stderr[..500];
                return new RealCommandResult(false, stderr == "" ? "Komut hata verdi." : stderr);
            }

            if (stdout.Length > MaxOutputChars)
                stdout = stdout[..MaxOutputChars] + "\n... (çıktı kısaltıldı)";
            return new RealCommandResult(true, stdout);
        }
        catch (Win32Exception) { return new RealCommandResult(false, "powershell.exe bulunamadı."); }
        catch (InvalidOperationException ex) { return new RealCommandResult(false, $"Komut çalıştırılamadı: {ex.Message}"); }
    }
}