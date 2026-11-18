using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Applications.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

/// <summary>Serves <see cref="IApplicationPostings"/>; the ownership filter keeps it to the caller's applications.</summary>
internal sealed class ApplicationPostings(ApplicationsDbContext dbContext) : IApplicationPostings
{
    public Task<ApplicationPosting?> FindAsync(Guid applicationId, CancellationToken cancellationToken) =>
        dbContext.Applications
            .AsNoTracking()
            .Where(application => application.Id == applicationId)
            .Select(application => new ApplicationPosting(application.Company, application.Role, application.PostingText))
            .FirstOrDefaultAsync(cancellationToken);
}
