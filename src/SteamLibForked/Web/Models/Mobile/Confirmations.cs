using System.Text.Json.Serialization;
using SteamLib.Models;
using SteamLib.SteamMobile.Confirmations;
using SteamLib.Web.Serialization.Converters;

namespace SteamLib.Web.Models.Mobile;

public class ConfirmationsJson
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("needauth")]
    public bool NeedAuth { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("conf")]
    public List<ConfirmationJson> Conf { get; set; } = [];
}

public class ConfirmationJson
{
    [JsonPropertyName("type")]
    public ConfirmationType Type { get; set; }

    [JsonPropertyName("type_name")]
    public string TypeName { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Id { get; set; }

    [JsonPropertyName("creator_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong CreatorId { get; set; }

    [JsonPropertyName("nonce")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong Nonce { get; set; }

    [JsonPropertyName("creation_time")]
    [JsonConverter(typeof(UnixTimeStampJsonConverter))]
    public UnixTimeStamp CreationTime { get; set; }

    [JsonPropertyName("cancel")]
    public string Cancel { get; set; } = string.Empty;

    [JsonPropertyName("accept")]
    public string Confirm { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public Uri? Icon { get; set; }

    [JsonPropertyName("multi")]
    public bool Multi { get; set; }

    [JsonPropertyName("headline")]
    public string Headline { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public List<string> Summary { get; set; } = [];

    [JsonPropertyName("warn")]
    public List<string>? Warn { get; set; } = [];
}