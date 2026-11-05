using System.Text.Json.Serialization;

namespace SteamLib.Web.Models.Account;

public class UserWalletSchema
{
    [JsonPropertyName("amount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }
}