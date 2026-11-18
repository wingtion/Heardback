using Heardback.Modules.Postings.Contracts;
using Heardback.Modules.Postings.Domain;
using Heardback.Modules.Postings.Persistence;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Heardback.Modules.Postings.Catalogue;

internal sealed partial class JobPostings(
    PostingsDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<JobPostings> logger) : IJobPostings
{
    // One lookup, one insert; if the insert loses a race, the second lookup finds the winner's row.
    private const int MaxAttempts = 2;

    public async Task<Result<Guid>> ResolveAsync(PostingSubmission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        PostingUrlParts? link = null;
        if (!string.IsNullOrWhiteSpace(submission.Url) && !PostingUrl.TryParse(submission.Url, out link))
        {
            return PostingErrors.InvalidUrl;
        }

        var normalizedUrl = link?.Key;
        var contentHash = PostingContentHash.ComputeOrNull(submission.Company, submission.Title, submission.Text);
        var now = timeProvider.GetUtcNow();
        var posting = JobPosting.Create(
            link, submission.Company, submission.Title, submission.Text, contentHash, now);

        // Nothing to recognise it by, so it can only ever be a new posting.
        if (normalizedUrl is null && contentHash is null)
        {
            return await TryAddAsync(posting, cancellationToken) ?? throw UnexpectedConflict();
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (await FindMatchAsync(normalizedUrl, contentHash, cancellationToken) is { } existingId)
            {
                await RecordSubmittedAgainAsync(existingId, now, cancellationToken);
                return existingId;
            }

            if (await TryAddAsync(posting, cancellationToken) is { } createdId)
            {
                return createdId;
            }
        }

        throw UnexpectedConflict();
    }

    public async Task<Guid?> FindIdByUrlAsync(string url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (!PostingUrl.TryNormalize(url, out var normalizedUrl))
        {
            return null;
        }

        return await dbContext.JobPostings.AsNoTracking()
            .Where(posting => posting.NormalizedUrl == normalizedUrl)
            .Select(posting => (Guid?)posting.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // A link match wins over a text match: the link names the posting, while the same text can be reposted.
    private async Task<Guid?> FindMatchAsync(
        string? normalizedUrl,
        string? contentHash,
        CancellationToken cancellationToken)
    {
        var matches = await dbContext.JobPostings.AsNoTracking()
            .Where(posting => (normalizedUrl != null && posting.NormalizedUrl == normalizedUrl)
                || (contentHash != null && posting.ContentHash == contentHash))
            .Select(posting => new { posting.Id, IsUrlMatch = posting.NormalizedUrl == normalizedUrl })
            .ToListAsync(cancellationToken);

        return matches.OrderByDescending(match => match.IsUrlMatch).Select(match => (Guid?)match.Id).FirstOrDefault();
    }

    private Task<int> RecordSubmittedAgainAsync(Guid postingId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.JobPostings
            .Where(posting => posting.Id == postingId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(posting => posting.LastSeenAt, now)
                    .SetProperty(posting => posting.TimesAdded, posting => posting.TimesAdded + 1),
                cancellationToken);

    private async Task<Guid?> TryAddAsync(JobPosting posting, CancellationToken cancellationToken)
    {
        dbContext.JobPostings.Add(posting);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            LogPostingCreated(posting.Id);
            return posting.Id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Someone else created the same posting between our lookup and insert.
            dbContext.ChangeTracker.Clear();
            return null;
        }
    }

    private static InvalidOperationException UnexpectedConflict() =>
        new("A job posting kept conflicting with an existing row that could not be found.");

    [LoggerMessage(Level = LogLevel.Information, Message = "Job posting {PostingId} created")]
    private partial void LogPostingCreated(Guid postingId);
}
