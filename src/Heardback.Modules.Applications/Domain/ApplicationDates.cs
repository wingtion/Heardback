namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// Applied and update dates are calendar dates in the user's own day. The browser checks "today or earlier"
/// against its local date; the server only knows UTC, so it allows one extra day for time zones ahead of UTC.
/// </summary>
internal static class ApplicationDates
{
    public static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);

    public static bool IsTodayOrEarlier(DateOnly date, DateTimeOffset now) => date <= Today(now).AddDays(1);
}
