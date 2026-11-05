using SteamLib.Models;
using SteamLib.Abstractions;

namespace SteamLib.SteamMobile;

/// <summary>
/// Synchronizes local time with Steam server time for accurate TOTP code generation.
/// Static facade delegating to <see cref="Default"/> implementation of <see cref="ISteamTimeAligner"/>.
/// </summary>
public static class TimeAligner
{
    private static ISteamTimeAligner _default = new SteamTimeAligner();

    /// <summary>
    /// Gets or sets the default <see cref="ISteamTimeAligner"/> instance.
    /// </summary>
    public static ISteamTimeAligner Default
    {
        get => _default;
        set => _default = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static bool IsAligned => Default.IsAligned;
    public static int TimeDifference => Default.TimeDifference;
    public static UnixTimeStamp UtcNow => UnixTimeStamp.FromDateTime(DateTime.UtcNow);

    public static ValueTask<long> GetSteamTimeAsync(CancellationToken ct = default)
    {
        return Default.GetSteamTimeAsync(ct);
    }

    [Obsolete("Use GetSteamTimeAsync instead to prevent synchronous blocking of threads.")]
    public static long GetSteamTime()
    {
        return Default.GetSteamTime();
    }

    [Obsolete("Use AlignTimeAsync instead to prevent synchronous blocking of threads.")]
    public static void AlignTime()
    {
        Default.AlignTime();
    }

    public static Task AlignTimeAsync(CancellationToken ct = default)
    {
        return Default.AlignTimeAsync(ct);
    }

    public static void SetTimeDifference(int timeDifference)
    {
        Default.SetTimeDifference(timeDifference);
    }
}