namespace Heardback.Modules.Applications.Analysis;

/// <summary>What the model read from the posting. Plain text; never rendered as HTML.</summary>
internal sealed record PostingAnalysisFields(
    string? Company,
    string? Title,
    PostingSeniority Seniority,
    PostingWorkMode WorkMode,
    IReadOnlyList<string> Skills,
    string? Location,
    DateTimeOffset AnalyzedAt);
