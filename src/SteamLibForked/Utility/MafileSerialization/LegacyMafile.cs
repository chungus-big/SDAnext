using System.Text.Json.Serialization;

namespace SteamLib.Utility.MafileSerialization;

internal class LegacySessionData
{
    [JsonPropertyName("AccessToken")] public string? AccessToken { get; set; }
    [JsonPropertyName("steamLoginSecure")] public string? SteamLoginSecure { get; set; }
    [JsonPropertyName("RefreshToken")] public string? RefreshToken { get; set; }
    [JsonPropertyName("SteamID")] public long? SteamId { get; set; }
    [JsonPropertyName("SessionID")] public string? SessionId { get; set; }
}

internal class LegacyMafile
{
    [JsonPropertyName("shared_secret")] public string SharedSecret { get; set; } = null!;

    [JsonPropertyName("identity_secret")] public string IdentitySecret { get; set; } = null!;

    [JsonPropertyName("device_id")] public string DeviceId { get; set; } = null!;

    [JsonPropertyName("revocation_code")] public string RevocationCode { get; set; } = null!;
    [JsonPropertyName("account_name")] public string AccountName { get; set; } = null!;
    [JsonPropertyName("Session")] public LegacySessionData? SessionData { get; set; }
    [JsonPropertyName("server_time")] public long ServerTime { get; set; } // Preserved for SDA format compatibility
    [JsonPropertyName("steamid")] public long SteamId { get; set; }
    [JsonPropertyName("serial_number")] public string SerialNumber { get; set; } = null!; // Preserved for SDA format compatibility
    [JsonPropertyName("uri")] public string Uri { get; set; } = null!; // Preserved for SDA format compatibility
    [JsonPropertyName("token_gid")] public string TokenGid { get; set; } = null!; // Preserved for SDA format compatibility
    [JsonPropertyName("secret_1")] public string Secret1 { get; set; } = null!; // Preserved for SDA format compatibility

    public override string ToString() =>
        $"LegacyMafile {{ AccountName = {AccountName}, SteamId = {SteamId}, DeviceId = {DeviceId}, SharedSecret = ****, IdentitySecret = ****, RevocationCode = **** }}";
}