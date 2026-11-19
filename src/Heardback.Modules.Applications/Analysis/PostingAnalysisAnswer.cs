namespace Heardback.Modules.Applications.Analysis;

/// <summary>
/// The model's answer for <see cref="PostingAnalysisPrompt"/>. The JSON schema sent to the provider is generated
/// from this record, so changing it means a new prompt version.
/// </summary>
internal sealed record PostingAnalysisAnswer(
    string? Company,
    string? Title,
    PostingSeniority Seniority,
    PostingWorkMode WorkMode,
    IReadOnlyList<string> Skills,
    string? Location);
