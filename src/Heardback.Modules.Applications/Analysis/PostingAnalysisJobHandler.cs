using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Modules.Applications.Persistence;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>
/// Runs a posting-analysis job. The job holds only the content hash; the text is read from an application with
/// that hash when the job runs, so a job for text that was since deleted or edited simply has nothing to do.
/// </summary>
internal sealed class PostingAnalysisJobHandler(
    ApplicationsDbContext dbContext,
    IStructuredLlm llm,
    PostingAnalysisPrompt prompt,
    TimeProvider timeProvider) : IAnalysisJobHandler
{
    public const string JobKind = "posting-analysis";

    public string Kind => JobKind;

    public async Task<AnalysisJobOutcome> RunAsync(AnalysisJobContext job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        var contentHash = job.SubjectKey;

        var existing = await dbContext.PostingAnalyses.FindAsync([contentHash], cancellationToken);
        if (existing?.PromptVersion == prompt.Version)
        {
            return AnalysisJobOutcome.Succeeded();
        }

        var input = await FindTextAsync(contentHash, cancellationToken);
        if (input is null)
        {
            return AnalysisJobOutcome.Succeeded();
        }

        var result = await llm.RunAsync(prompt, input, cancellationToken);
        if (!result.IsSuccess)
        {
            return LlmFailure.ToJobOutcome(result.Outcome, result.RetryAfter);
        }

        var now = timeProvider.GetUtcNow();
        if (existing is null)
        {
            dbContext.PostingAnalyses.Add(PostingAnalysis.Create(contentHash, result.Value, result.Completion, now));
        }
        else
        {
            existing.Replace(result.Value, result.Completion, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return AnalysisJobOutcome.Succeeded();
    }

    // The worker has no signed-in user, so the ownership filter is lifted for this one read. Every application
    // with this hash has the same company, role and text, so it does not matter whose row is read.
    private Task<PostingAnalysisInput?> FindTextAsync(string contentHash, CancellationToken cancellationToken) =>
        dbContext.Applications
            .IgnoreQueryFilters([ModuleDbContext.UserOwnershipFilter])
            .AsNoTracking()
            .Where(application => application.ContentHash == contentHash && application.PostingText != null)
            .Select(application => new PostingAnalysisInput(application.Company, application.Role, application.PostingText!))
            .FirstOrDefaultAsync(cancellationToken);
}
