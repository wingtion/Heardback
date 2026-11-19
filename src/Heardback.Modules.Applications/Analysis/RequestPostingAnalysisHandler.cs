using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>
/// Queues an analysis of the application's posting text when the user asks for one (decided 2026-11-19: on
/// request only, so test postings never use the LLM). Text someone already had analysed is ready at once.
/// </summary>
internal sealed class RequestPostingAnalysisHandler(
    ApplicationsDbContext dbContext,
    IAnalysisJobs jobs,
    ICurrentUser currentUser,
    GetPostingAnalysisHandler getAnalysis)
{
    public async Task<Result<PostingAnalysisResponse>> HandleAsync(Guid applicationId, CancellationToken cancellationToken)
    {
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
            return ApplicationsErrors.NoPostingText;
        }

        var current = await getAnalysis.ForContentHashAsync(application.ContentHash, cancellationToken);
        if (current.State is PostingAnalysisState.Ready or PostingAnalysisState.Queued or PostingAnalysisState.Running)
        {
            return current;
        }

        var queued = await jobs.EnqueueAsync(
            PostingAnalysisJobHandler.JobKind,
            application.ContentHash,
            currentUser.GetRequiredUserId(),
            cancellationToken);
        if (queued == EnqueueResult.LlmDisabled)
        {
            return ApplicationsErrors.AnalysisUnavailable;
        }

        return await getAnalysis.ForContentHashAsync(application.ContentHash, cancellationToken);
    }
}
