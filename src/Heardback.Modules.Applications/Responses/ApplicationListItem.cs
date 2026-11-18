using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Responses;

/// <summary>One row of the list or one board card.</summary>
internal sealed record ApplicationListItem(
    Guid Id,
    string Company,
    string Role,
    ApplicationChannel Channel,
    DateOnly AppliedOn,
    ApplicationStatus Status,
    int? NoResponseDays,
    DateTimeOffset UpdatedAt);
