using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using BtPlatform.Core.Services;

namespace BtPlatform.Infrastructure.Services;

// "winget upgrade" komutunu SABİT argümanlarla çalıştırır. Paket kimliği ya da --all yok:
// bu mod yalnızca listeler, hiçbir uygulamayı kurmaz ya da güncellemez.
public class WingetScanner : IWingetScanner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private const int MaxOutputChars = 256 * 1024;

    private static readonly string[] Args =
    {
        "upgrade", "--include-unknown", "--accept-source-agreements", "--disable-interactivity"
    };

    public async Task<WingetScanResult> ScanAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
            return WingetScanResult.Fail("Winget taraması sadece Windows'ta çalışır.");

        var psi = new ProcessStartInfo("winget.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in Args) psi.ArgumentList.Add(a);

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return WingetScanResult.Fail("Winget başlatılamadı.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            var stdoutTask = ReadLimitedAsync(process.StandardOutput, cts);
            var stderrTask = process.StandardError.ReadToEndAsync();

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return WingetScanResult.Fail(ct.IsCancellationRequested
                    ? "Winget taraması iptal edildi."
                    : "Winget taraması zaman aşımına uğradı (60 sn) ya da çıktı çok büyük.");
            }

            var stdout = await stdoutTask;
            _ = await stderrTask;

            var parsed = WingetOutputParser.Parse(stdout);
            if (parsed.TableFound) return WingetScanResult.Success(parsed.Items);

            // Tablo yok: "güncelleme yok" mesajı mı, gerçek hata mı?
            var text = stdout.ToLowerInvariant();
            var saysNothing = text.Contains("no installed package found")
                              || text.Contains("no applicable upgrade")
                              || text.Contains("eşleşen yüklü paket bulunamadı")
                              || text.Contains("uygun güncelleştirme bulunamadı")
                              || text.Contains("güncelleme bulunamadı");

            if (process.ExitCode == 0 || saysNothing)
                return WingetScanResult.Success(new List<WingetUpgrade>());

            return WingetScanResult.Fail($"Winget hata verdi (çıkış kodu {process.ExitCode}).");
        }
        catch (Win32Exception)
        {
            return WingetScanResult.Fail("winget.exe bulunamadı. Windows'ta 'Uygulama Yükleyicisi' kurulu olmalı.");
        }
        catch (InvalidOperationException ex)
        {
            return WingetScanResult.Fail($"Winget çalıştırılamadı: {ex.Message}");
        }
    }

    // Çıktıyı sınırlı okur: sınır aşılırsa işlemi iptal ettirir (cts), bellek şişmez.
    private static async Task<string> ReadLimitedAsync(StreamReader reader, CancellationTokenSource cts)
    {
        var sb = new StringBuilder();
        var buffer = new char[4096];
        try
        {
            int n;
            while ((n = await reader.ReadAsync(buffer.AsMemory(), cts.Token)) > 0)
            {
                sb.Append(buffer, 0, n);
                if (sb.Length > MaxOutputChars)
                {
                    cts.Cancel();
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        return sb.ToString();
    }
}