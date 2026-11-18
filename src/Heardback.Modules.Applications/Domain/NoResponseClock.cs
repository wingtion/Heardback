using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// <see cref="NoResponseRule"/> bound to today's date and the configured threshold. Scoped, so every number in
/// one response is computed against the same day.
/// </summary>
internal sealed class NoResponseClock(TimeProvider timeProvider, IOptions<ApplicationsSettings> settings)
{
    private readonly DateOnly _today = ApplicationDates.Today(timeProvider.GetUtcNow());

    public int? DaysFor(ApplicationStatus status, DateOnly lastActivityOn) =>
        NoResponseRule.DaysWithoutResponse(status, lastActivityOn, _today, settings.Value.NoResponseAfterDays);

    /// <summary>The day the "No response" label started (last event + threshold), or null when it does not apply.</summary>
    public DateOnly? SinceFor(ApplicationStatus status, DateOnly lastActivityOn) =>
        DaysFor(status, lastActivityOn) is null ? null : lastActivityOn.AddDays(settings.Value.NoResponseAfterDays);

    public Expression<Func<Application, bool>> Matches() =>
        NoResponseRule.Matches(_today, settings.Value.NoResponseAfterDays);

    public Expression<Func<Application, bool>> MatchesRecentlyApplied() =>
        NoResponseRule.MatchesRecentlyApplied(_today, settings.Value.NoResponseAfterDays);
}
