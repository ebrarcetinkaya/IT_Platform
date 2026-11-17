using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class UpdateRulesTests
{
    private static InstalledApp App(string version, bool authorized = true) =>
        new() { Name = "Chrome", Version = version, IsAuthorized = authorized };

    private static SoftwareCatalogItem Catalog(string latest) =>
        new() { Name = "Chrome", LatestVersion = latest, PackageId = "Google.Chrome" };

    [Fact]
    public void Unauthorized_IsNeverUpdated_EvenIfOutdated()
    {
        Assert.Equal(UpdateState.Unauthorized,
            UpdateRules.Evaluate(App("1.0", authorized: false), Catalog("2.0"), false));
    }

    [Fact]
    public void NotInCatalog_ReturnsNotInCatalog()
    {
        Assert.Equal(UpdateState.NotInCatalog, UpdateRules.Evaluate(App("1.0"), null, false));
    }

    [Fact]
    public void Outdated_NoActiveJob_ReturnsUpdateAvailable()
    {
        Assert.Equal(UpdateState.UpdateAvailable, UpdateRules.Evaluate(App("16.0.2"), Catalog("16.0.10"), false));
    }

    [Fact]
    public void SameVersion_ReturnsUpToDate()
    {
        Assert.Equal(UpdateState.UpToDate, UpdateRules.Evaluate(App("2.0"), Catalog("2.0.0"), false));
    }

    [Fact]
    public void NewerThanCatalog_ReturnsUpToDate()
    {
        Assert.Equal(UpdateState.UpToDate, UpdateRules.Evaluate(App("3.0"), Catalog("2.0"), false));
    }

    [Fact]
    public void Outdated_WithActiveJob_ReturnsJobActive()
    {
        Assert.Equal(UpdateState.JobActive, UpdateRules.Evaluate(App("1.0"), Catalog("2.0"), true));
    }
}