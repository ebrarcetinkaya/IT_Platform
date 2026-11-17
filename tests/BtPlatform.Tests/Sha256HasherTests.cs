using BtPlatform.Core.Security;
using Xunit;

namespace BtPlatform.Tests;

public class Sha256HasherTests
{
    [Fact]
    public void Compute_KnownValue_ReturnsExpectedHash()
    {
        // "abc" için standart SHA-256 test vektörü
        var hash = Sha256Hasher.Compute("abc");
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash);
    }

    [Fact]
    public void Compute_SameInput_SameHash() 
    {
        Assert.Equal(Sha256Hasher.Compute("switch-config"), Sha256Hasher.Compute("switch-config"));
    }

    [Fact]
    public void Compute_OneCharChange_ChangesHash()
    {
        Assert.NotEqual(Sha256Hasher.Compute("merhaba"), Sha256Hasher.Compute("merhabA"));
    }
}