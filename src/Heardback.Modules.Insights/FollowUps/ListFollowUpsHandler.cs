using Heardback.Modules.Applications.Contracts;
using Heardback.Shared.Paging;

namespace Heardback.Modules.Insights.FollowUps;

/// <summary>
/// The applications that show "No response · N days", longest silence first (ADR-0011). Marking one as followed
/// up is an ordinary note, which moves its last activity; no reminder state is stored.
/// </summary>
internal sealed class ListFollowUpsHandler(IApplicationHistory history)
{
    public async Task<PagedResponse<FollowUpItem>> HandleAsync(PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var applications = await history.ListAsync(cancellationToken);
        var due = applications
            .Where(application => application.NoResponseDays is not null)
            .OrderByDescending(application => application.NoResponseDays)
            .ThenBy(application => application.Company, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(application => application.ApplicationId)
            .Select(application => new FollowUpItem(
                application.ApplicationId,
                application.Company,
                application.Role,
                application.Channel,
                application.AppliedOn,
                application.LastActivityOn,
                application.NoResponseDays!.Value))
            .ToList();

        return new PagedResponse<FollowUpItem>(
            due.Skip(page.Skip).Take(page.PageSize).ToList(), page.Page, page.PageSize, due.Count);
    }
}
