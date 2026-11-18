using System.Globalization;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// Applied and update dates are calendar dates in the user's own day. The browser checks "today or earlier"
/// against its local date; the server only knows UTC, so it allows one extra day for time zones ahead of UTC.
/// </summary>
internal static class ApplicationDates
{
    /// <summary>Oldest accepted date; anything earlier is a typo (decided 2026-11-18).</summary>
    public static readonly DateOnly Earliest = new(2000, 1, 1);

    // Matches how dates are shown in the app (docs/design.md: "12 Nov 2026").
    private const string DisplayFormat = "dd MMM yyyy";

    public static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);

    public static bool IsTodayOrEarlier(DateOnly date, DateTimeOffset now) => date <= Today(now).AddDays(1);

    public static bool IsTooEarly(DateOnly date) => date < Earliest;

    public static string Display(DateOnly date) => date.ToString(DisplayFormat, CultureInfo.InvariantCulture);
}
