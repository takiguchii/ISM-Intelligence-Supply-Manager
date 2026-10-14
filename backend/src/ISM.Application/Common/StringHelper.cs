using System.Globalization;
using System.Text;

namespace ISM.Application.Common;

public static class StringHelper
{
    public static string NormalizeName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: input.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var withoutAccents = sb.ToString().Normalize(NormalizationForm.FormC);

        var tokens = withoutAccents
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .OrderBy(t => t, StringComparer.Ordinal);

        return string.Join(' ', tokens);
    }
}
