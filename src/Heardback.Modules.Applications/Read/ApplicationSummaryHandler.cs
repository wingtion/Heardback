using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Responses;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

internal sealed class ApplicationSummaryHandler(ApplicationsDbContext dbContext, NoResponseClock noResponse)
{
    public async Task<ApplicationSummaryResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var applications = dbContext.Applications.AsNoTracking();
        var total = await applications.CountAsync(cancellationToken);
        var withoutResponse = await applications.CountAsync(noResponse.Matches(), cancellationToken);

        return new ApplicationSummaryResponse(total, withoutResponse);
    }
}
