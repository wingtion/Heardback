using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Community;

/// <summary>
/// Serves <see cref="ICommunityOutcomes"/>. This is the one place that reads other users' applications (ADR-0013):
/// it only looks at groups the signed-in user tracks, leaves the user's own applications out, and returns counts.
/// </summary>
internal sealed class CommunityOutcomesReader(
    ApplicationsDbContext dbContext,
    ICurrentUser currentUser,
    NoResponseClock noResponse,
    IJobPostings jobPostings,
    IDemoAccounts demoAccounts,
    IOptions<ApplicationsSettings> settings) : ICommunityOutcomes
{
    public async Task<CommunitySnapshot> ForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var viewerId = currentUser.GetRequiredUserId();
        var tracked = await ListTrackedAsync(cancellationToken);

        var postingIds = tracked.Select(application => application.PostingId).Distinct().ToList();
        var companyKeys = tracked
            .Where(application => application.CompanyKey is not null)
            .Select(application => application.CompanyKey!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var audience = await CommunityAudience.ForAsync(currentUser, demoAccounts, cancellationToken);
        var others = await ListOthersAsync(viewerId, audience, postingIds, companyKeys, cancellationToken);
        var minimumMembers = settings.Value.CommunityMinimumMembers;

        var postingOutcomes = others
            .Where(application => postingIds.Contains(application.PostingId))
            .GroupBy(application => application.PostingId)
            .Select(group => (group.Key, Outcome: CommunityGrouping.Outcome([.. group], minimumMembers)))
            .Where(pair => pair.Outcome is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Outcome!);

        // Asked only for postings that passed: an earlier first-seen date would show that someone else tracks it.
        var firstSeen = await jobPostings.FirstSeenAsync(postingOutcomes.Keys, cancellationToken);
        var postings = postingOutcomes
            .Where(pair => firstSeen.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => new PostingOutcome(pair.Value, firstSeen[pair.Key]));

        var companyOutcomes = others
            .Where(application => application.CompanyKey is not null && companyKeys.Contains(application.CompanyKey))
            .GroupBy(application => application.CompanyKey!, StringComparer.Ordinal)
            .Select(group => (group.Key, Outcome: CommunityGrouping.Outcome([.. group], minimumMembers)))
            .Where(pair => pair.Outcome is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Outcome!, StringComparer.Ordinal);

        var companies = await AddRolePostingsAsync(viewerId, audience, tracked, companyOutcomes, cancellationToken);
        return new CommunitySnapshot(tracked, postings, companies);
    }

    private async Task<List<TrackedApplication>> ListTrackedAsync(CancellationToken cancellationToken)
    {
        // The ownership filter is on here: these are the signed-in user's own applications.
        var rows = await dbContext.Applications
            .AsNoTracking()
            .OrderByDescending(application => application.AppliedOn)
            .ThenByDescending(application => application.CreatedAt)
            .Select(application => new
            {
                application.Id,
                application.PostingId,
                application.Company,
                application.CompanyKey,
                application.Role,
                application.RoleKey,
                application.Status,
                application.AppliedOn,
                application.LastActivityOn,
                application.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new TrackedApplication(
                row.Id,
                row.PostingId,
                row.Company,
                row.CompanyKey,
                row.Role,
                row.RoleKey,
                ApplicationStages.From(row.Status),
                row.AppliedOn,
                noResponse.DaysFor(row.Status, row.LastActivityOn),
                row.UpdatedAt))
            .ToList();
    }

    private async Task<List<OtherApplication>> ListOthersAsync(
        Guid viewerId,
        CommunityAudience audience,
        List<Guid> postingIds,
        List<string> companyKeys,
        CancellationToken cancellationToken)
    {
        if (postingIds.Count == 0)
        {
            return [];
        }

        var latestSettled = noResponse.LatestSettledAppliedOn;

        // Deliberately across users (ADR-0013): only groups the viewer tracks, never the viewer's own rows, and
        // the projection keeps text, notes, links and ids other than the grouping keys out.
        return await audience
            .Filter(dbContext.Applications.IgnoreQueryFilters([ModuleDbContext.UserOwnershipFilter]), viewerId)
            .AsNoTracking()
            .Where(application => application.UserId != viewerId
                && application.AppliedOn <= latestSettled
                && (postingIds.Contains(application.PostingId)
                    || (application.CompanyKey != null && companyKeys.Contains(application.CompanyKey))))
            .Select(application => new OtherApplication(
                application.UserId,
                application.PostingId,
                application.CompanyKey,
                application.AppliedOn,
                application.Events
                    .Where(timelineEvent => !timelineEvent.IsSubmission
                        && timelineEvent.Status != null
                        && timelineEvent.Status != ApplicationStatus.Applied)
                    .Min(timelineEvent => (DateOnly?)timelineEvent.OccurredOn)))
            .ToListAsync(cancellationToken);
    }

    // Only for companies that passed the minimum size, so a lone other member's posting is never hinted at.
    private async Task<Dictionary<string, CompanyOutcome>> AddRolePostingsAsync(
        Guid viewerId,
        CommunityAudience audience,
        List<TrackedApplication> tracked,
        Dictionary<string, CommunityOutcome> companyOutcomes,
        CancellationToken cancellationToken)
    {
        if (companyOutcomes.Count == 0)
        {
            return new Dictionary<string, CompanyOutcome>(StringComparer.Ordinal);
        }

        var companyKeys = companyOutcomes.Keys.ToList();
        var roleKeys = tracked
            .Where(application => application.RoleKey is not null && companyOutcomes.ContainsKey(application.CompanyKey ?? string.Empty))
            .Select(application => application.RoleKey!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var rolePostings = await audience
            .Filter(dbContext.Applications.IgnoreQueryFilters([ModuleDbContext.UserOwnershipFilter]), viewerId)
            .AsNoTracking()
            .Where(application => application.CompanyKey != null
                && companyKeys.Contains(application.CompanyKey)
                && application.RoleKey != null
                && roleKeys.Contains(application.RoleKey))
            .GroupBy(application => new { application.CompanyKey, application.RoleKey })
            .Select(group => new
            {
                group.Key.CompanyKey,
                group.Key.RoleKey,
                Postings = group.Select(application => application.PostingId).Distinct().Count(),
            })
            .ToListAsync(cancellationToken);

        return companyOutcomes.ToDictionary(
            pair => pair.Key,
            pair => new CompanyOutcome(
                pair.Value,
                TrackedRoles(tracked, pair.Key).ToDictionary(
                    roleKey => roleKey,
                    roleKey => rolePostings
                        .Where(row => row.CompanyKey == pair.Key && row.RoleKey == roleKey)
                        .Sum(row => row.Postings),
                    StringComparer.Ordinal)),
            StringComparer.Ordinal);
    }

    private static IEnumerable<string> TrackedRoles(List<TrackedApplication> tracked, string companyKey) =>
        tracked
            .Where(application => application.CompanyKey == companyKey && application.RoleKey is not null)
            .Select(application => application.RoleKey!)
            .Distinct(StringComparer.Ordinal);
}
