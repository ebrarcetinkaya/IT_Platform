using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class RemoteCommandTests
{
    // ---- Katalog ----

    [Fact]
    public void Catalog_Keys_AreUnique() =>
        Assert.Equal(RemoteCommandCatalog.All.Count,
            RemoteCommandCatalog.All.Select(c => c.Key.ToLowerInvariant()).Distinct().Count());

    [Fact]
    public void Build_Processes_ReturnsTemplate()
    {
        var r = RemoteCommandCatalog.Build("processes", null, isRealTarget: true);
        Assert.True(r.Ok);
        Assert.StartsWith("Get-Process", r.Command);
    }

    [Fact]
    public void Build_Ping_InsertsValidTarget()
    {
        var r = RemoteCommandCatalog.Build("ping", "pc-bt-003.local", isRealTarget: false);
        Assert.True(r.Ok);
        Assert.Equal("Test-Connection -ComputerName pc-bt-003.local -Count 2", r.Command);
    }

    [Theory]
    [InlineData("x; Remove-Item C:\\x")]
    [InlineData("a b")]
    [InlineData("")]
    [InlineData("$(calc)")]
    [InlineData("host|calc")]
    public void Build_Ping_RejectsBadTarget(string target) =>
        Assert.False(RemoteCommandCatalog.Build("ping", target, isRealTarget: false).Ok);

    [Fact]
    public void Build_UnknownKey_Fails() =>
        Assert.False(RemoteCommandCatalog.Build("format-disk", null, isRealTarget: false).Ok);

    [Fact]
    public void Build_SimulatedOnlyCommand_FailsOnRealTarget() =>
        Assert.False(RemoteCommandCatalog.Build("spooler", null, isRealTarget: true).Ok);

    [Fact]
    public void Build_SimulatedOnlyCommand_WorksOnSimulatedTarget() =>
        Assert.True(RemoteCommandCatalog.Build("gpupdate", null, isRealTarget: false).Ok);

    [Fact]
    public void Catalog_RealCommands_PassTheReadOnlyGate()
    {
        foreach (var item in RemoteCommandCatalog.All.Where(c => !c.SimulatedOnly))
        {
            var built = RemoteCommandCatalog.Build(item.Key, "localhost", isRealTarget: true);
            Assert.True(built.Ok, item.Key);
            var check = RemoteCommandValidator.ValidateFree(built.Command, isRealTarget: true);
            Assert.True(check.Ok, $"{item.Key}: {check.Error}");
        }
    }

    // ---- Gerekçe ----

    [Fact]
    public void Reason_Valid_Passes() =>
        Assert.True(RemoteCommandValidator.ValidateReason("Yazıcı sorunu, ticket #12").Ok);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void Reason_TooShort_Fails(string reason) =>
        Assert.False(RemoteCommandValidator.ValidateReason(reason).Ok);

    [Fact]
    public void Reason_TooLong_Fails() =>
        Assert.False(RemoteCommandValidator.ValidateReason(new string('a', 301)).Ok);

    // ---- Serbest komut ----

    [Fact]
    public void Free_Simulated_AllowsAnyText() =>
        Assert.True(RemoteCommandValidator.ValidateFree("Restart-Service Spooler", isRealTarget: false).Ok);

    [Fact]
    public void Free_Simulated_RejectsEmpty() =>
        Assert.False(RemoteCommandValidator.ValidateFree("   ", isRealTarget: false).Ok);

    [Fact]
    public void Free_TooLong_Fails() =>
        Assert.False(RemoteCommandValidator.ValidateFree(new string('a', 501), isRealTarget: false).Ok);

    [Fact]
    public void Free_NewLine_FailsForBothTargets()
    {
        Assert.False(RemoteCommandValidator.ValidateFree("Get-Process\nGet-Service", isRealTarget: false).Ok);
        Assert.False(RemoteCommandValidator.ValidateFree("Get-Process\nGet-Service", isRealTarget: true).Ok);
    }

    [Fact]
    public void Free_Real_AllowsReadOnlyPipeline() =>
        Assert.True(RemoteCommandValidator.ValidateFree(
            "Get-Process | Sort-Object CPU -Descending | Select-Object -First 5 Name, CPU", isRealTarget: true).Ok);

    [Theory]
    [InlineData("Remove-Item C:\\x")]
    [InlineData("Stop-Process -Name notepad")]
    [InlineData("Restart-Service Spooler")]
    [InlineData("Set-ExecutionPolicy Bypass")]
    [InlineData("Invoke-Expression 'dir'")]
    [InlineData("iex 'dir'")]
    [InlineData("Get-Process; Remove-Item x")]
    [InlineData("Get-Process & calc")]
    [InlineData("Get-Process > out.txt")]
    [InlineData("Get-Process | Out-File a.txt")]
    [InlineData("(Remove-Item x)")]
    [InlineData("Get-Process | Stop-Process")]
    [InlineData("ipconfig /release")]
    [InlineData("Get-Process $env:TEMP")]
    [InlineData("Start-Process calc")]
    [InlineData("Invoke-WebRequest http://example.com")]
    public void Free_Real_RejectsDangerousCommands(string command) =>
        Assert.False(RemoteCommandValidator.ValidateFree(command, isRealTarget: true).Ok);

    [Fact]
    public void Free_Real_IpconfigAll_IsAllowed() =>
        Assert.True(RemoteCommandValidator.ValidateFree("ipconfig /all", isRealTarget: true).Ok);

    [Fact]
    public void Free_Real_EmptyPipelineSegment_Fails() =>
        Assert.False(RemoteCommandValidator.ValidateFree("Get-Process | ", isRealTarget: true).Ok);

    [Fact]
    public void Free_Real_CommandNamesAreCaseInsensitive() =>
        Assert.True(RemoteCommandValidator.ValidateFree("get-process | select-object -First 3 Name", isRealTarget: true).Ok);
}