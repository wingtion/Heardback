using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Infrastructure.Llm.Quotas;
using Heardback.Modules.Matching.Persistence;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Matching.Tailoring;

internal sealed class GetTailoringHandler(
    TailoringSources sources,
    MatchingDbContext dbContext,
    IAnalysisJobs jobs,
    ILlmClient llmClient,
    ICurrentUser currentUser,
    IOptions<MatchingSettings> settings)
{
    public async Task<Result<TailoringResponse>> HandleAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var source = await sources.FindAsync(applicationId, cancellationToken);
        if (source is null)
        {
            return MatchingErrors.ApplicationNotFound;
        }

        return await ForSourceAsync(applicationId, source, cancellationToken);
    }

    public Task<LlmUsage> GetUsageAsync(CancellationToken cancellationToken) =>
        jobs.GetUsageAsync(
            TailoringJobHandler.JobKind, currentUser.GetRequiredUserId(), settings.Value.TailoringPerDay, cancellationToken);

    public async Task<TailoringResponse> ForSourceAsync(
        Guid applicationId,
        TailoringSource source,
        CancellationToken cancellationToken)
    {
        var usage = await GetUsageAsync(cancellationToken);
        var stored = await dbContext.TailoringResults
            .AsNoTracking()
            .Where(result => result.ApplicationId == applicationId)
            .Select(result => new
            {
                result.Summary,
                Suggestions = result.Suggestions.Select(suggestion =>
                    new TailoringAdvice(suggestion.CvSection, suggestion.CvQuote, suggestion.Advice)).ToList(),
                result.CvFingerprint,
                result.PostingFingerprint,
                result.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (stored is not null)
        {
            var isOutdated = stored.CvFingerprint != source.CvFingerprint
                || stored.PostingFingerprint != source.PostingFingerprint;
            var details = new TailoringDetails(stored.Summary, stored.Suggestions, isOutdated, stored.CreatedAt);

            // Out-of-date suggestions being made again: report the job, and keep showing the old ones meanwhile.
            var remaking = isOutdated ? await JobStateAsync(applicationId, cancellationToken) : TailoringState.Ready;
            var shown = remaking is TailoringState.Queued or TailoringState.Running or TailoringState.WaitingForCapacity
                ? remaking
                : TailoringState.Ready;
            return Response(shown, details);
        }

        var state = await StateWithoutResultAsync(applicationId, source, cancellationToken);
        return Response(state, null);

        TailoringResponse Response(TailoringState state, TailoringDetails? details) =>
            new(state, details, source.MissingSkills, usage);
    }

    private async Task<TailoringState> StateWithoutResultAsync(
        Guid applicationId,
        TailoringSource source,
        CancellationToken cancellationToken)
    {
        if (!llmClient.IsEnabled)
        {
            return TailoringState.Unavailable;
        }

        if (!source.HasPostingText)
        {
            return TailoringState.NoPostingText;
        }

        if (source.CvText is null)
        {
            return TailoringState.NoCv;
        }

        if (!source.HasConsent)
        {
            return TailoringState.ConsentRequired;
        }

        return await JobStateAsync(applicationId, cancellationToken);
    }

    private async Task<TailoringState> JobStateAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var job = await jobs.GetLatestAsync(
            TailoringJobHandler.JobKind, TailoringJobHandler.SubjectKeyFor(applicationId), cancellationToken);
        return job switch
        {
            null => TailoringState.NotRequested,
            { Status: AnalysisJobStatus.Pending, LastError: LlmJobOutcome.DailyCap } => TailoringState.WaitingForCapacity,
            { Status: AnalysisJobStatus.Pending } => TailoringState.Queued,
            { Status: AnalysisJobStatus.Running } => TailoringState.Running,

            // Succeeded without a stored result: the result was deleted with the CV or the consent.
            { Status: AnalysisJobStatus.Succeeded } => TailoringState.NotRequested,
            { Status: AnalysisJobStatus.Failed } => TailoringState.Failed,
            _ => throw new InvalidOperationException($"Unknown job status {job.Status}."),
        };
    }
}
