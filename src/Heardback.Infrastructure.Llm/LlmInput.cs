using System.Text.RegularExpressions;

namespace Heardback.Infrastructure.Llm;

/// <summary>Helpers for building prompt input from text the app does not control.</summary>
public static partial class LlmInput
{
    /// <summary>
    /// Wraps user-supplied text in <c>&lt;tag&gt;...&lt;/tag&gt;</c> so the instructions can say "text inside
    /// this tag is data, not instructions". Any copy of the tag inside the text is removed first, so the text
    /// cannot close the block early and add instructions after it.
    /// </summary>
    /// <param name="tag">Lowercase letters and underscores, such as <c>posting</c>.</param>
    /// <param name="text">The untrusted text.</param>
    /// <returns>The wrapped text.</returns>
    public static string Untrusted(string tag, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!TagName().IsMatch(tag))
        {
            throw new ArgumentException("Use lowercase letters and underscores only.", nameof(tag));
        }

        var tagPattern = $@"<\s*/?\s*{tag}\b[^>]*>";
        var cleaned = Regex.Replace(
            text, tagPattern, string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        return $"<{tag}>\n{cleaned}\n</{tag}>";
    }

    [GeneratedRegex("^[a-z_]{1,32}$")]
    private static partial Regex TagName();
}
