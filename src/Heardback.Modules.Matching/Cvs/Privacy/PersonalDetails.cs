using System.Text.RegularExpressions;

namespace Heardback.Modules.Matching.Cvs.Privacy;

/// <summary>
/// Removes personal details from CV text before it is stored (PLAN.md §5, §7): email addresses, phone numbers
/// (and other long digit runs such as national ID numbers), links, and lines that look like a postal address.
/// Each is replaced by a short marker so the user can see what was taken out. Names are kept: they carry no
/// matching value but are not listed as PII to strip, and guessing them would remove skills too.
/// </summary>
internal static partial class PersonalDetails
{
    public const string EmailMarker = "[email]";
    public const string PhoneMarker = "[phone]";
    public const string LinkMarker = "[link]";
    public const string AddressMarker = "[address]";

    // A phone number has 10 to 15 digits (E.164 allows 15). Shorter runs are years, dates and versions.
    private const int PhoneMinDigits = 10;
    private const int PhoneMaxDigits = 15;

    // A whole paragraph is never an address line, even if it mentions a street.
    private const int AddressLineMaxLength = 120;

    private const int YearLength = 4;
    private const string LineBreak = "\n";

    public static string Strip(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var withoutEmails = Email().Replace(text, EmailMarker);
        var withoutLinks = Link().Replace(withoutEmails, LinkMarker);
        var withoutPhones = PhoneCandidate().Replace(withoutLinks, ReplacePhone);
        return string.Join(LineBreak, withoutPhones.Split(LineBreak).Select(StripAddressLine));
    }

    private static string ReplacePhone(Match candidate)
    {
        var groups = Digits().Matches(candidate.Value).Select(group => group.Value).ToList();
        var digitCount = groups.Sum(group => group.Length);
        if (digitCount is < PhoneMinDigits or > PhoneMaxDigits || groups.All(IsYear))
        {
            return candidate.Value;
        }

        return PhoneMarker;
    }

    private static bool IsYear(string group) =>
        group.Length == YearLength && (group.StartsWith("19", StringComparison.Ordinal) ||
                                       group.StartsWith("20", StringComparison.Ordinal));

    private static string StripAddressLine(string line) =>
        line.Length <= AddressLineMaxLength && AddressWord().IsMatch(line) && line.Any(char.IsAsciiDigit)
            ? AddressMarker
            : line;

    [GeneratedRegex(@"[\p{L}\p{N}._%+-]+@[\p{L}\p{N}-]+(\.[\p{L}\p{N}-]+)*\.\p{L}{2,}")]
    private static partial Regex Email();

    // Full links, "www." links, and bare domains with a path (linkedin.com/in/ada). Bare domains count only for
    // common personal-site endings, never .net or .js, so "ASP.NET/MVC" and "Node.js/Express" survive; Behance is
    // the one well-known profile site on .net.
    [GeneratedRegex(
        @"\b(?:https?://|www\.)\S+" +
        @"|\b(?:[\p{L}\p{N}-]+\.)+(?:com|org|io|dev|me|app|co|tr|info|ai|page|site|xyz)/\S*" +
        @"|\bbehance\.net/\S*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Link();

    // Digits with the separators phone numbers are written with; checked further in ReplacePhone.
    [GeneratedRegex(@"(?<![\p{L}\p{N}])\+?\(?\d[\d  ().-]*\d(?![\p{L}\p{N}])")]
    private static partial Regex PhoneCandidate();

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();

    // Turkish and English address words, as whole words ("road" but not "roadmap"). A line counts only if it also
    // has a digit (a house or postal number).
    [GeneratedRegex(
        @"\b(?:mah\.|mahallesi|cad\.|caddesi|sok\.|sokak|sk\.|bulvar[ıi]|blv\.|apt\.?|apartman[ıi]|daire|kat:|no:" +
        @"|street|st\.|avenue|ave\.|road|rd\.|suite|zip|postcode|postal code)(?!\p{L})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AddressWord();
}
