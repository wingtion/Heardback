using Heardback.Modules.Matching.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Account;

/// <summary>Exports a user's CV text, AI consent and tailoring suggestions (ADR-0015).</summary>
internal sealed class MatchingDataExporter(MatchingDbContext dbContext) : IUserDataExporter
{
    public string Section => "cv";

    public async Task<object> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        // The ownership filter limits every query to the signed-in user, whose id this is.
        var cv = await dbContext.Cvs.AsNoTracking()
            .Select(stored => new CvExport(stored.Text, stored.PageCount, stored.UploadedAt, stored.ExcludedSkills))
            .FirstOrDefaultAsync(cancellationToken);

        var consentGivenAt = await dbContext.AiConsents.AsNoTracking()
            .Select(consent => (DateTimeOffset?)consent.GrantedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var tailoring = await dbContext.TailoringResults.AsNoTracking()
            .OrderBy(result => result.CreatedAt)
            .ToListAsync(cancellationToken);

        return new MatchingExport(
            cv,
            consentGivenAt,
            [.. tailoring.Select(result => new TailoringExport(
                result.ApplicationId,
                result.Summary,
                [.. result.Suggestions.Select(advice => new TailoringSuggestionExport(advice.CvSection, advice.CvQuote, advice.Advice))],
                result.Provider,
                result.Model,
                result.CreatedAt))]);
    }
}
