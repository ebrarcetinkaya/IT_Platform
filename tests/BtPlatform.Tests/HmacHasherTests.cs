using BtPlatform.Core.Security;
using Xunit;

namespace BtPlatform.Tests;

public class HmacHasherTests
{
    [Fact]
    public void Hash_Rfc4231Vector_ReturnsExpected()
    {
        // RFC 4231 Test Case 2 (resmi HMAC-SHA256 test vektörü)
        var hasher = new HmacHasher("Jefe");
        var hash = hasher.Hash("what do ya want for nothing?");
        Assert.Equal("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843", hash);
    }

    [Fact]
    public void Hash_SameKeySameInput_SameHash()
    {
        var h = new HmacHasher("anahtar-1");
        Assert.Equal(h.HashIp("192.168.20.45"), h.HashIp("192.168.20.45"));
    }

    [Fact]
    public void Hash_DifferentKey_DifferentHash()
    {
        var a = new HmacHasher("anahtar-1").HashIp("192.168.20.45");
        var b = new HmacHasher("anahtar-2").HashIp("192.168.20.45");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void HashMac_DifferentFormats_SameHash()
    {
        var h = new HmacHasher("anahtar-1");
        var x = h.HashMac("AA-BB-CC-DD-EE-FF");
        Assert.Equal(x, h.HashMac("aa:bb:cc:dd:ee:ff"));
        Assert.Equal(x, h.HashMac("aabb.ccdd.eeff"));
    }

    [Fact]
    public void Matches_CorrectAndWrongValue()
    {
        var h = new HmacHasher("anahtar-1");
        var stored = h.Hash("Windows 11 Pro");
        Assert.True(h.Matches("Windows 11 Pro", stored));
        Assert.False(h.Matches("Windows 10 Pro", stored));
    }

    [Fact]
    public void Constructor_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HmacHasher(""));
    }
}