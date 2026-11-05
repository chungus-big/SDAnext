namespace SteamLib.Utility;

public static class StringExtensions
{
    public static bool EqualsIgnoreCase(this string? str, string? other) =>
        string.Equals(str, other, StringComparison.OrdinalIgnoreCase);

    public static bool ContainsIgnoreCase(this string? str, string? value) =>
        str is not null && value is not null && str.Contains(value, StringComparison.OrdinalIgnoreCase);
}
