using System.Globalization;

namespace NextAuth.Core.Utility;

public static class LanguageUtility
{
    public static LocalizationLanguage DetectPreferredLanguage()
    {
        var userCulture = CultureInfo.CurrentUICulture;
        var userLang = userCulture.TwoLetterISOLanguageName;

        switch (userLang)
        {
            case "ru" or "uk" or "be" or "kk": return LocalizationLanguage.Russian;
            case "en": return LocalizationLanguage.English;
        }

        string? userRegion = null;
        try
        {
            if (!userCulture.IsNeutralCulture && !string.IsNullOrWhiteSpace(userCulture.Name))
            {
                userRegion = new RegionInfo(userCulture.Name).TwoLetterISORegionName;
            }
            else
            {
                userRegion = RegionInfo.CurrentRegion?.TwoLetterISORegionName;
            }
        }
        catch
        {
            try
            {
                userRegion = RegionInfo.CurrentRegion?.TwoLetterISORegionName;
            }
            catch
            {
                // Fallback if region cannot be resolved
            }
        }

        string[] cisRegions =
        [
            "RU", "BY", "KZ", "KG", "TJ", "TM", "UZ", "AM", "AZ", "GE", "MD"
        ];

        return userRegion != null && cisRegions.Any(r => userRegion.Equals(r, StringComparison.OrdinalIgnoreCase))
            ? LocalizationLanguage.Russian
            : LocalizationLanguage.English;
    }
}