using Heardback.Modules.Applications.Contracts;
using Heardback.Shared.Paging;
using Heardback.Shared.Results;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Insights.Community;

/// <summary>
/// Community numbers and ghost-listing hints for the viewer's own applications and companies (ADR-0013). Everything
/// comes from <see cref="ICommunityOutcomes"/>, which already dropped groups below the minimum size.
/// </summary>
internal sealed class CommunitySignalsHandler(
    ICommunityOutcomes outcomes,
    TimeProvider timeProvider,
    IOptions<CommunitySettings> settings)
{
    public async Task<Result<ApplicationCommunityResponse>> ForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await outcomes.ForCurrentUserAsync(cancellationToken);
        var application = snapshot.Applications.FirstOrDefault(tracked => tracked.ApplicationId == applicationId);
        if (application is null)
        {
            return CommunityErrors.ApplicationNotFound;
        }

        var posting = snapshot.Postings.GetValueOrDefault(application.PostingId);
        var company = CompanyOf(snapshot, application);
        return new ApplicationCommunityResponse(
            application.ApplicationId,
            application.CompanyKey,
            posting is null ? null : CommunityNumbers.From(posting.Outcome),
            company is null ? null : CommunityNumbers.From(company.Outcome),
            Ghost(snapshot, application));
    }

    public async Task<PagedResponse<CommunityBadge>> BadgesAsync(PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var snapshot = await outcomes.ForCurrentUserAsync(cancellationToken);
        var badges = snapshot.Applications
            .Select(application => (application.ApplicationId, Ghost: Ghost(snapshot, application)))
            .Where(pair => pair.Ghost is { Level: not GhostLevel.Low })
            .Select(pair => new CommunityBadge(pair.ApplicationId, pair.Ghost!.Level, pair.Ghost.Score))
            .ToList();

        return new PagedResponse<CommunityBadge>(
            badges.Skip(page.Skip).Take(page.PageSize).ToList(), page.Page, page.PageSize, badges.Count);
    }

    public async Task<Result<CompanyCommunityResponse>> ForCompanyAsync(
        string companyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(companyKey);

        var snapshot = await outcomes.ForCurrentUserAsync(cancellationToken);
        var own = snapshot.Applications
            .Where(application => string.Equals(application.CompanyKey, companyKey, StringComparison.Ordinal))
            .ToList();
        if (own.Count == 0)
        {
            return CommunityErrors.CompanyNotFound;
        }

        var company = snapshot.Companies.GetValueOrDefault(companyKey);
        return new CompanyCommunityResponse(
            companyKey,
            own[0].Company,
            company is null ? null : CommunityNumbers.From(company.Outcome),
            own.Select(application => new CompanyApplicationItem(
                    application.ApplicationId,
                    application.Role,
                    application.Stage.ToString(),
                    application.AppliedOn,
                    application.NoResponseDays,
                    Ghost(snapshot, application)?.Level))
                .ToList());
    }

    private GhostSignal? Ghost(CommunitySnapshot snapshot, TrackedApplication application) =>
        GhostSignalCalculator.Calculate(
            application,
            snapshot.Postings.GetValueOrDefault(application.PostingId),
            CompanyOf(snapshot, application),
            timeProvider.GetUtcNow(),
            settings.Value);

    private static CompanyOutcome? CompanyOf(CommunitySnapshot snapshot, TrackedApplication application) =>
        application.CompanyKey is { } key ? snapshot.Companies.GetValueOrDefault(key) : null;
}
