using System.Globalization;
using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// Writes tailoring suggestions for one application. Runs as the user who asked (the job queue sets them as the
/// current user), so the CV, consent and application are read through the normal per-user filters. It reads the
/// current data when it runs: withdrawn consent, a deleted CV or deleted posting text ends the job without an LLM
/// call, and the failure gives the use back.
/// </summary>
internal sealed class TailoringJobHandler(
    TailoringSources sources,
    MatchingDbContext dbContext,
    IStructuredLlm llm,
    TailoringPrompt prompt,
    TimeProvider timeProvider) : IAnalysisJobHandler
{
    public const string JobKind = "cv-tailoring";

    public const string NoConsent = "no_consent";
    public const string NothingToTailor = "nothing_to_tailor";
    public const string NoGroundedSuggestions = "no_grounded_suggestions";

    public string Kind => JobKind;

    public static string SubjectKeyFor(Guid applicationId) => applicationId.ToString("N", CultureInfo.InvariantCulture);

    public async Task<AnalysisJobOutcome> RunAsync(AnalysisJobContext job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var applicationId = Guid.ParseExact(job.SubjectKey, "N");
        var source = await sources.FindAsync(applicationId, cancellationToken);
        if (source is null || source.CvText is null || !source.HasPostingText)
        {
            return AnalysisJobOutcome.Failed(NothingToTailor);
        }

        if (!source.HasConsent)
        {
            return AnalysisJobOutcome.Failed(NoConsent);
        }

        var input = source.ToInput();
        var result = await llm.RunAsync(prompt, input, cancellationToken);
        if (!result.IsSuccess)
        {
            return LlmJobOutcome.FromFailure(result);
        }

        var grounded = TailoringGrounding.Keep(result.Value.Suggestions, input);
        if (grounded.Count == 0)
        {
            return AnalysisJobOutcome.Failed(NoGroundedSuggestions);
        }

        var answer = result.Value with { Suggestions = grounded };

        var now = timeProvider.GetUtcNow();
        var stored = await dbContext.TailoringResults
            .FirstOrDefaultAsync(candidate => candidate.ApplicationId == applicationId, cancellationToken);
        if (stored is null)
        {
            stored = TailoringResult.Create(applicationId, now);
            dbContext.TailoringResults.Add(stored);
        }

        stored.Replace(answer, result.Completion, source.CvFingerprint!, source.PostingFingerprint, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AnalysisJobOutcome.Succeeded();
    }
}
