using System.Linq.Expressions;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// "No response · N days": an application still in <see cref="ApplicationStatus.Applied"/> whose last event is at
/// least the threshold number of days old. Derived on every read, never stored, so it cannot go stale.
/// </summary>
internal static class NoResponseRule
{
    /// <summary>Days without a response, or <see langword="null"/> when the label does not apply.</summary>
    public static int? DaysWithoutResponse(
        ApplicationStatus status,
        DateOnly lastActivityOn,
        DateOnly today,
        int thresholdDays)
    {
        var days = today.DayNumber - lastActivityOn.DayNumber;
        return status == ApplicationStatus.Applied && days >= thresholdDays ? days : null;
    }

    /// <summary>The same rule as a query filter, so counts and filters run in the database. Keep the two in step.</summary>
    public static Expression<Func<Application, bool>> Matches(DateOnly today, int thresholdDays)
    {
        var latestQualifyingActivity = today.AddDays(-thresholdDays);
        return application => application.Status == ApplicationStatus.Applied
            && application.LastActivityOn <= latestQualifyingActivity;
    }
}
