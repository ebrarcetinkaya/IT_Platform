using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class SoftwareVersionTests
{
    [Theory]
    [InlineData("16.0.2", "16.0.10", -1)]        // metin olarak karşılaştırsak yanlış çıkardı
    [InlineData("16.0.10", "16.0.2", 1)]
    [InlineData("1.0", "1.0.0", 0)]              // eksik parça 0 sayılır
    [InlineData("v2.1.0-beta", "2.1", 0)]        // ön ek ve sonek yok sayılır
    [InlineData("120.0.6099", "120.0.6100", -1)]
    [InlineData("", "0", 0)]                     // boş sürüm 0 sayılır
    [InlineData("", "1", -1)]
    [InlineData("2.0", "1.99.99", 1)]
    public void Compare_ReturnsExpectedSign(string a, string b, int expected)
    {
        Assert.Equal(expected, Math.Sign(SoftwareVersion.Compare(a, b)));
    }

    [Fact]
    public void IsOlder_InstalledBehindLatest_ReturnsTrue()
    {
        Assert.True(SoftwareVersion.IsOlder("120.0.6099", "121.0.6167"));
    }

    [Fact]
    public void IsOlder_SameOrNewer_ReturnsFalse()
    {
        Assert.False(SoftwareVersion.IsOlder("121.0", "121.0.0"));
        Assert.False(SoftwareVersion.IsOlder("122.0", "121.0.6167"));
    }

    [Theory]
    [InlineData("120.0.6099", 1, "120.0.6100")]
    [InlineData("23.01", 1, "23.02")]            // sıfır dolgusu korunur
    [InlineData("1.0", -1, "1.0")]               // 0'ın altına inmez
    [InlineData("v2", 1, "v3")]
    [InlineData("abc", 1, "abc")]                // sayı yoksa değişmez
    public void ShiftLast_ChangesOnlyLastNumber(string version, int delta, string expected)
    {
        Assert.Equal(expected, SoftwareVersion.ShiftLast(version, delta));
    }
}