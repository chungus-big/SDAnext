using System.Text.Json.Serialization;
using SteamLib.Web.Serialization.Converters;

namespace SteamLib.Models.Account;

public class WebTradeEligibility
{
    [JsonPropertyName("allowed")]
    public int Allowed { get; set; }

    /// <summary>
    /// Restriction reason code from Steam API (e.g. 16416 = SteamGuard required days, 16672 = Password reset cooldown).
    /// </summary>
    [JsonPropertyName("reason")]
    public int Reason { get; set; }

    [JsonPropertyName("allowed_at_time")]
    public long AllowedAtTime { get; set; }

    [JsonPropertyName("steamguard_required_days")]
    public int SteamGuardRequiredDays { get; set; }

    [JsonPropertyName("new_device_cooldown_days")]
    public int NewDeviceCooldownDays { get; set; }

    [JsonPropertyName("expiration")]
    [JsonConverter(typeof(UnixTimeStampJsonConverter))]
    public UnixTimeStamp Expiration { get; set; }

    [JsonPropertyName("time_checked")]
    [JsonConverter(typeof(UnixTimeStampJsonConverter))]
    public UnixTimeStamp TimeChecked { get; set; }
}