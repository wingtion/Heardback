using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>
/// Queues an analysis of the application's posting text when the user asks for one (decided 2026-11-19: on
/// request only, so test postings never use the LLM). Text someone already had analysed is ready at once and
/// uses none of the daily limit.
/// </summary>
internal sealed class RequestPostingAnalysisHandler(
    IAnalysisJobs jobs,
    ICurrentUser currentUser,
    IOptions<ApplicationsSettings> settings,
    GetPostingAnalysisHandler getAnalysis)
{
    public async Task<Result<PostingAnalysisResponse>> HandleAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await getAnalysis.FindAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return ApplicationsErrors.NotFound;
        }

        if (application.ContentHash is not { } contentHash)
        {
            return ApplicationsErrors.NoPostingText;
        }

        var current = await getAnalysis.ForContentHashAsync(contentHash, cancellationToken);
        if (current.State is PostingAnalysisState.Ready or PostingAnalysisState.Queued
            or PostingAnalysisState.Running or PostingAnalysisState.WaitingForCapacity)
        {
            return current;
        }

        var limit = settings.Value.PostingAnalysesPerDay;
        var queued = await jobs.EnqueueForUserAsync(
            PostingAnalysisJobHandler.JobKind, contentHash, currentUser.GetRequiredUserId(), limit, cancellationToken);

        return queued switch
        {
            EnqueueResult.LlmDisabled => ApplicationsErrors.AnalysisUnavailable,
            EnqueueResult.QuotaExceeded => ApplicationsErrors.AnalysisQuotaExceeded(limit, current.Usage.ResetsAt),
            _ => await getAnalysis.ForContentHashAsync(contentHash, cancellationToken),
        };
    }
}
