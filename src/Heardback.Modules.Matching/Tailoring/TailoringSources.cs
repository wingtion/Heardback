using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Matching.MatchResults;
using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// Everything tailoring needs about one of the current user's applications, read through the per-user filters
/// (in a job, the current user is the one who asked).
/// </summary>
internal sealed class TailoringSources(
    IApplicationPostings postings,
    MatchingDbContext dbContext,
    GetMatchHandler getMatch)
{
    /// <returns>Null when the application does not exist or belongs to someone else.</returns>
    public async Task<TailoringSource?> FindAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var posting = await postings.FindAsync(applicationId, cancellationToken);
        if (posting is null)
        {
            return null;
        }

        var cvText = await dbContext.Cvs.AsNoTracking().Select(cv => cv.Text).FirstOrDefaultAsync(cancellationToken);
        var hasConsent = await dbContext.AiConsents.AnyAsync(cancellationToken);
        var match = await getMatch.HandleAsync(applicationId, cancellationToken);
        var matched = match.IsSuccess ? match.Value.Matched.Select(skill => skill.Name).ToList() : [];
        var missing = match.IsSuccess ? match.Value.Missing.Select(skill => skill.Name).ToList() : [];

        return new TailoringSource(posting, cvText, hasConsent, matched, missing);
    }
}
