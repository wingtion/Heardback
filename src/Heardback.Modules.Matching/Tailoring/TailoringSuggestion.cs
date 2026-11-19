namespace Heardback.Modules.Matching.Tailoring;

/// <summary>One stored suggestion; kept as JSON inside its <see cref="TailoringResult"/>.</summary>
internal sealed class TailoringSuggestion(string cvSection, string cvQuote, string advice)
{
    public string CvSection { get; private set; } = cvSection;

    public string CvQuote { get; private set; } = cvQuote;

    public string Advice { get; private set; } = advice;
}
