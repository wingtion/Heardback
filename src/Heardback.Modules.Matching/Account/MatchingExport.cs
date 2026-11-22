namespace Heardback.Modules.Matching.Account;

/// <summary>The Matching module's part of the data export: CV text, AI consent and tailoring (ADR-0015).</summary>
internal sealed record MatchingExport(
    CvExport? Cv,
    DateTimeOffset? AiConsentGivenAt,
    IReadOnlyList<TailoringExport> CvSuggestions);

/// <summary>The stored CV text (personal details already replaced) and the skills the user hid.</summary>
internal sealed record CvExport(string Text, int PageCount, DateTimeOffset UploadedAt, IReadOnlyList<string> ExcludedSkills);

/// <summary>AI suggestions for one application's CV.</summary>
internal sealed record TailoringExport(
    Guid ApplicationId,
    string Summary,
    IReadOnlyList<TailoringSuggestionExport> Suggestions,
    string Provider,
    string Model,
    DateTimeOffset CreatedAt);

/// <summary>One AI suggestion.</summary>
internal sealed record TailoringSuggestionExport(string CvSection, string CvQuote, string Advice);
