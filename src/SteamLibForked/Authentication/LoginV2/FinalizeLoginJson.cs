using System.Text.Json.Serialization;

namespace SteamLib.Authentication.LoginV2;

public class FinalizeLoginJson
{
    [JsonPropertyName("steamID")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong SteamId { get; set; }

    [JsonPropertyName("transfer_info")]
    public List<TransferInfo> TransferInfo { get; set; } = [];
}

public class TransferInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("params")]
    public TransferInfoParams TransferInfoParams { get; set; } = null!;
}

public class TransferInfoParams
{
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = null!;

    [JsonPropertyName("auth")]
    public string Auth { get; set; } = null!;
}