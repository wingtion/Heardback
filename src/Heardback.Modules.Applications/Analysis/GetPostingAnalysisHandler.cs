using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Infrastructure.Llm.Quotas;
using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Analysis;

internal sealed class GetPostingAnalysisHandler(
    ApplicationsDbContext dbContext,
    IAnalysisJobs jobs,
    ILlmClient llmClient,
    ICurrentUser currentUser,
    IOptions<ApplicationsSettings> settings)
{
    public async Task<Result<PostingAnalysisResponse>> HandleAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        // Another user's application is hidden by the ownership filter, so it is simply "not found".
        var application = await FindAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return ApplicationsErrors.NotFound;
        }

        if (application.ContentHash is not { } contentHash)
        {
            return new PostingAnalysisResponse(
                PostingAnalysisState.NoPostingText, null, await GetUsageAsync(cancellationToken));
        }

        return await ForContentHashAsync(contentHash, cancellationToken);
    }

    /// <summary>The signed-in user's application, or null when it does not exist or is someone else's.</summary>
    public Task<AnalysedApplication?> FindAsync(Guid applicationId, CancellationToken cancellationToken) =>
        dbContext.Applications
            .AsNoTracking()
            .Where(candidate => candidate.Id == applicationId)
            .Select(candidate => new AnalysedApplication(candidate.PostingText == null ? null : candidate.ContentHash))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<LlmUsage> GetUsageAsync(CancellationToken cancellationToken) =>
        jobs.GetUsageAsync(
            PostingAnalysisJobHandler.JobKind,
            currentUser.GetRequiredUserId(),
            settings.Value.PostingAnalysesPerDay,
            cancellationToken);

    public async Task<PostingAnalysisResponse> ForContentHashAsync(string contentHash, CancellationToken cancellationToken)
    {
        var usage = await GetUsageAsync(cancellationToken);
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
            return new PostingAnalysisResponse(PostingAnalysisState.Ready, analysis, usage);
        }

        if (!llmClient.IsEnabled)
        {
            return new PostingAnalysisResponse(PostingAnalysisState.Unavailable, null, usage);
        }

        var job = await jobs.GetLatestAsync(PostingAnalysisJobHandler.JobKind, contentHash, cancellationToken);
        var state = job switch
        {
            null => PostingAnalysisState.NotRequested,
            { Status: AnalysisJobStatus.Pending, LastError: LlmJobOutcome.DailyCap } => PostingAnalysisState.WaitingForCapacity,
            { Status: AnalysisJobStatus.Pending } => PostingAnalysisState.Queued,
            { Status: AnalysisJobStatus.Running } => PostingAnalysisState.Running,

            // Succeeded without an analysis: the text was deleted or edited while the job waited.
            { Status: AnalysisJobStatus.Succeeded } => PostingAnalysisState.NotRequested,
            { Status: AnalysisJobStatus.Failed } => PostingAnalysisState.Failed,
            _ => throw new InvalidOperationException($"Unknown job status {job.Status}."),
        };
        return new PostingAnalysisResponse(state, null, usage);
    }
}
