using System.Text.Json.Serialization;

namespace SteamLib.SteamMobile;

public class TimeQueryResponse
{
    [JsonPropertyName("response")]
    public TimeQueryInnerResponse? Response { get; set; }
}

public class TimeQueryInnerResponse
{
    [JsonPropertyName("server_time")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long ServerTime { get; set; }
}
