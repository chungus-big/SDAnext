using System.Text.Json.Serialization;

namespace SteamLib.Web.Models.Account;

public class WalletInfoSchema
{
    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("alternate_min_amount")]
    public bool AlternateMinAmount { get; set; }

    [JsonPropertyName("amounts")]
    public List<int>? Amounts { get; set; }

    [JsonPropertyName("related_trans_type")]
    public bool RelatedTransType { get; set; }

    [JsonPropertyName("related_trainsid")]
    public bool RelatedTrainsId { get; set; }

    [JsonPropertyName("user_wallet")]
    public UserWalletSchema? UserWalletSchema { get; set; }
}