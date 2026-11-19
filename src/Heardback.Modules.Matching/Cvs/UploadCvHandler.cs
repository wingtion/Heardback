using Heardback.Modules.Matching.Cvs.Domain;
using Heardback.Modules.Matching.Cvs.Extraction;
using Heardback.Modules.Matching.Persistence;
using Heardback.Shared.Privacy;
using Heardback.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Heardback.Modules.Matching.Cvs;

internal sealed partial class UploadCvHandler(
    MatchingDbContext dbContext,
    CvTextExtractor extractor,
    TimeProvider timeProvider,
    IOptions<MatchingSettings> settings,
    ILogger<UploadCvHandler> logger)
{
    private const string OneCvPerUserIndex = "ix_cvs_user_id";

    /// <summary>
    /// Reads the PDF in memory, removes personal details from its text and stores only that text, replacing the
    /// user's previous CV. The file itself is dropped when the request ends.
    /// </summary>
    public async Task<Result<CvResponse>> HandleAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return MatchingErrors.CvMissing;
        }

        var maxBytes = settings.Value.CvMaxBytes;
        if (file.Length > maxBytes)
        {
            return MatchingErrors.CvTooLarge(maxBytes);
        }

        var extracted = extractor.Extract(await ReadAsync(file, cancellationToken));
        if (!extracted.IsSuccess)
        {
            return extracted.Error;
        }

        var text = PersonalDetails.Strip(extracted.Value.Text);
        var cv = await SaveAsync(text, extracted.Value.PageCount, cancellationToken);

        LogUploaded(cv.PageCount, cv.Text.Length);
        return CvResponse.From(cv.UploadedAt, cv.PageCount, cv.Text, cv.ExcludedSkills);
    }

    private static async Task<byte[]> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);
        await source.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private async Task<Cv> SaveAsync(string text, int pageCount, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await dbContext.Cvs.FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            existing.Replace(text, pageCount, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var created = Cv.Create(text, pageCount, now);
        dbContext.Cvs.Add(created);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return created;
        }
        catch (DbUpdateException exception) when (IsSecondFirstUpload(exception))
        {
            // Two first uploads raced; the other one created the row. This upload is the newer one, so it wins.
            dbContext.ChangeTracker.Clear();
            var winner = await dbContext.Cvs.FirstAsync(cancellationToken);
            winner.Replace(text, pageCount, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return winner;
        }
    }

    private static bool IsSecondFirstUpload(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: OneCvPerUserIndex,
        };

    // Sizes only; the text is personal data and never logged.
    [LoggerMessage(Level = LogLevel.Information, Message = "CV uploaded ({PageCount} pages, {CharacterCount} characters)")]
    private partial void LogUploaded(int pageCount, int characterCount);
}
