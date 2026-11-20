using Heardback.Modules.Applications.Contracts;

namespace Heardback.Modules.Insights.Dashboard;

/// <summary>Turns the user's applications into the dashboard numbers. Pure; the definitions are in ADR-0011.</summary>
internal static class DashboardCalculator
{
    public const int WeeksShown = 12;

    private const int DaysPerWeek = 7;
    private const double Percent = 100.0;

    public static DashboardResponse Calculate(IReadOnlyList<ApplicationHistory> applications, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(applications);

        var responded = applications.Count(application => FirstResponse(application) is not null);
        return new DashboardResponse(
            applications.Count,
            responded,
            applications.Count == 0 ? null : Rate(responded, applications.Count),
            Weeks(applications, today),
            Channels(applications),
            ResponseTimes(applications),
            Funnel(applications));
    }

    /// <summary>The first status change that is an answer: anything but Applied again.</summary>
    public static ApplicationStatusChange? FirstResponse(ApplicationHistory application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return application.StatusChanges.FirstOrDefault(change => change.Stage != ApplicationStage.Applied);
    }

    /// <summary>The Monday on or before <paramref name="date"/>.</summary>
    public static DateOnly WeekStart(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + DaysPerWeek - (int)DayOfWeek.Monday) % DaysPerWeek));

    private static List<WeeklyCount> Weeks(IReadOnlyList<ApplicationHistory> applications, DateOnly today)
    {
        var currentWeek = WeekStart(today);
        var counts = applications
            .GroupBy(application => WeekStart(application.AppliedOn))
            .ToDictionary(group => group.Key, group => group.Count());

        return Enumerable.Range(0, WeeksShown)
            .Select(offset => currentWeek.AddDays(-DaysPerWeek * (WeeksShown - 1 - offset)))
            .Select(week => new WeeklyCount(week, counts.GetValueOrDefault(week)))
            .ToList();
    }

    private static List<ChannelResponse> Channels(IReadOnlyList<ApplicationHistory> applications) =>
        applications
            .GroupBy(application => application.Channel)
            .Select(group =>
            {
                var total = group.Count();
                var answered = group.Count(application => FirstResponse(application) is not null);
                return new ChannelResponse(group.Key, total, answered, Rate(answered, total));
            })
            .OrderByDescending(channel => channel.Applications)
            .ThenBy(channel => channel.Channel, StringComparer.Ordinal)
            .ToList();

    private static ResponseTime ResponseTimes(IReadOnlyList<ApplicationHistory> applications)
    {
        // A response dated before the applied date can only be a typing mistake; it counts as the same day.
        var days = applications
            .Select(application => (application.AppliedOn, Response: FirstResponse(application)))
            .Where(pair => pair.Response is not null)
            .Select(pair => Math.Max(0, pair.Response!.OccurredOn.DayNumber - pair.AppliedOn.DayNumber))
            .Order()
            .ToList();
        if (days.Count == 0)
        {
            return new ResponseTime(0, null, null);
        }

        var middle = days.Count / 2;
        var median = days.Count % 2 == 1 ? days[middle] : (days[middle - 1] + days[middle]) / 2.0;
        return new ResponseTime(days.Count, Math.Round(days.Average(), 1, MidpointRounding.AwayFromZero), median);
    }

    private static Funnel Funnel(IReadOnlyList<ApplicationHistory> applications)
    {
        bool Reached(ApplicationHistory application, params ApplicationStage[] stages) =>
            application.StatusChanges.Any(change => stages.Contains(change.Stage));

        return new Funnel(
            applications.Count,
            applications.Count(application => Reached(application, ApplicationStage.Interview, ApplicationStage.Offer)),
            applications.Count(application => Reached(application, ApplicationStage.Offer)),
            applications.Count(application => Reached(application, ApplicationStage.Rejected)));
    }

    /// <summary>A whole percent, rounded half away from zero.</summary>
    public static int Rate(int part, int whole) =>
        (int)Math.Round(Percent * part / whole, MidpointRounding.AwayFromZero);
}
