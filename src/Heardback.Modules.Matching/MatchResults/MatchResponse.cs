using Heardback.Modules.Matching.Skills;

namespace Heardback.Modules.Matching.MatchResults;

/// <summary>
/// How an application's posting compares with the user's CV. <see cref="Score"/> is the share of the posting's
/// skills the CV shows, 0 to 100, or null when there is nothing to compare.
/// </summary>
internal sealed record MatchResponse(
    MatchState State,
    int? Score,
    IReadOnlyList<SkillResponse> Matched,
    IReadOnlyList<SkillResponse> Missing);

/// <summary>A dictionary skill with its group, for display.</summary>
internal sealed record SkillResponse(string Name, SkillCategory Category)
{
    public static SkillResponse From(Skill skill) => new(skill.Name, skill.Category);
}
