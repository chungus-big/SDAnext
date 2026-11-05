using System.Text.Json;
using System.Text.Json.Serialization;
using SteamLib.Core.StatusCodes;
using SteamLib.Models;
using SteamLib.Utility;
using SteamLib.Web;
using SteamLib.Web.Converters;
using Xunit;

namespace SteamLib.Tests;

public class UtilitiesReplacementTests
{
    private class TestSerializationModel
    {
        [JsonConverter(typeof(UnixTimeStampConverter))]
        public UnixTimeStamp Timestamp { get; set; }

        [JsonConverter(typeof(IntToStringConverter))]
        public int Amount { get; set; }
    }

    [Fact]
    public void UnixTimeStamp_FromDateTime_AndConversions_WorkAccurately()
    {
        var dtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ts = UnixTimeStamp.FromDateTime(dtUtc);

        Assert.Equal(1704067200L, ts.ToLong());
        Assert.Equal(dtUtc, ts.Time);
        Assert.Equal(dtUtc, ts.ToUtcDateTime());
        Assert.Equal(dtUtc.ToLocalTime(), ts.ToLocalDateTime());
        Assert.Equal("1704067200", ts.ToString());
    }

    [Fact]
    public void UnixTimeStamp_MillisecondsFormat_DividesCorrectly()
    {
        var ts = new UnixTimeStamp(1704067200000L, UnixFormat.Milliseconds);
        Assert.Equal(1704067200L, ts.ToLong());
    }

    [Fact]
    public void UnixTimeStamp_OperatorsAndTimeSpan_WorkProperly()
    {
        var ts1 = new UnixTimeStamp(1000L);
        var ts2 = ts1 + TimeSpan.FromSeconds(50);
        Assert.Equal(1050L, ts2.ToLong());

        var ts3 = ts2 - TimeSpan.FromSeconds(30);
        Assert.Equal(1020L, ts3.ToLong());

        var diff = ts2 - ts1;
        Assert.Equal(TimeSpan.FromSeconds(50), diff);

        Assert.True(ts1 < ts2);
        Assert.True(ts2 > ts1);
        Assert.True(ts1 <= ts2);
        Assert.True(ts2 >= ts1);
        Assert.True(ts1 == new UnixTimeStamp(1000L));
        Assert.True(ts1 != ts2);

        long implicitLong = ts1;
        Assert.Equal(1000L, implicitLong);

        UnixTimeStamp implicitTs = 2000L;
        Assert.Equal(2000L, implicitTs.ToLong());
    }

    [Fact]
    public void UnixTimeStampConverter_SystemTextJson_SerializesAndDeserializes()
    {
        var model = new TestSerializationModel
        {
            Timestamp = new UnixTimeStamp(1704067200L),
            Amount = 150
        };

        var json = JsonSerializer.Serialize(model);
        Assert.Contains("\"Timestamp\":1704067200", json);
        Assert.Contains("\"Amount\":\"150\"", json);

        var deserialized = JsonSerializer.Deserialize<TestSerializationModel>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(1704067200L, deserialized.Timestamp.ToLong());
        Assert.Equal(150, deserialized.Amount);

        // Deserialization from string timestamp and integer amount
        var jsonFromString = "{\"Timestamp\":\"1704067200\",\"Amount\":250}";
        var deserialized2 = JsonSerializer.Deserialize<TestSerializationModel>(jsonFromString);
        Assert.NotNull(deserialized2);
        Assert.Equal(1704067200L, deserialized2.Timestamp.ToLong());
        Assert.Equal(250, deserialized2.Amount);
    }

    [Fact]
    public void Enumeration_SteamStatusCode_LookupsAndComparisons_Work()
    {
        var ok = SteamStatusCode.FromId(1);
        Assert.NotNull(ok);
        Assert.Equal("Ok", ok.Name);
        Assert.Equal(1, ok.Id);
        Assert.Equal(SteamStatusCode.Ok, ok);

        var okByName = SteamStatusCode.FromName("ok");
        Assert.NotNull(okByName);
        Assert.Equal(SteamStatusCode.Ok, okByName);

        var nullByName = SteamStatusCode.FromName(null);
        Assert.Null(nullByName);

        var unknown = SteamStatusCode.FromId(99999);
        Assert.Null(unknown);

        var all = SteamStatusCode.GetAll();
        Assert.Contains(SteamStatusCode.Ok, all);
        Assert.Contains(SteamStatusCode.Fail, all);
    }

    [Fact]
    public void HttpExtensions_ToQueryString_EncodesProperly()
    {
        Assert.Equal(string.Empty, HttpExtensions.ToQueryString(null));
        Assert.Equal(string.Empty, new List<KeyValuePair<string, string>>().ToQueryString());

        var list = new List<KeyValuePair<string, string>>
        {
            new("op", "allow"),
            new("cid", "12345"),
            new("detail", "hello world & more")
        };

        var query = list.ToQueryString();
        Assert.Equal("?op=allow&cid=12345&detail=hello%20world%20%26%20more", query);

        // Test non-IList IEnumerable and null values
        IEnumerable<KeyValuePair<string, string>> nonListSeq = list.Where(x => x.Key != "cid");
        var filteredQuery = nonListSeq.ToQueryString();
        Assert.Equal("?op=allow&detail=hello%20world%20%26%20more", filteredQuery);
    }

    [Fact]
    public void StringExtensions_EqualsAndContains_IgnoreCase()
    {
        Assert.True("MaFiLe".EqualsIgnoreCase("mafile"));
        Assert.True("test.mafile".ContainsIgnoreCase("MAFILE"));
        Assert.False("test.txt".ContainsIgnoreCase("mafile"));
        Assert.False("abc".EqualsIgnoreCase("def"));

        string? nullStr = null;
        Assert.True(nullStr.EqualsIgnoreCase(null));
        Assert.False(nullStr.EqualsIgnoreCase("abc"));
        Assert.False(nullStr.ContainsIgnoreCase("abc"));
        Assert.False("abc".ContainsIgnoreCase(null));
    }

    [Fact]
    public void HttpClientHandlerPair_RecordStruct_Works()
    {
        using var handler = new HttpClientHandler();
        using var client = new HttpClient(handler);
        var pair = new HttpClientHandlerPair(client, handler);

        Assert.Same(client, pair.Client);
        Assert.Same(handler, pair.Handler);

        // Verify default struct Dispose does not throw NRE
        var defaultPair = default(HttpClientHandlerPair);
        defaultPair.Dispose();
    }
}
