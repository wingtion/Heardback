using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Responses;

/// <summary>A timeline entry. <see cref="Status"/> is null for a note-only update.</summary>
internal sealed record ApplicationEventResponse(
    Guid Id,
    ApplicationStatus? Status,
    DateOnly OccurredOn,
    string? Note,
    bool IsSubmission);
