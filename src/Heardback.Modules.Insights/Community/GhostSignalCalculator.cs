using System.Globalization;
using Heardback.Modules.Applications.Contracts;

namespace Heardback.Modules.Insights.Community;

/// <summary>
/// The ghost-listing points (ADR-0013). Pure. Every point uses only a group that passed the minimum size, and
/// each one carries the sentence that explains it, so the UI never shows a bare number.
/// </summary>
internal static class GhostSignalCalculator
{
    public const int MaxScore = 5;

    private const int HighFrom = 4;
    private const int MediumFrom = 2;
    private const int NoResponsePoints = 2;
    private const int Percent = 100;

    /// <summary>The hint, or <see langword="null"/> when neither the posting nor the company passed the minimum size.</summary>
    public static GhostSignal? Calculate(
        TrackedApplication application,
        PostingOutcome? posting,
        CompanyOutcome? company,
        DateTimeOffset now,
        CommunitySettings settings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(settings);

        if (posting is null && company is null)
        {
            return null;
        }

        IEnumerable<GhostReason?> candidates =
        [
            .. posting is null ? [] : PostingReasons(posting, now, settings),
            .. company is null ? [] : CompanyReasons(application, company, settings),
        ];
        var reasons = candidates
            .OfType<GhostReason>()
            .OrderByDescending(reason => reason.Points)
            .ThenBy(reason => reason.Kind)
            .ToList();

        var score = Math.Min(MaxScore, reasons.Sum(reason => reason.Points));
        return new GhostSignal(score, MaxScore, LevelFor(score), reasons);
    }

    public static GhostLevel LevelFor(int score) =>
        score >= HighFrom ? GhostLevel.High
        : score >= MediumFrom ? GhostLevel.Medium
        : GhostLevel.Low;

    private static IEnumerable<GhostReason?> PostingReasons(
        PostingOutcome posting,
        DateTimeOffset now,
        CommunitySettings settings)
    {
        var listedDays = (int)(now - posting.FirstSeenAt).TotalDays;
        yield return listedDays >= settings.LongListedDays
            ? new GhostReason(GhostReasonKind.LongListed, 1, Format($"First added here {listedDays} days ago."))
            : null;

        var outcome = posting.Outcome;
        if (outcome.Responded == 0)
        {
            yield return new GhostReason(
                GhostReasonKind.NoPostingResponse,
                NoResponsePoints,
                Format($"None of the {outcome.Members} other members who applied heard back."));
        }
        else if (IsLow(outcome.Responded, outcome.Applications, settings))
        {
            yield return new GhostReason(
                GhostReasonKind.LowPostingResponse,
                1,
                Format($"Only {outcome.Responded} of the {outcome.Applications} other members who applied heard back."));
        }
    }

    private static IEnumerable<GhostReason?> CompanyReasons(
        TrackedApplication application,
        CompanyOutcome company,
        CommunitySettings settings)
    {
        var outcome = company.Outcome;
        yield return IsLow(outcome.Responded, outcome.Applications, settings)
            ? new GhostReason(
                GhostReasonKind.LowCompanyResponse,
                1,
                Format($"Other members heard back on {outcome.Responded} of {outcome.Applications} applications to this company."))
            : null;

        yield return outcome.AverageDaysToResponse is { } average && average >= settings.SlowResponseDays
            ? new GhostReason(
                GhostReasonKind.SlowCompanyResponse,
                1,
                Format($"This company took {Math.Round(average, 1, MidpointRounding.AwayFromZero):0.#} days on average to answer other members."))
            : null;

        yield return application.RoleKey is { } roleKey
            && company.PostingsPerRole.TryGetValue(roleKey, out var postings)
            && postings >= settings.RepostedRolePostings
            ? new GhostReason(
                GhostReasonKind.RoleReposted,
                1,
                Format($"This role has been added here as {postings} separate postings."))
            : null;
    }

    // Compared as whole numbers, so 1 of 4 is exactly 25 % and not "low" against a 25 % threshold.
    private static bool IsLow(int responded, int applications, CommunitySettings settings) =>
        responded * Percent < settings.LowResponsePercent * applications;

    private static string Format(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
