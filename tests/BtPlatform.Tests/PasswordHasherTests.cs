using BtPlatform.Core.Security;
using Xunit;

namespace BtPlatform.Tests;

public class PasswordHasherTests
{
    // Testlerin hızlı çalışması için az tur kullanıyoruz.
    private const int Fast = 1000;

    [Fact]
    public void Hash_DoesNotContainPlainPassword()
    {
        var hash = PasswordHasher.Hash("Gizli123!", Fast);
        Assert.DoesNotContain("Gizli123!", hash);
    }

    [Fact]
    public void SamePassword_ProducesDifferentHashes_BecauseOfSalt()
    {
        var h1 = PasswordHasher.Hash("Gizli123!", Fast);
        var h2 = PasswordHasher.Hash("Gizli123!", Fast);
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("Gizli123!", Fast);
        Assert.True(PasswordHasher.Verify("Gizli123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("Gizli123!", Fast);
        Assert.False(PasswordHasher.Verify("gizli123!", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("duz-metin")]
    [InlineData("PBKDF2-SHA256$abc$xx$yy")]
    [InlineData("PBKDF2-SHA256$1000$!!!$???")]
    public void Verify_MalformedStoredValue_ReturnsFalse(string stored)
    {
        Assert.False(PasswordHasher.Verify("Gizli123!", stored));
    }

    [Fact]
    public void Verify_UsesIterationCountFromStoredValue()
    {
        // Farklı tur sayısıyla üretilmiş kayıt da doğrulanabilmeli.
        var hash = PasswordHasher.Hash("Gizli123!", 2000);
        Assert.StartsWith("PBKDF2-SHA256$2000$", hash);
        Assert.True(PasswordHasher.Verify("Gizli123!", hash));
    }
}