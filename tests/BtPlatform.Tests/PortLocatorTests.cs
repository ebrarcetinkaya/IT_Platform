using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class PortLocatorTests
{
    private static SwitchPort Port(string sw, int number, string? mac, int vlan = 10,
        PortStatus status = PortStatus.Up) =>
        new() { Switch = new NetworkSwitch { Name = sw }, Number = number, LearnedMacHash = mac, Vlan = vlan, Status = status };

    [Fact]
    public void Find_MatchingMac_ReturnsSwitchAndPort()
    {
        var ports = new[] { Port("SW-KAT1", 3, "aaa"), Port("SW-KAT1", 4, "bbb", vlan: 20) };

        var r = PortLocator.Find("bbb", ports);

        Assert.NotNull(r);
        Assert.Equal("SW-KAT1", r!.SwitchName);
        Assert.Equal(4, r.PortNumber);
        Assert.Equal(20, r.Vlan);
        Assert.Equal("SW-KAT1 Gi1/0/4", r.Label);
    }

    [Fact]
    public void Find_NoMatch_ReturnsNull() =>
        Assert.Null(PortLocator.Find("zzz", new[] { Port("SW-KAT1", 1, "aaa") }));

    [Fact]
    public void Find_NullOrEmptyHash_ReturnsNull()
    {
        var ports = new[] { Port("SW-KAT1", 1, null), Port("SW-KAT1", 2, "") };
        Assert.Null(PortLocator.Find(null, ports));
        Assert.Null(PortLocator.Find("", ports));
        Assert.Null(PortLocator.Find("  ", ports));
    }

    [Fact]
    public void Find_EmptyPortsDoNotMatchEmptyHash()
    {
        // Boş portlar (LearnedMacHash null) hiçbir bilgisayara bağlanmamalı
        Assert.Null(PortLocator.Find("", new[] { Port("SW-KAT1", 1, null) }));
    }

    [Fact]
    public void Find_IsCaseInsensitive() =>
        Assert.NotNull(PortLocator.Find("ABC123", new[] { Port("SW-KAT2", 5, "abc123") }));

    [Fact]
    public void Find_SameMacOnUplinkAndUserPort_PrefersUserPort()
    {
        var ports = new[] { Port("SW-CORE-1", 23, "aaa"), Port("SW-KAT2", 7, "aaa") };

        var r = PortLocator.Find("aaa", ports);

        Assert.Equal("SW-KAT2", r!.SwitchName);
        Assert.Equal(7, r.PortNumber);
    }

    [Fact]
    public void Find_OnlyUplinkMatch_ReturnsUplink() =>
        Assert.Equal(24, PortLocator.Find("aaa", new[] { Port("SW-CORE-1", 24, "aaa") })!.PortNumber);

    [Fact]
    public void Find_ReturnsPortStatus() =>
        Assert.Equal(PortStatus.ErrDisabled,
            PortLocator.Find("aaa", new[] { Port("SW-KAT1", 9, "aaa", status: PortStatus.ErrDisabled) })!.Status);
}