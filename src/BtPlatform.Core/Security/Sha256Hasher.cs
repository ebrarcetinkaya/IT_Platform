using System.Security.Cryptography;
using System.Text;

namespace BtPlatform.Core.Security;

public static class Sha256Hasher
{
    public static string Compute(string text) =>
        Compute(Encoding.UTF8.GetBytes(text));

    public static string Compute(byte[] data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public static bool Matches(string text, string expectedHash) =>
        string.Equals(Compute(text), expectedHash, StringComparison.OrdinalIgnoreCase);
}