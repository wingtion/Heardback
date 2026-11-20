namespace Heardback.Modules.Applications.Contracts;

/// <summary>
/// What other members' settled applications in one group show (ADR-0013). The signed-in user's own applications
/// are never part of it. A response is the first status change that is not Applied again (ADR-0011).
/// </summary>
/// <param name="Members">Distinct other members with a settled application in the group.</param>
/// <param name="Applications">Their settled applications.</param>
/// <param name="Responded">How many of those applications got a response.</param>
/// <param name="AverageDaysToResponse">Average days from applying to the first response; null when none responded.</param>
/// <param name="MedianDaysToResponse">Median of the same days; null when none responded.</param>
public sealed record CommunityOutcome(
    int Members,
    int Applications,
    int Responded,
    double? AverageDaysToResponse,
    double? MedianDaysToResponse);
