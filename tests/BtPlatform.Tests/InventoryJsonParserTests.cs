using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class InventoryJsonParserTests
{
    // Geçerli bir JSON üretir; tek tek alanları bozarak test ederiz
    private static string Make(string hostname = "PC-TEST-01", string ip = "192.168.1.20",
        string mac = "AA-BB-CC-DD-EE-FF", string ram = "16", string apps = "[]") =>
        $$"""
        {
          "hostname": "{{hostname}}",
          "manufacturer": "Dell",
          "model": "Latitude 5440",
          "serialNumber": "SN12345",
          "processor": "Intel Core i5",
          "ramGb": {{ram}},
          "diskGb": 512,
          "osName": "Microsoft Windows 11 Pro",
          "ipAddress": "{{ip}}",
          "macAddress": "{{mac}}",
          "apps": {{apps}}
        }
        """;

    [Fact]
    public void Parse_ValidJson_ReturnsSnapshot()
    {
        var r = InventoryJsonParser.Parse(Make());
        Assert.True(r.Ok);
        Assert.Equal("PC-TEST-01", r.Snapshot!.Hostname);
        Assert.Equal(16, r.Snapshot.RamGb);
        Assert.Equal(512, r.Snapshot.DiskGb);
        Assert.Equal("192.168.1.20", r.Snapshot.IpAddress);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsError()
    {
        var r = InventoryJsonParser.Parse("   ");
        Assert.False(r.Ok);
        Assert.NotNull(r.Error);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsError()
    {
        var r = InventoryJsonParser.Parse("{ bu json degil");
        Assert.False(r.Ok);
    }

    [Fact]
    public void Parse_MissingHostname_ReturnsError()
    {
        var r = InventoryJsonParser.Parse(Make(hostname: ""));
        Assert.False(r.Ok);
        Assert.Contains("hostname", r.Error);
    }

    [Fact]
    public void Parse_InvalidIp_ReturnsError()
    {
        var r = InventoryJsonParser.Parse(Make(ip: "999.1.1.1"));
        Assert.False(r.Ok);
    }

    [Fact]
    public void Parse_InvalidMac_ReturnsError()
    {
        var r = InventoryJsonParser.Parse(Make(mac: "AA-BB"));
        Assert.False(r.Ok);
    }

    [Fact]
    public void Parse_NonPositiveRam_ReturnsError()
    {
        var r = InventoryJsonParser.Parse(Make(ram: "0"));
        Assert.False(r.Ok);
    }

    [Fact]
    public void Parse_DecimalRam_IsRounded()
    {
        var r = InventoryJsonParser.Parse(Make(ram: "15.8"));
        Assert.True(r.Ok);
        Assert.Equal(16, r.Snapshot!.RamGb);
    }

    [Fact]
    public void Parse_MoreThan15Apps_KeepsFirst15SortedByName()
    {
        var items = Enumerable.Range(1, 20)
            .Select(i => $$"""{"name":"App{{i:00}}","version":"1.0","publisher":"X"}""");
        var r = InventoryJsonParser.Parse(Make(apps: "[" + string.Join(",", items) + "]"));

        Assert.True(r.Ok);
        Assert.Equal(15, r.Snapshot!.Apps.Count);
        Assert.Equal("App01", r.Snapshot.Apps[0].Name);
        Assert.Equal("App15", r.Snapshot.Apps[14].Name);
    }

    [Fact]
    public void Parse_DuplicateAppNames_KeepsOne()
    {
        var apps = """[{"name":"7-Zip","version":"23.01"},{"name":"7-ZIP","version":"22.00"}]""";
        var r = InventoryJsonParser.Parse(Make(apps: apps));
        Assert.True(r.Ok);
        Assert.Single(r.Snapshot!.Apps);
    }

    [Fact]
    public void Parse_SingleAppObject_IsTreatedAsList()
    {
        var apps = """{"name":"Google Chrome","version":"126.0","publisher":"Google"}""";
        var r = InventoryJsonParser.Parse(Make(apps: apps));
        Assert.True(r.Ok);
        Assert.Single(r.Snapshot!.Apps);
        Assert.Equal("Google Chrome", r.Snapshot.Apps[0].Name);
    }

    [Fact]
    public void Parse_NoAppsProperty_ReturnsEmptyList()
    {
        var json = """
        {"hostname":"PC-X","ramGb":8,"diskGb":256,"osName":"Windows 11",
         "ipAddress":"10.0.0.5","macAddress":"AABBCCDDEEFF"}
        """;
        var r = InventoryJsonParser.Parse(json);
        Assert.True(r.Ok);
        Assert.Empty(r.Snapshot!.Apps);
    }

    [Fact]
    public void Parse_BlankAppNames_AreSkipped()
    {
        var apps = """[{"name":"","version":"1"},{"name":"  ","version":"2"},{"name":"Notepad++","version":"8"}]""";
        var r = InventoryJsonParser.Parse(Make(apps: apps));
        Assert.True(r.Ok);
        Assert.Single(r.Snapshot!.Apps);
    }

    [Fact]
    public void Parse_TooLongHostname_IsTruncatedTo63()
    {
        var r = InventoryJsonParser.Parse(Make(hostname: new string('A', 100)));
        Assert.True(r.Ok);
        Assert.Equal(63, r.Snapshot!.Hostname.Length);
    }

    [Fact]
    public void Parse_TooLargeOutput_ReturnsError()
    {
        var r = InventoryJsonParser.Parse(new string('a', 300_000));
        Assert.False(r.Ok);
    }
}