using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Insights.Dashboard;

namespace Heardback.Modules.Insights.Community;

/// <summary>Other members' numbers for one group, shown exactly (ADR-0013). The viewer is never part of them.</summary>
/// <param name="Members">Distinct other members with a settled application.</param>
/// <param name="Applications">Their settled applications.</param>
/// <param name="Responded">How many of those got a response.</param>
/// <param name="ResponseRatePercent">Responded / applications as a whole percent.</param>
/// <param name="AverageDays">Average days to the first response, one decimal; null when nobody heard back.</param>
/// <param name="MedianDays">Median days to the first response; null when nobody heard back.</param>
internal sealed record CommunityNumbers(
    int Members,
    int Applications,
    int Responded,
    int ResponseRatePercent,
    double? AverageDays,
    double? MedianDays)
{
    public static CommunityNumbers From(CommunityOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        return new CommunityNumbers(
            outcome.Members,
            outcome.Applications,
            outcome.Responded,
            DashboardCalculator.Rate(outcome.Responded, outcome.Applications),
            outcome.AverageDaysToResponse is { } average ? Math.Round(average, 1, MidpointRounding.AwayFromZero) : null,
            outcome.MedianDaysToResponse);
    }
}
