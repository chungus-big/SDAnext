using System.Text.Json.Serialization;
using SteamLib.Web.Serialization.Converters;

namespace SteamLib.Models;

public class ScriptHeaderModel
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; set; }

    [JsonPropertyName("steamId")]
    [JsonConverter(typeof(SteamIdJsonConverter))]
    public SteamId? SteamId { get; set; }
}