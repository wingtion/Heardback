using System.Globalization;
using System.Text;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// A deterministic check on the model's suggestions, because instructions alone did not stop it from suggesting
/// things the CV does not show (tested 2026-11-19). A suggestion is kept only when its quote appears in the CV and
/// its advice names none of the posting skills the CV lacks: those gaps are shown from the keyword match instead.
/// </summary>
internal static class TailoringGrounding
{
    private const int MinQuoteLength = 3;

    public static IReadOnlyList<TailoringAdvice> Keep(IEnumerable<TailoringAdvice> suggestions, TailoringInput input)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        ArgumentNullException.ThrowIfNull(input);

        var cv = Normalize(input.CvText);
        return suggestions
            .Where(suggestion => QuotesTheCv(suggestion.CvQuote, cv))
            .Where(suggestion => !input.MissingSkills.Any(skill => Names(suggestion.Advice, skill)))
            .ToList();
    }

    private static bool QuotesTheCv(string quote, string normalizedCv)
    {
        var normalized = Normalize(quote).Trim('"', '\'', '…', '.', ' ');
        return normalized.Length >= MinQuoteLength
            && normalizedCv.Contains(normalized, StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether <paramref name="text"/> names <paramref name="skill"/> as a whole word. "Go" is not found in
    /// "good", and names with symbols such as "C#" or ".NET" still match.
    /// </summary>
    internal static bool Names(string text, string skill)
    {
        var start = 0;
        while ((start = text.IndexOf(skill, start, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = start + skill.Length;
            var wordBefore = start > 0 && char.IsLetterOrDigit(text[start - 1]) && char.IsLetterOrDigit(skill[0]);
            var wordAfter = end < text.Length && char.IsLetterOrDigit(text[end]) && char.IsLetterOrDigit(skill[^1]);
            if (!wordBefore && !wordAfter)
            {
                return true;
            }

            start = end;
        }

        return false;
    }

    // Case, runs of whitespace and typographic dashes or quotes differ between the CV text and a copied quote.
    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var character in text.Normalize(NormalizationForm.FormKC))
        {
            var mapped = CharUnicodeInfo.GetUnicodeCategory(character) switch
            {
                UnicodeCategory.DashPunctuation => '-',
                UnicodeCategory.InitialQuotePunctuation or UnicodeCategory.FinalQuotePunctuation => '\'',
                _ => char.ToLowerInvariant(character),
            };
            if (char.IsWhiteSpace(mapped))
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
                continue;
            }

            builder.Append(mapped);
            lastWasSpace = false;
        }

        return builder.ToString().Trim();
    }
}
