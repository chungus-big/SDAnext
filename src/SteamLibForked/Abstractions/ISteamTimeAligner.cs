namespace SteamLib.Abstractions;

/// <summary>
/// Service for synchronizing local system time with Steam server time.
/// </summary>
public interface ISteamTimeAligner
{
    /// <summary>
    /// Gets a value indicating whether time has been aligned with Steam servers.
    /// </summary>
    bool IsAligned { get; }

    /// <summary>
    /// Gets the difference in seconds between local system time and Steam server time.
    /// </summary>
    int TimeDifference { get; }

    /// <summary>
    /// Asynchronously gets the current aligned Steam Unix timestamp.
    /// </summary>
    ValueTask<long> GetSteamTimeAsync(CancellationToken ct = default);

    /// <summary>
    /// Asynchronously aligns time with Steam server if not aligned yet or cooldown expired.
    /// </summary>
    Task AlignTimeAsync(CancellationToken ct = default);

    /// <summary>
    /// Synchronously aligns time with Steam server.
    /// </summary>
    void AlignTime();

    /// <summary>
    /// Synchronously gets the current aligned Steam Unix timestamp.
    /// </summary>
    long GetSteamTime();

    /// <summary>
    /// Explicitly sets the time difference in seconds.
    /// </summary>
    void SetTimeDifference(int timeDifference);
}
