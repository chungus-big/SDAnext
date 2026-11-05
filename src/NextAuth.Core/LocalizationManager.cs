using System.Globalization;
using NextAuth.Core.Resources;

namespace NextAuth.Core;

public static class LocManager
{
    public const string CODE_BEHIND_PATH_PART = "CodeBehind";
    public const string COMMON_PATH_PART = "Common";

    private static LocalizationLanguage _currentLanguage = LocalizationLanguage.English;

    public static event EventHandler? CurrentLanguageChanged;

    public static void SetApplicationLocalization(LocalizationLanguage language)
    {
        _currentLanguage = language;
        var code = GetLanguageCode(language);
        var culture = CultureInfo.GetCultureInfo(code);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Strings.Culture = culture;

        CurrentLanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string GetCurrentLanguageCode()
    {
        return GetLanguageCode(_currentLanguage);
    }

    public static string GetLanguageCode(LocalizationLanguage language)
    {
        return language switch
        {
            LocalizationLanguage.English => "en",
            LocalizationLanguage.Russian => "ru",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }

    public static void Init()
    {
        SetApplicationLocalization(_currentLanguage);
    }

    public static string? GetCodeBehind(params string[] path)
    {
        return GetConcat(CODE_BEHIND_PATH_PART, path);
    }

    public static string GetCodeBehindOrDefault(string def, params string[] path)
    {
        return GetCodeBehind(path) ?? def;
    }

    public static string? GetCommon(params string[] path)
    {
        return GetConcat(COMMON_PATH_PART, path);
    }

    public static string GetCommonOrDefault(string def, params string[] path)
    {
        return GetCommon(path) ?? def;
    }

    public static string? Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        var normalizedKey = key.Contains('.') ? key.Replace('.', '_') : key;
        var val = Strings.ResourceManager.GetString(normalizedKey, Strings.Culture);
        if (val != null) return val;

        if (normalizedKey != key)
        {
            val = Strings.ResourceManager.GetString(key, Strings.Culture);
            if (val != null) return val;
        }

        return null;
    }

    public static string? Get(params string[] path)
    {
        if (path == null || path.Length == 0)
            return null;

        var key = string.Join('_', path);
        var val = Strings.ResourceManager.GetString(key, Strings.Culture);
        if (val != null) return val;

        var dottedKey = string.Join('.', path);
        return Strings.ResourceManager.GetString(dottedKey, Strings.Culture);
    }

    public static string GetOrDefault(string def, params string[] path)
    {
        return Get(path) ?? def;
    }

    private static string? GetConcat(string first, string[]? path)
    {
        if (path == null || path.Length == 0)
            return Get(first);

        var newArray = new string[path.Length + 1];
        newArray[0] = first;
        Array.Copy(path, 0, newArray, 1, path.Length);
        return Get(newArray);
    }
}

public enum LocalizationLanguage
{
    English,
    Russian
}
