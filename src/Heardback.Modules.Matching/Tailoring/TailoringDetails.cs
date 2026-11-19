namespace Heardback.Modules.Matching.Tailoring;

/// <summary>Stored suggestions. Plain text; never rendered as HTML.</summary>
/// <param name="Summary">How well the CV fits the posting.</param>
/// <param name="Suggestions">Concrete changes, most useful first.</param>
/// <param name="IsOutdated">The CV or the posting changed since; asking again makes new ones.</param>
/// <param name="CreatedAt">When they were made.</param>
internal sealed record TailoringDetails(
    string Summary,
    IReadOnlyList<TailoringAdvice> Suggestions,
    bool IsOutdated,
    DateTimeOffset CreatedAt);
