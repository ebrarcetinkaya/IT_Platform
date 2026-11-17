using System.Security.Cryptography;
using System.Text;

namespace BtPlatform.Core.Security;

public class HmacHasher
{
    private readonly byte[] _key;

    public HmacHasher(string secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Gizli anahtar boş olamaz.", nameof(secretKey));
        _key = Encoding.UTF8.GetBytes(secretKey);
    }

    public string Hash(string value) =>
        Convert.ToHexString(
            HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value))
        ).ToLowerInvariant();

    public string HashIp(string ip) => Hash(ip.Trim());

    public string HashMac(string mac) => Hash(NormalizeMac(mac));

    public string HashOs(string osName) => Hash(osName.Trim().ToLowerInvariant());

    public bool Matches(string value, string expectedHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(value)),
            Encoding.UTF8.GetBytes(expectedHash.ToLowerInvariant()));

    // AA-BB-CC-DD-EE-FF, aa:bb:cc:dd:ee:ff, aabb.ccdd.eeff -> aabbccddeeff
    public static string NormalizeMac(string mac) =>
        new string(mac.Where(Uri.IsHexDigit).ToArray()).ToLowerInvariant();
}