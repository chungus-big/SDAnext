using System.Text.Json.Serialization;

namespace SteamLib.Authentication.LoginV2;

public class TransferResult
{
    [JsonPropertyName("result")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? Result { get; set; }
}
