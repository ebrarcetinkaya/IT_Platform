using System.Security.Cryptography;

namespace BtPlatform.Core.Security;

// Parolaları PBKDF2-SHA256 ile hash'ler. Çıktı formatı:
//   PBKDF2-SHA256$<tur sayısı>$<salt base64>$<hash base64>
// Tur sayısını da saklıyoruz; ileride artırırsak eski kayıtlar bozulmaz.
public static class PasswordHasher
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int SaltSize = 16;          // 128 bit rastgele salt
    private const int HashSize = 32;          // 256 bit çıktı
    private const int DefaultIterations = 600_000;   // OWASP'ın PBKDF2-SHA256 için önerdiği değer

    public static string Hash(string password, int iterations = DefaultIterations)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Parola boş olamaz.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Prefix}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;

        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix) return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;   // bozuk kayıt
        }
    }
}