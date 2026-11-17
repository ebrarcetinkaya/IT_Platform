using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class WingetOutputParserTests
{
    // Gerçek winget çıktısındaki sütun genişlikleri
    private static string Row(string name, string id, string ver, string avail, string src) =>
        name.PadRight(54) + id.PadRight(38) + ver.PadRight(29) + avail.PadRight(24) + src;

    private static string Table(string[] header, params string[] rows)
    {
        var lines = new List<string>
        {
            "The `msstore` source requires that you view the following agreements before using.",
            "Terms of Transaction: https://aka.ms/microsoft-store-terms-of-transaction",
            "",
            Row(header[0], header[1], header[2], header[3], header[4]),
            new string('-', 135)
        };
        lines.AddRange(rows);
        lines.Add($"{rows.Length} upgrades available.");
        lines.Add("1 package(s) have version numbers that cannot be determined. Use --include-unknown to see all results.");
        return string.Join("\n", lines);
    }

    private static readonly string[] En = { "Name", "Id", "Version", "Available", "Source" };
    private static readonly string[] Tr = { "Ad", "Kimlik", "Sürüm", "Kullanılabilir", "Kaynak" };

    private static readonly string[] Sample =
    {
        Row("Adobe Creative Cloud", "Adobe.CreativeCloud", "6.10.0.252.41", "6.11.0.478.6", "winget"),
        Row("Git", "Git.Git", "2.54.0", "2.56.0.4", "winget"),
        Row("Microsoft .NET SDK 10.0.100-preview.4.25258.110 (x64)", "Microsoft.DotNet.SDK.Preview",
            "10.0.100-preview.4.25258.110", "11.0.100-rc.3.26522.148", "winget"),
        Row("NVIDIA PhysX Sistem Yazılımı 9.23.1019", "Nvidia.PhysX", "9.23.1019", "9.26.0703", "winget"),
        Row("SQL Server için Microsoft OLE DB Sürücüsü 19", "Microsoft.SQLServer.OLEDBDriver", "19.4.1.0", "19.4.3", "winget")
    };

    [Fact]
    public void Parse_RealWingetOutput_ParsesAllRows()
    {
        var r = WingetOutputParser.Parse(Table(En, Sample));

        Assert.True(r.TableFound);
        Assert.Equal(5, r.Items.Count);
        Assert.Equal("Adobe Creative Cloud", r.Items[0].Name);
        Assert.Equal("Adobe.CreativeCloud", r.Items[0].Id);
        Assert.Equal("6.10.0.252.41", r.Items[0].CurrentVersion);
        Assert.Equal("6.11.0.478.6", r.Items[0].AvailableVersion);
        Assert.Equal("winget", r.Items[0].Source);
    }

    [Fact]
    public void Parse_NamesWithSpacesAndTurkishCharacters_AreKeptWhole()
    {
        var r = WingetOutputParser.Parse(Table(En, Sample));

        Assert.Equal("Microsoft .NET SDK 10.0.100-preview.4.25258.110 (x64)", r.Items[2].Name);
        Assert.Equal("11.0.100-rc.3.26522.148", r.Items[2].AvailableVersion);
        Assert.Equal("NVIDIA PhysX Sistem Yazılımı 9.23.1019", r.Items[3].Name);
        Assert.Equal("SQL Server için Microsoft OLE DB Sürücüsü 19", r.Items[4].Name);
    }

    [Fact]
    public void Parse_FooterLines_AreNotRows()
    {
        var r = WingetOutputParser.Parse(Table(En, Sample[0], Sample[1], Sample[2]));
        Assert.Equal(3, r.Items.Count);
        Assert.Equal("Microsoft.DotNet.SDK.Preview", r.Items[2].Id);
    }

    [Fact]
    public void Parse_TurkishHeaders_Work()
    {
        var r = WingetOutputParser.Parse(Table(Tr, Sample));
        Assert.True(r.TableFound);
        Assert.Equal(5, r.Items.Count);
        Assert.Equal("Git.Git", r.Items[1].Id);
    }

    [Fact]
    public void Parse_EmptyInput_NoTable()
    {
        var r = WingetOutputParser.Parse("  ");
        Assert.False(r.TableFound);
        Assert.Empty(r.Items);
    }

    [Fact]
    public void Parse_NoTableMessage_NoTable()
    {
        var r = WingetOutputParser.Parse("No installed package found matching input criteria.");
        Assert.False(r.TableFound);
        Assert.Empty(r.Items);
    }

    [Fact]
    public void Parse_WindowsLineEndingsAndProgressNoise_Work()
    {
        // İlerleme çubuğundan kalan tek \r'lar ve \r\n satır sonları
        var text = "  -  \r  \\  \r  |  \r" + Table(En, Sample).Replace("\n", "\r\n");
        var r = WingetOutputParser.Parse(text);

        Assert.True(r.TableFound);
        Assert.Equal(5, r.Items.Count);
        Assert.Equal("Adobe Creative Cloud", r.Items[0].Name);
    }

    [Fact]
    public void Parse_MoreThan100Rows_IsCapped()
    {
        var rows = Enumerable.Range(1, 120)
            .Select(i => Row($"App{i:000}", $"Pub.App{i:000}", "1.0", "2.0", "winget"))
            .ToArray();

        var r = WingetOutputParser.Parse(Table(En, rows));

        Assert.True(r.TableFound);
        Assert.Equal(WingetOutputParser.MaxItems, r.Items.Count);
    }

    [Fact]
    public void Parse_TooLargeInput_IsRejected()
    {
        var r = WingetOutputParser.Parse(new string('a', 300_000));
        Assert.False(r.TableFound);
        Assert.Empty(r.Items);
    }
}