using Heardback.Modules.Applications.Analysis;
using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Account;

/// <summary>One application in the data export, with the user's own copy of the posting (ADR-0015).</summary>
internal sealed record ApplicationExport(
    Guid Id,
    string Company,
    string Role,
    string? Url,
    string? PostingText,
    ApplicationChannel Channel,
    DateOnly AppliedOn,
    ApplicationStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ApplicationEventExport> Timeline,
    PostingAnalysisExport? JobSummary);

/// <summary>One timeline entry in the data export.</summary>
internal sealed record ApplicationEventExport(
    ApplicationStatus? Status,
    DateOnly OccurredOn,
    string? Note,
    bool IsSubmission,
    DateTimeOffset CreatedAt);

/// <summary>The AI job summary of an application's posting text in the data export.</summary>
internal sealed record PostingAnalysisExport(
    string? Company,
    string? Title,
    PostingSeniority Seniority,
    PostingWorkMode WorkMode,
    IReadOnlyList<string> Skills,
    string? Location,
    string Provider,
    string Model,
    DateTimeOffset AnalyzedAt);
