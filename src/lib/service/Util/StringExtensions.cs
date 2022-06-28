using System.Globalization;
using System.Text;

namespace SunshineExpress.Service.Util;
internal static class StringExtensions
{
    /// <summary>
    /// Replaces all the diacrtics from the string.
    /// </summary>
    /// <param name="text">Text to remove the diacritics from.</param>
    /// <returns>The same <paramref name="text"/> without diacritics.</returns>
    /// <example>Klaipėda -> Klaipeda</example>
    public static string RemoveDiacritics(this string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(c);
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
