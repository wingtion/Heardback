using Heardback.Modules.Matching.Persistence;
using Heardback.Modules.Matching.Skills;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Matching.Cvs;

internal sealed partial class ExcludeSkillsHandler(MatchingDbContext dbContext, ILogger<ExcludeSkillsHandler> logger)
{
    private static readonly HashSet<string> KnownSkills =
        SkillCatalog.All.Select(skill => skill.Name).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Stores which skills the user says are not theirs ("Not my skill"). The whole list is sent each time, so
    /// repeating a request, or two arriving at once, leaves a list the user actually chose.
    /// </summary>
    public async Task<Result<CvResponse>> HandleAsync(ExcludeSkillsRequest request, CancellationToken cancellationToken)
    {
        var names = request.Skills!;
        if (names.FirstOrDefault(name => !KnownSkills.Contains(name)) is { } unknown)
        {
            return MatchingErrors.UnknownSkill(unknown);
        }

        var cv = await dbContext.Cvs.FirstOrDefaultAsync(cancellationToken);
        if (cv is null)
        {
            return MatchingErrors.CvNotFound;
        }

        cv.ExcludeSkills(names);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogExcluded(cv.ExcludedSkills.Length);
        return CvResponse.From(cv.UploadedAt, cv.PageCount, cv.Text, cv.ExcludedSkills);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "CV skills excluded ({ExcludedCount} in total)")]
    private partial void LogExcluded(int excludedCount);
}
