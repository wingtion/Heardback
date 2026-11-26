using Heardback.Modules.Applications.Domain;
using Heardback.Shared.Users;

namespace Heardback.Modules.Applications.Community;

/// <summary>
/// Whose applications a viewer's community statistics may count (ADR-0018). Members count only members: demo data
/// never mixes into real signals. Demo visitors count only the demo neighbors, so an anonymous visitor never sees
/// anything derived from members' applications, not even in aggregate.
/// </summary>
internal sealed class CommunityAudience
{
    private readonly bool _onlyListed;
    private readonly List<Guid> _userIds;

    private CommunityAudience(bool onlyListed, List<Guid> userIds)
    {
        _onlyListed = onlyListed;
        _userIds = userIds;
    }

    public static async Task<CommunityAudience> ForAsync(
        ICurrentUser viewer,
        IDemoAccounts demoAccounts,
        CancellationToken cancellationToken)
    {
        var demo = await demoAccounts.ListAsync(cancellationToken);
        return viewer.IsDemo
            ? new CommunityAudience(onlyListed: true, [.. demo.Neighbors])
            : new CommunityAudience(onlyListed: false, [.. demo.All]);
    }

    /// <summary>Keeps the applications this audience may count, plus the viewer's own.</summary>
    public IQueryable<Application> Filter(IQueryable<Application> applications, Guid viewerId)
    {
        var userIds = _userIds;
        return _onlyListed
            ? applications.Where(application => application.UserId == viewerId || userIds.Contains(application.UserId))
            : applications.Where(application => application.UserId == viewerId || !userIds.Contains(application.UserId));
    }
}
