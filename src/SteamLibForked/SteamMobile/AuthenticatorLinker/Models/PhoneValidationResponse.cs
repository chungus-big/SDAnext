using System.Text.Json.Serialization;

namespace SteamLib.SteamMobile.AuthenticatorLinker.Models;

public class PhoneValidationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("is_voip")]
    public bool IsVoip { get; set; }

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }
}
