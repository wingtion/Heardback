using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Analysis;

internal sealed class GetPostingAnalysisHandler(
    ApplicationsDbContext dbContext,
    IAnalysisJobs jobs,
    ILlmClient llmClient)
{
    public async Task<Result<PostingAnalysisResponse>> HandleAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        // Another user's application is hidden by the ownership filter, so it is simply "not found".
        var application = await dbContext.Applications
            .AsNoTracking()
            .Where(candidate => candidate.Id == applicationId)
            .Select(candidate => new { candidate.ContentHash, HasText = candidate.PostingText != null })
            .FirstOrDefaultAsync(cancellationToken);
        if (application is null)
        {
            return ApplicationsErrors.NotFound;
        }

        if (application.ContentHash is null || !application.HasText)
        {
            return new PostingAnalysisResponse(PostingAnalysisState.NoPostingText, null);
        }

        return await ForContentHashAsync(application.ContentHash, cancellationToken);
    }

    public async Task<PostingAnalysisResponse> ForContentHashAsync(string contentHash, CancellationToken cancellationToken)
    {
        var analysis = await dbContext.PostingAnalyses
            .AsNoTracking()
            .Where(candidate => candidate.ContentHash == contentHash)
            .Select(candidate => new PostingAnalysisFields(
                candidate.Company,
                candidate.Title,
                candidate.Seniority,
                candidate.WorkMode,
                candidate.Skills,
                candidate.Location,
                candidate.AnalyzedAt))
            .FirstOrDefaultAsync(cancellationToken);
        if (analysis is not null)
        {
            return new PostingAnalysisResponse(PostingAnalysisState.Ready, analysis);
        }

        if (!llmClient.IsEnabled)
        {
            return new PostingAnalysisResponse(PostingAnalysisState.Unavailable, null);
        }

        var job = await jobs.GetLatestAsync(PostingAnalysisJobHandler.JobKind, contentHash, cancellationToken);
        var state = job?.Status switch
        {
            null => PostingAnalysisState.NotRequested,
            AnalysisJobStatus.Pending => PostingAnalysisState.Queued,
            AnalysisJobStatus.Running => PostingAnalysisState.Running,

            // Succeeded without an analysis: the text was deleted or edited while the job waited.
            AnalysisJobStatus.Succeeded => PostingAnalysisState.NotRequested,
            AnalysisJobStatus.Failed => PostingAnalysisState.Failed,
            _ => throw new InvalidOperationException($"Unknown job status {job.Status}."),
        };
        return new PostingAnalysisResponse(state, null);
    }
}
