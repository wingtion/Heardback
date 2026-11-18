using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Matching.Cvs;
using Heardback.Modules.Matching.Persistence;
using Heardback.Modules.Matching.Skills;
using Heardback.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.MatchResults;

internal sealed class GetMatchHandler(IApplicationPostings postings, MatchingDbContext dbContext)
{
    private const int FullScore = 100;

    /// <summary>
    /// Compares one of the user's applications with their CV, worked out fresh on every read (decided
    /// 2026-11-18): always current after an edit or a new CV, and no LLM involved. The posting's skills are what
    /// its role and text name; the CV's include what those imply ("ASP.NET Core" shows ".NET"), minus the skills
    /// the user marked as not theirs.
    /// </summary>
    public async Task<Result<MatchResponse>> HandleAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var posting = await postings.FindAsync(applicationId, cancellationToken);
        if (posting is null)
        {
            return MatchingErrors.ApplicationNotFound;
        }

        var required = SkillFinder.Find(string.Join('\n', posting.Role, posting.PostingText));
        if (required.Count == 0)
        {
            var state = string.IsNullOrWhiteSpace(posting.PostingText)
                ? MatchState.NoPostingText
                : MatchState.NoSkillsInPosting;
            return new MatchResponse(state, Score: null, Matched: [], Missing: []);
        }

        var cv = await dbContext.Cvs
            .AsNoTracking()
            .Select(cv => new { cv.Text, cv.ExcludedSkills })
            .FirstOrDefaultAsync(cancellationToken);
        if (cv is null)
        {
            return new MatchResponse(MatchState.NoCv, Score: null, Matched: [], Missing: ToResponses(required));
        }

        var shown = CvSkills.Shown(cv.Text, cv.ExcludedSkills);
        var matched = required.Where(shown.Contains).ToList();
        var missing = required.Where(skill => !shown.Contains(skill)).ToList();
        var score = (int)Math.Round(FullScore * (double)matched.Count / required.Count, MidpointRounding.AwayFromZero);

        return new MatchResponse(MatchState.Ready, score, ToResponses(matched), ToResponses(missing));
    }

    private static List<SkillResponse> ToResponses(IEnumerable<Skill> skills) =>
        skills.Select(SkillResponse.From).ToList();
}
