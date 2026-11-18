using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Heardback.Shared.Paging;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

internal sealed class ListApplicationsHandler(ApplicationsDbContext dbContext, NoResponseClock noResponse)
{
    /// <summary>Newest change first.</summary>
    public Task<PagedResponse<ApplicationListItem>> HandleAsync(
        PageRequest page,
        ApplicationFilter filter,
        CancellationToken cancellationToken) =>
        dbContext.Applications
            .AsNoTracking()
            .Filter(filter, noResponse)
            .ToListItems(noResponse)
            .ToPagedResponseAsync(page, cancellationToken);
}
