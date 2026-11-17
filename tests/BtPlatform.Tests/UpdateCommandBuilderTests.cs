using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class UpdateCommandBuilderTests
{
    [Theory]
    [InlineData("Google.Chrome")]
    [InlineData("7zip.7zip")]
    [InlineData("Microsoft.Office")]
    [InlineData("AnyDeskSoftwareGmbH.AnyDesk")]
    public void IsValidPackageId_RealisticIds_ReturnsTrue(string id)
    {
        Assert.True(UpdateCommandBuilder.IsValidPackageId(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]                             // çok kısa
    [InlineData("Google.Chrome; calc")]           // komut ekleme denemesi
    [InlineData("Google.Chrome && calc")]
    [InlineData("Google.Chrome\n")]               // sonda satır sonu
    [InlineData("-rf")]                           // seçenek gibi başlıyor
    [InlineData("Google Chrome")]                 // boşluk
    [InlineData("$(whoami)")]
    [InlineData("x|y")]
    public void IsValidPackageId_DangerousOrMalformed_ReturnsFalse(string id)
    {
        Assert.False(UpdateCommandBuilder.IsValidPackageId(id));
    }

    [Fact]
    public void IsValidPackageId_TooLong_ReturnsFalse()
    {
        Assert.False(UpdateCommandBuilder.IsValidPackageId(new string('a', 101)));
    }

    [Fact]
    public void BuildUpgradeCommand_ValidId_ReturnsTemplate()
    {
        var cmd = UpdateCommandBuilder.BuildUpgradeCommand("Google.Chrome");
        Assert.Equal("winget upgrade --id Google.Chrome --exact --silent " +
                     "--accept-source-agreements --accept-package-agreements", cmd);
    }

    [Fact]
    public void BuildUpgradeCommand_InvalidId_Throws()
    {
        Assert.Throws<ArgumentException>(() => UpdateCommandBuilder.BuildUpgradeCommand("Google.Chrome; calc"));
    }
}