using System.Globalization;
using System.Text;

namespace WiktiView.Web.Search;

/// <summary>
/// Produces search keys that ignore case and diacritics, so that "Hauser", "häuser" and "HÄUSER" all match.
/// </summary>
public static class TextNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            switch (c)
            {
                case 'ß':
                    builder.Append("ss");
                    break;
                case 'ı':
                    builder.Append('i');
                    break;
                default:
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
