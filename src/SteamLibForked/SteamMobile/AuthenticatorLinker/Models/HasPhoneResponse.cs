using System.Text.Json.Serialization;

namespace SteamLib.SteamMobile.AuthenticatorLinker.Models;

public class HasPhoneResponse
{
    [JsonPropertyName("has_phone")]
    public bool HasPhone { get; set; }
}
