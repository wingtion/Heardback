using Heardback.Shared.Persistence;

namespace Heardback.Modules.Matching.Cvs.Domain;

/// <summary>
/// A user's CV as text, with personal details already removed. One per user; a new upload replaces it. The
/// original PDF is never stored (PLAN.md §4).
/// </summary>
internal sealed class Cv : IUserOwned
{
    /// <summary>Column size: above the upload limit (MatchingSettings.CvMaxTextLength), with room for the markers.</summary>
    public const int TextMaxLength = 100_000;

    private Cv()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public int PageCount { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public static Cv Create(string text, int pageCount, DateTimeOffset now)
    {
        var cv = new Cv { Id = Guid.CreateVersion7(now) };
        cv.Replace(text, pageCount, now);
        return cv;
    }

    public void Replace(string text, int pageCount, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCount);

        Text = text;
        PageCount = pageCount;
        UploadedAt = ToStoredPrecision(now);
    }

    // Postgres keeps microseconds; rounding here makes the upload response match every later read.
    private static DateTimeOffset ToStoredPrecision(DateTimeOffset time) =>
        time.AddTicks(-(time.Ticks % TimeSpan.TicksPerMicrosecond));
}
