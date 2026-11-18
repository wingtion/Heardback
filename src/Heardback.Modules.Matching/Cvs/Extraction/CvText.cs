using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>Tidies text read from a PDF so it stores and searches well.</summary>
internal static partial class CvText
{
    private const char LineBreak = '\n';
    private const char Space = ' ';
    private const string ParagraphBreak = "\n\n";

    /// <summary>
    /// Unicode NFC; Windows and old Mac line ends become <c>\n</c>; tabs and other control characters
    /// (Postgres cannot store NUL) become spaces; runs of spaces collapse; each line is trimmed; at most one
    /// blank line in a row.
    /// </summary>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var unified = text.Normalize(NormalizationForm.FormC).ReplaceLineEndings("\n");
        var visible = new StringBuilder(unified.Length);
        foreach (var character in unified)
        {
            visible.Append(character != LineBreak && IsInvisible(character) ? Space : character);
        }

        var lines = visible.ToString()
            .Split(LineBreak)
            .Select(line => Spaces().Replace(line, " ").Trim());

        return BlankLines().Replace(string.Join(LineBreak, lines), ParagraphBreak).Trim();
    }

    private static bool IsInvisible(char character) =>
        char.IsControl(character) || CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format;

    [GeneratedRegex(@"[   -   　]+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();
}
