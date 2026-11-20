using Heardback.Modules.Applications.Contracts;

namespace Heardback.Modules.Applications.Community;

/// <summary>Turns other members' applications into group counts, dropping groups below the minimum size (ADR-0013).</summary>
internal static class CommunityGrouping
{
    /// <summary>
    /// The outcome of one group, or <see langword="null"/> when fewer than <paramref name="minimumMembers"/>
    /// distinct members are in it: then not even the count may be shown.
    /// </summary>
    public static CommunityOutcome? Outcome(IReadOnlyCollection<OtherApplication> group, int minimumMembers)
    {
        ArgumentNullException.ThrowIfNull(group);

        var members = group.Select(application => application.UserId).Distinct().Count();
        if (members < minimumMembers)
        {
            return null;
        }

        // A response dated before the applied date can only be a typing mistake; it counts as the same day (ADR-0011).
        var days = group
            .Where(application => application.FirstResponseOn is not null)
            .Select(application => Math.Max(0, application.FirstResponseOn!.Value.DayNumber - application.AppliedOn.DayNumber))
            .Order()
            .ToList();

        return new CommunityOutcome(
            members,
            group.Count,
            days.Count,
            days.Count == 0 ? null : days.Average(),
            days.Count == 0 ? null : Median(days));
    }

    private static double Median(List<int> sortedDays)
    {
        var middle = sortedDays.Count / 2;
        return sortedDays.Count % 2 == 1
            ? sortedDays[middle]
            : (sortedDays[middle - 1] + sortedDays[middle]) / 2.0;
    }
}
