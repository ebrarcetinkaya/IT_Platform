using System.Text.RegularExpressions;

namespace BtPlatform.Core.Services;

// Güncelleme komutunu SERBEST METİNDEN değil, sabit şablondan üretir.
public static class UpdateCommandBuilder
{
    // winget paket kimliği: harf/rakamla başlar, harf, rakam, nokta, tire, alt çizgi içerir (2-100 karakter).
    // Dikkat: sonda "$" yerine "\z": "$" sonda bir satır sonu karakterine izin verirdi.
    private static readonly Regex PackageIdPattern =
        new(@"^[A-Za-z0-9][A-Za-z0-9._-]{1,99}\z", RegexOptions.Compiled);

    public static bool IsValidPackageId(string? packageId) =>
        !string.IsNullOrEmpty(packageId) && PackageIdPattern.IsMatch(packageId);

    public static string BuildUpgradeCommand(string packageId)
    {
        if (!IsValidPackageId(packageId))
            throw new ArgumentException("Geçersiz paket kimliği.", nameof(packageId));

        // --exact: kimliği tam eşleştir, benzer adlı başka bir paketi güncelleme
        return $"winget upgrade --id {packageId} --exact --silent " +
               "--accept-source-agreements --accept-package-agreements";
    }
}