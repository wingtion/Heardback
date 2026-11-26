using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// Queues tailoring suggestions for one application. Needs posting text, a CV and the user's consent. Current
/// suggestions are returned as they are; out-of-date ones are made again.
/// </summary>
internal sealed class RequestTailoringHandler(
    TailoringSources sources,
    GetTailoringHandler getTailoring,
    IAnalysisJobs jobs,
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

        var current = await getTailoring.ForSourceAsync(applicationId, source, cancellationToken);
        switch (current.State)
        {
            case TailoringState.Ready when current.Tailoring is { IsOutdated: false }:
            case TailoringState.Queued or TailoringState.Running or TailoringState.WaitingForCapacity:
                return current;
            case TailoringState.Unavailable:
                return MatchingErrors.TailoringUnavailable;
            case TailoringState.NoPostingText:
                return MatchingErrors.TailoringNeedsPostingText;
            case TailoringState.NoCv:
                return MatchingErrors.TailoringNeedsCv;
            case TailoringState.ConsentRequired:
                return MatchingErrors.AiConsentRequired;
        }

        // An out-of-date result is reported as Ready before the checks above, so remaking it checks them here.
        if (!source.HasPostingText)
        {
            return MatchingErrors.TailoringNeedsPostingText;
        }

        if (source.CvText is null)
        {
            return MatchingErrors.TailoringNeedsCv;
        }

        if (!source.HasConsent)
        {
            return MatchingErrors.AiConsentRequired;
        }

        var limit = settings.Value.TailoringPerDayFor(currentUser);
        var queued = await jobs.EnqueueForUserAsync(
            TailoringJobHandler.JobKind,
            TailoringJobHandler.SubjectKeyFor(applicationId),
            currentUser.GetRequiredUserId(),
            limit,
            cancellationToken);

        return queued switch
        {
            EnqueueResult.LlmDisabled => MatchingErrors.TailoringUnavailable,
            EnqueueResult.QuotaExceeded => MatchingErrors.TailoringQuotaExceeded(limit, current.Usage.ResetsAt),
            _ => await getTailoring.ForSourceAsync(applicationId, source, cancellationToken),
        };
    }
}
