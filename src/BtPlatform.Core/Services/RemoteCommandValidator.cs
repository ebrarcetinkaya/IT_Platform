namespace BtPlatform.Core.Services;

public record CommandValidation(bool Ok, string? Error)
{
    public static CommandValidation Pass() => new(true, null);
    public static CommandValidation Fail(string error) => new(false, error);
}

// Serbest komut ve gerekçe kuralları. İstemciye güvenmeyiz: sunucuda yeniden doğrulanır.
//
// Simüle hedef: komut gerçekte çalışmaz, yalnızca uzunluk ve kontrol karakteri denetlenir.
// Gerçek hedef: "izin verilen komut listesi" (deny by default). Boru hattındaki HER komut listede olmalı;
// yazma, indirme, dosyaya yönlendirme, alt komut ve komut zincirleme karakterleri reddedilir.
// Bu kontrol ilk katmandır; 17e'de PowerShell'in kendi ayrıştırıcısıyla ikinci kez doğrulanacak.
public static class RemoteCommandValidator
{
    public const int MaxCommandLength = 500;
    public const int MinReasonLength = 5;
    public const int MaxReasonLength = 300;

    private static readonly char[] RealForbiddenChars = { ';', '&', '`', '$', '{', '}', '<', '>', '(', ')' };

    private static readonly HashSet<string> RealAllowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "Get-Process", "Get-Service", "Get-HotFix", "Get-CimInstance", "Get-NetIPConfiguration",
        "Get-ComputerInfo", "Test-Connection",
        "Select-Object", "Where-Object", "Sort-Object", "Measure-Object", "Format-Table", "ConvertTo-Json"
    };

    public static CommandValidation ValidateReason(string? reason)
    {
        var r = (reason ?? "").Trim();
        if (r.Length < MinReasonLength)
            return CommandValidation.Fail($"Gerekçe en az {MinReasonLength} karakter olmalı.");
        if (r.Length > MaxReasonLength)
            return CommandValidation.Fail($"Gerekçe en fazla {MaxReasonLength} karakter olabilir.");
        return CommandValidation.Pass();
    }

    public static CommandValidation ValidateFree(string? command, bool isRealTarget)
    {
        var c = (command ?? "").Trim();
        if (c.Length == 0) return CommandValidation.Fail("Komut boş olamaz.");
        if (c.Length > MaxCommandLength) return CommandValidation.Fail($"Komut en fazla {MaxCommandLength} karakter olabilir.");
        if (c.Any(char.IsControl)) return CommandValidation.Fail("Komutta satır sonu ya da kontrol karakteri olamaz.");

        if (!isRealTarget) return CommandValidation.Pass();   // simüle: gerçekte çalışmaz

        if (c.IndexOfAny(RealForbiddenChars) >= 0)
            return CommandValidation.Fail("Gerçek makinede bu karakterlere izin yok: ; & ` $ { } < > ( )");

        foreach (var raw in c.Split('|'))
        {
            var segment = raw.Trim();
            if (segment.Length == 0) return CommandValidation.Fail("Boş komut parçası var.");

            var first = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

            if (string.Equals(first, "ipconfig", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(segment, "ipconfig /all", StringComparison.OrdinalIgnoreCase))
                    return CommandValidation.Fail("Gerçek makinede yalnızca 'ipconfig /all' çalıştırılabilir.");
                continue;
            }

            if (!RealAllowed.Contains(first))
                return CommandValidation.Fail($"'{first}' gerçek makinede izinli değil (yalnızca okuma komutları).");
        }

        return CommandValidation.Pass();
    }
}