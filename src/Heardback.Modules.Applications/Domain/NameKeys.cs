using System.Globalization;
using System.Text;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// Comparison keys for company and role names, so the same company typed in different ways ("Acme Inc.",
/// "ACME", "Şişecam A.Ş." and "Sisecam") lands in one community group (ADR-0013).
/// </summary>
internal static class NameKeys
{
    private const char Separator = ' ';

    // Trailing legal forms, as words after folding ("A.Ş." becomes "a s"). Longer forms first, so
    // "ltd sti" is removed as one suffix rather than leaving "ltd" behind.
    private static readonly string[][] LegalSuffixes =
    [
        ["sanayi", "ve", "ticaret", "a", "s"],
        ["san", "ve", "tic", "a", "s"],
        ["san", "ve", "tic", "ltd", "sti"],
        ["sanayi", "ve", "ticaret", "ltd", "sti"],
        ["ltd", "sti"],
        ["a", "s"],
        ["as"],
        ["inc"],
        ["incorporated"],
        ["llc"],
        ["ltd"],
        ["limited"],
        ["plc"],
        ["gmbh"],
        ["ag"],
        ["bv"],
        ["nv"],
        ["sa"],
        ["sas"],
        ["srl"],
        ["spa"],
        ["corp"],
        ["corporation"],
        ["co"],
    ];

    /// <summary>
    /// The company key: folded like <see cref="Role"/>, then trailing legal forms removed. A name made only of
    /// legal-form words keeps them, so it never becomes empty.
    /// </summary>
    public static string Company(string name)
    {
        var words = Fold(name);
        while (FindSuffix(words) is { } suffix && suffix.Length < words.Count)
        {
            words.RemoveRange(words.Count - suffix.Length, suffix.Length);
        }

        return string.Join(Separator, words);
    }

    /// <summary>
    /// The role key: Unicode compatibility form, accents and the Turkish dotless i folded to plain letters,
    /// lower case, and every run of punctuation or whitespace turned into one space.
    /// </summary>
    public static string Role(string name) => string.Join(Separator, Fold(name));

    private static List<string> Fold(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var folded = new StringBuilder(name.Length);
        foreach (var character in name.Normalize(NormalizationForm.FormKD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            folded.Append(char.IsLetterOrDigit(character) ? FoldLetter(character) : Separator);
        }

        return [.. folded.ToString().Split(Separator, StringSplitOptions.RemoveEmptyEntries)];
    }

    // Invariant lower case keeps "ı" apart from "i"; people type Turkish names with and without it.
    private static char FoldLetter(char character)
    {
        var lower = char.ToLowerInvariant(character);
        return lower == 'ı' ? 'i' : lower;
    }

    private static string[]? FindSuffix(List<string> words) =>
        LegalSuffixes.FirstOrDefault(suffix => suffix.Length <= words.Count
            && suffix.SequenceEqual(words.Skip(words.Count - suffix.Length)));
}
