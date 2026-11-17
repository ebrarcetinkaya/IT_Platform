using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class UpdateRulesRealComputerTests
{
    private static InstalledApp App(bool authorized = true) =>
        new() { Name = "Google Chrome", Version = "1.0", IsAuthorized = authorized };

    private static SoftwareCatalogItem Cat() =>
        new() { Name = "Google Chrome", PackageId = "Google.Chrome", LatestVersion = "2.0" };

    [Fact]
    public void Evaluate_RealComputer_BlocksUpdateEvenIfOutdated()
    {
        var state = UpdateRules.Evaluate(App(), Cat(), hasActiveJob: false, isRealComputer: true);
        Assert.Equal(UpdateState.RealComputer, state);
    }

    [Fact]
    public void Evaluate_SimulatedComputer_StillAllowsUpdate()
    {
        var state = UpdateRules.Evaluate(App(), Cat(), hasActiveJob: false);
        Assert.Equal(UpdateState.UpdateAvailable, state);
    }

    [Fact]
    public void Evaluate_RealComputer_UnauthorizedAppStaysUnauthorized()
    {
        var state = UpdateRules.Evaluate(App(authorized: false), Cat(), hasActiveJob: false, isRealComputer: true);
        Assert.Equal(UpdateState.Unauthorized, state);
    }
}