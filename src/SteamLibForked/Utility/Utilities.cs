using System.Text.RegularExpressions;

namespace SteamLib.Utility;

/// <summary>
/// General helper utilities for parsing Steam API responses.
/// </summary>
public static partial class Utilities
{
    [GeneratedRegex("\"success\":\\s?(\\w*)", RegexOptions.Compiled)]
    private static partial Regex GetRegexBool();

    [GeneratedRegex("\"success\":\\s?(\\d*)", RegexOptions.Compiled)]
    private static partial Regex GetRegexInt();

    public static int GetSuccessCode(string response)
    {
        var length = Math.Min(response.Length, 100);
        var matchInt = GetRegexInt().Match(response, 0, length);
        if (!string.IsNullOrEmpty(matchInt.Groups[1].Value))
            return Convert.ToInt32(matchInt.Groups[1].Value);

        var matchBool = GetRegexBool().Match(response, 0, length);
        if (bool.TryParse(matchBool.Groups[1].Value, out var successBool))
            return successBool ? 1 : 0;

        return 0;
    }
}