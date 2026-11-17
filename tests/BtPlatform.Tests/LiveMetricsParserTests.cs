using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class LiveMetricsParserTests
{
    private static string Make(string cpu = "12.5", string ramTotal = "16", string ramUsed = "9.8",
        string diskTotal = "476", string diskFree = "210", string uptime = "5.2") =>
        $$"""{"cpuPercent":{{cpu}},"ramTotalGb":{{ramTotal}},"ramUsedGb":{{ramUsed}},"diskTotalGb":{{diskTotal}},"diskFreeGb":{{diskFree}},"uptimeHours":{{uptime}}}""";

    [Fact]
    public void Parse_ValidJson_ReturnsMetrics()
    {
        var r = LiveMetricsParser.Parse(Make());
        Assert.True(r.Ok);
        Assert.Equal(12.5, r.Metrics!.CpuPercent);
        Assert.Equal(9.8, r.Metrics.RamUsedGb);
        Assert.Equal(210, r.Metrics.DiskFreeGb);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsError() => Assert.False(LiveMetricsParser.Parse("  ").Ok);

    [Fact]
    public void Parse_InvalidJson_ReturnsError() => Assert.False(LiveMetricsParser.Parse("{ bozuk").Ok);

    [Fact]
    public void Parse_MissingField_ReturnsError() =>
        Assert.False(LiveMetricsParser.Parse("""{"cpuPercent":10,"ramTotalGb":16}""").Ok);

    [Fact]
    public void Parse_CpuOver100_ReturnsError() => Assert.False(LiveMetricsParser.Parse(Make(cpu: "140")).Ok);

    [Fact]
    public void Parse_RamUsedGreaterThanTotal_ReturnsError() =>
        Assert.False(LiveMetricsParser.Parse(Make(ramUsed: "30")).Ok);

    [Fact]
    public void Parse_DiskFreeGreaterThanTotal_ReturnsError() =>
        Assert.False(LiveMetricsParser.Parse(Make(diskFree: "900")).Ok);

    [Fact]
    public void Parse_NegativeUptime_ReturnsError() => Assert.False(LiveMetricsParser.Parse(Make(uptime: "-1")).Ok);

    [Fact]
    public void Parse_LeadingBom_IsTrimmed() => Assert.True(LiveMetricsParser.Parse("\uFEFF" + Make()).Ok);

    [Fact]
    public void Parse_TooLargeOutput_ReturnsError() =>
        Assert.False(LiveMetricsParser.Parse(new string('a', 5000)).Ok);
}