using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class RealComputerFormValidatorTests
{
    private static readonly DateTime Today = new(2026, 11, 3);

    private static RealComputerForm Make(string tag = "BT-0999", string user = "Ebrar", string dept = "Bilgi Teknolojileri",
        string loc = "BT Katı", int vlan = 10, DateTime? warranty = null) =>
        new(tag, user, dept, loc, vlan, warranty ?? Today.AddYears(1));

    [Fact]
    public void Validate_ValidForm_NoErrors() =>
        Assert.Empty(RealComputerFormValidator.Validate(Make(), Today));

    [Fact]
    public void Validate_EmptyAssetTag_ReturnsError() =>
        Assert.NotEmpty(RealComputerFormValidator.Validate(Make(tag: ""), Today));

    [Fact]
    public void Validate_AssetTagWithSpecialChars_ReturnsError() =>
        Assert.NotEmpty(RealComputerFormValidator.Validate(Make(tag: "BT;0999"), Today));

    [Fact]
    public void Validate_EmptyUser_ReturnsError() =>
        Assert.NotEmpty(RealComputerFormValidator.Validate(Make(user: "  "), Today));

    [Fact]
    public void Validate_EmptyDepartment_ReturnsError() =>
        Assert.NotEmpty(RealComputerFormValidator.Validate(Make(dept: ""), Today));

    [Theory]
    [InlineData(0)]
    [InlineData(4095)]
    public void Validate_VlanOutOfRange_ReturnsError(int vlan) =>
        Assert.NotEmpty(RealComputerFormValidator.Validate(Make(vlan: vlan), Today));

    [Fact]
    public void Validate_WarrantyTooFarAway_ReturnsError() =>
        Assert.NotEmpty(RealComputerFormValidator.Validate(Make(warranty: Today.AddYears(50)), Today));
}