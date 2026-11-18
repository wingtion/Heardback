using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Read;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Board;

internal sealed class GetBoardHandler(
    ApplicationsDbContext dbContext,
    NoResponseClock noResponse,
    IOptions<ApplicationsSettings> settings)
{
    public async Task<BoardResponse> HandleAsync(BoardFilter filter, CancellationToken cancellationToken)
    {
        var applications = dbContext.Applications.AsNoTracking().Filter(filter.ToApplicationFilter(), noResponse);

        var totals = await applications
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(total => total.Status, total => total.Count, cancellationToken);

        var columns = new List<BoardColumn>();
        foreach (var status in Enum.GetValues<ApplicationStatus>())
        {
            var items = await applications
                .Where(application => application.Status == status)
                .ToListItems(noResponse)
                .Take(settings.Value.BoardColumnLimit)
                .ToListAsync(cancellationToken);

            columns.Add(new BoardColumn(status, totals.GetValueOrDefault(status), items));
        }

        return new BoardResponse(columns);
    }
}
