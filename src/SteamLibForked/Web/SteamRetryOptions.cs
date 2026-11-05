namespace SteamLib.Web;

/// <summary>
/// Configuration options for <see cref="SteamRetryHandler"/>.
/// </summary>
public class SteamRetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts for transient errors. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial backoff delay before the first retry. Default is 1 second.
    /// </summary>
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries. Default is 30 seconds.
    /// </summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to retry network and timeout errors on non-idempotent HTTP methods (such as POST or PATCH).
    /// Default is true (safe for Steam mobile confirmations, time queries, and status updates).
    /// Set to false if executing operations where re-executing a partially processed request is risky (e.g. trade offers).
    /// Note: HTTP error status codes (429, 502, 503, 504) are always retried because the server rejected the request.
    /// </summary>
    public bool RetryNetworkErrorsOnNonIdempotentMethods { get; set; } = true;

    public static readonly SteamRetryOptions Default = new();
}
