using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Cvs;

internal sealed class GetCvHandler(MatchingDbContext dbContext)
{
    /// <summary>The signed-in user's CV, or null when they have not uploaded one.</summary>
    public async Task<CvResponse?> HandleAsync(CancellationToken cancellationToken)
    {
        var stored = await dbContext.Cvs
            .AsNoTracking()
            .Select(cv => new { cv.UploadedAt, cv.PageCount, cv.Text, cv.ExcludedSkills })
            .FirstOrDefaultAsync(cancellationToken);

        return stored is null
            ? null
            : CvResponse.From(stored.UploadedAt, stored.PageCount, stored.Text, stored.ExcludedSkills);
    }
}
