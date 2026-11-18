using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Responses;

/// <summary>The application that already tracks a posting ("You already track this posting").</summary>
internal sealed record TrackedApplicationResponse(
    Guid Id,
    string Company,
    string Role,
    ApplicationStatus Status,
    int? NoResponseDays,
    DateOnly AppliedOn);
