using System.Globalization;

namespace Schoolera.Application.Common;

/// <summary>
/// Picks the display value for bilingual content based on the current UI culture.
/// </summary>
public static class LocalizationDisplayHelper
{
    public static string Pick(string? nameAr, string? nameEn)
    {
        var useArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        if (useArabic)
        {
            return string.IsNullOrWhiteSpace(nameAr) ? nameEn ?? string.Empty : nameAr;
        }

        return string.IsNullOrWhiteSpace(nameEn) ? nameAr ?? string.Empty : nameEn;
    }
}
