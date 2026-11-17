using BtPlatform.Core.Entities;
using BtPlatform.Core.Services;
using Xunit;

namespace BtPlatform.Tests;

public class EventJsonParserTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    private static string One(string log = "System", string level = "Error", string eventId = "7034",
        string source = "Service Control Manager", string time = "2026-11-06T09:30:00Z",
        string message = "Servis beklenmedik şekilde sonlandı.", string recordId = "1001") =>
        $$"""{"log":"{{log}}","level":"{{level}}","eventId":{{eventId}},"source":"{{source}}","time":"{{time}}","message":"{{message}}","recordId":{{recordId}}}""";

    [Fact]
    public void Parse_ValidEvent_ReturnsIt()
    {
        var r = EventJsonParser.Parse("[" + One() + "]", Now);
        Assert.True(r.Ok);
        var e = Assert.Single(r.Events);
        Assert.Equal("System", e.LogName);
        Assert.Equal(PcEventLevel.Error, e.Level);
        Assert.Equal(7034, e.EventId);
        Assert.Equal("Service Control Manager", e.Source);
        Assert.Equal(1001, e.RecordId);
        Assert.Equal(new DateTime(2026, 11, 6, 9, 30, 0), e.OccurredAtUtc);
    }

    [Fact]
    public void Parse_EmptyOutput_MeansNoEvents()
    {
        var r = EventJsonParser.Parse("  ", Now);
        Assert.True(r.Ok);
        Assert.Empty(r.Events);
    }

    [Fact]
    public void Parse_EmptyArray_MeansNoEvents()
    {
        var r = EventJsonParser.Parse("[]", Now);
        Assert.True(r.Ok);
        Assert.Empty(r.Events);
    }

    [Fact]
    public void Parse_InvalidJson_ReturnsError() =>
        Assert.False(EventJsonParser.Parse("{ bozuk", Now).Ok);

    [Fact]
    public void Parse_TooLargeOutput_ReturnsError() =>
        Assert.False(EventJsonParser.Parse(new string('a', 300_000), Now).Ok);

    [Fact]
    public void Parse_SingleObject_IsTreatedAsList()
    {
        var r = EventJsonParser.Parse(One(), Now);
        Assert.True(r.Ok);
        Assert.Single(r.Events);
    }

    [Theory]
    [InlineData("Information")]
    [InlineData("Verbose")]
    [InlineData("")]
    [InlineData("Whatever")]
    public void Parse_NonErrorLevels_AreSkipped(string level)
    {
        var r = EventJsonParser.Parse("[" + One(level: level) + "]", Now);
        Assert.True(r.Ok);
        Assert.Empty(r.Events);
    }

    [Theory]
    [InlineData("critical", PcEventLevel.Critical)]
    [InlineData("ERROR", PcEventLevel.Error)]
    [InlineData("Warning", PcEventLevel.Warning)]
    public void Parse_Levels_AreCaseInsensitive(string level, PcEventLevel expected)
    {
        var r = EventJsonParser.Parse("[" + One(level: level) + "]", Now);
        Assert.Equal(expected, Assert.Single(r.Events).Level);
    }

    [Theory]
    [InlineData("Security")]
    [InlineData("Setup")]
    [InlineData("")]
    public void Parse_UnknownLog_IsSkipped(string log) =>
        Assert.Empty(EventJsonParser.Parse("[" + One(log: log) + "]", Now).Events);

    [Theory]
    [InlineData("-1")]
    [InlineData("70000")]
    public void Parse_EventIdOutOfRange_IsSkipped(string id) =>
        Assert.Empty(EventJsonParser.Parse("[" + One(eventId: id) + "]", Now).Events);

    [Fact]
    public void Parse_MissingSource_IsSkipped() =>
        Assert.Empty(EventJsonParser.Parse("[" + One(source: "") + "]", Now).Events);

    [Fact]
    public void Parse_BadTime_IsSkipped() =>
        Assert.Empty(EventJsonParser.Parse("[" + One(time: "dun") + "]", Now).Events);

    [Fact]
    public void Parse_FutureTime_IsSkipped() =>
        Assert.Empty(EventJsonParser.Parse("[" + One(time: "2026-11-07T10:00:00Z") + "]", Now).Events);

    [Fact]
    public void Parse_TooOldTime_IsSkipped() =>
        Assert.Empty(EventJsonParser.Parse("[" + One(time: "2024-01-01T00:00:00Z") + "]", Now).Events);

    [Fact]
    public void Parse_LongMessage_IsTruncatedTo500()
    {
        var r = EventJsonParser.Parse("[" + One(message: new string('x', 900)) + "]", Now);
        Assert.Equal(EventJsonParser.MaxMessageChars, Assert.Single(r.Events).Message.Length);
    }

    [Fact]
    public void Parse_NewLinesInMessage_BecomeSpaces()
    {
        var r = EventJsonParser.Parse("[" + One(message: "satir1\\nsatir2") + "]", Now);
        Assert.Equal("satir1 satir2", Assert.Single(r.Events).Message);
    }

    [Fact]
    public void Parse_BadRecordIsSkipped_GoodOnesKept()
    {
        var json = "[" + One(recordId: "1") + "," + One(level: "Information", recordId: "2") + "," + One(recordId: "3") + "]";
        var r = EventJsonParser.Parse(json, Now);
        Assert.True(r.Ok);
        Assert.Equal(new long[] { 1, 3 }, r.Events.Select(e => e.RecordId).ToArray());
    }

    [Fact]
    public void Parse_MoreThan50Events_IsCapped()
    {
        var items = Enumerable.Range(1, 80).Select(i => One(recordId: i.ToString()));
        var r = EventJsonParser.Parse("[" + string.Join(",", items) + "]", Now);
        Assert.Equal(EventJsonParser.MaxEvents, r.Events.Count);
    }

    [Fact]
    public void Parse_LeadingBom_IsTrimmed() =>
        Assert.Single(EventJsonParser.Parse("\uFEFF[" + One() + "]", Now).Events);
}
