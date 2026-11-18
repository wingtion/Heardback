using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Cvs;

internal sealed class GetCvHandler(MatchingDbContext dbContext)
{
    /// <summary>The signed-in user's CV, or null when they have not uploaded one.</summary>
    public Task<CvResponse?> HandleAsync(CancellationToken cancellationToken) =>
        dbContext.Cvs
            .AsNoTracking()
            .Select(cv => new CvResponse(cv.UploadedAt, cv.PageCount, cv.Text))
            .FirstOrDefaultAsync(cancellationToken);
}
