using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Account;

/// <summary>Exports a user's applications with their timeline, notes, posting text and job summary (ADR-0015).</summary>
internal sealed class ApplicationsDataExporter(ApplicationsDbContext dbContext) : IUserDataExporter
{
    public string Section => "applications";

    public async Task<object> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        // The ownership filter limits this to the signed-in user, whose id this is.
        var applications = await dbContext.Applications.AsNoTracking()
            .OrderBy(application => application.AppliedOn)
            .ThenBy(application => application.CreatedAt)
            .Select(application => new
            {
                application.Id,
                application.Company,
                application.Role,
                application.Url,
                application.PostingText,
                application.ContentHash,
                application.Channel,
                application.AppliedOn,
                application.Status,
                application.Notes,
                application.CreatedAt,
                application.UpdatedAt,
                Timeline = application.Events
                    .OrderBy(entry => entry.OccurredOn)
                    .ThenBy(entry => entry.CreatedAt)
                    .Select(entry => new ApplicationEventExport(
                        entry.Status, entry.OccurredOn, entry.Note, entry.IsSubmission, entry.CreatedAt))
                    .ToList(),
            })
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var summaries = await JobSummariesAsync(
            [.. applications.Select(application => application.ContentHash).OfType<string>().Distinct()],
            cancellationToken);

        return applications
            .Select(application => new ApplicationExport(
                application.Id,
                application.Company,
                application.Role,
                application.Url,
                application.PostingText,
                application.Channel,
                application.AppliedOn,
                application.Status,
                application.Notes,
                application.CreatedAt,
                application.UpdatedAt,
                application.Timeline,
                application.ContentHash is { } hash ? summaries.GetValueOrDefault(hash) : null))
            .ToList();
    }

    // Analyses hold no user id; only those of the user's own posting text are read (ADR-0008).
    private async Task<Dictionary<string, PostingAnalysisExport>> JobSummariesAsync(
        IReadOnlyCollection<string> contentHashes,
        CancellationToken cancellationToken) =>
        await dbContext.PostingAnalyses.AsNoTracking()
            .Where(analysis => contentHashes.Contains(analysis.ContentHash))
            .ToDictionaryAsync(
                analysis => analysis.ContentHash,
                analysis => new PostingAnalysisExport(
                    analysis.Company,
                    analysis.Title,
                    analysis.Seniority,
                    analysis.WorkMode,
                    analysis.Skills,
                    analysis.Location,
                    analysis.Provider,
                    analysis.Model,
                    analysis.AnalyzedAt),
                cancellationToken);
}
