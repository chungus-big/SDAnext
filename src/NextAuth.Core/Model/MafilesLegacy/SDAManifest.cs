using System.Text.Json.Serialization;

namespace NextAuth.Core.Model.MafilesLegacy;

public class SDAManifest
{
    [JsonPropertyName("encrypted")] public bool Encrypted { get; set; }

    [JsonPropertyName("first_run")] public bool FirstRun { get; set; }

    [JsonPropertyName("entries")] public SDAManifestEntry[] Entries { get; set; } = [];

    [JsonPropertyName("periodic_checking")] public bool PeriodicChecking { get; set; }

    [JsonPropertyName("periodic_checking_interval")]
    public long PeriodicCheckingInterval { get; set; }

    [JsonPropertyName("periodic_checking_checkall")]
    public bool PeriodicCheckingCheckAll { get; set; }

    [JsonPropertyName("auto_confirm_market_transactions")]
    public bool AutoConfirmMarketTransactions { get; set; }

    [JsonPropertyName("auto_confirm_trades")] public bool AutoConfirmTrades { get; set; }
}

public class SDAManifestEntry
{
    [JsonPropertyName("encryption_iv")] public string EncryptionIv { get; set; } = string.Empty;

    [JsonPropertyName("encryption_salt")] public string EncryptionSalt { get; set; } = string.Empty;

    [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("steamid")] public ulong SteamId { get; set; }
}