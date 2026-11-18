using System.Text.RegularExpressions;

namespace Heardback.Modules.Matching.Skills;

/// <summary>
/// Finds dictionary skills in free text (a CV or a job posting). Deterministic and offline: no LLM (PLAN.md §5).
/// </summary>
internal static class SkillFinder
{
    // A skill name must not be glued to other name characters: "Java" is not found in "JavaScript", ".NET" not in
    // "ASP.NET", "SQL" not in "T-SQL". A following dot only blocks when more name follows ("React.js").
    private const string Before = @"(?<![\p{L}\p{N}_#+.\-@])";
    private const string After = @"(?![\p{L}\p{N}_#+@]|\.[\p{L}\p{N}])";

    // Exact aliases are letters or ordinary words ("C", "Go", "REST"), so hyphens, apostrophes and ampersands
    // block them too: "C-level", "Go-to-market", "R&D".
    private const string ExactBefore = @"(?<![\p{L}\p{N}_#+.\-@'’&])";
    private const string ExactAfter = @"(?![\p{L}\p{N}_#+@\-'’&]|\.[\p{L}\p{N}])";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly List<AliasPattern> Patterns = SkillCatalog.All
        .SelectMany(skill =>
            skill.Aliases.Select(alias => Pattern(skill, alias, exact: false))
                .Concat(skill.ExactAliases.Select(alias => Pattern(skill, alias, exact: true))))
        .ToList();

    private static readonly Dictionary<string, Skill> ByName =
        SkillCatalog.All.ToDictionary(skill => skill.Name, StringComparer.Ordinal);

    /// <summary>
    /// Skills the text names, in dictionary order. Where names overlap, the longest wins: "React Native" is not
    /// also "React", "SQL Server" is not also "SQL".
    /// </summary>
    public static IReadOnlyList<Skill> Find(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var hits = Patterns
            .SelectMany(pattern => pattern.Regex.Matches(text).Select(match => (match.Index, match.Length, pattern.Skill)))
            .OrderByDescending(hit => hit.Length)
            .ThenBy(hit => hit.Index);

        var taken = new bool[text.Length];
        var found = new HashSet<Skill>();
        foreach (var (index, length, skill) in hits)
        {
            if (taken.AsSpan(index, length).Contains(true))
            {
                continue;
            }

            taken.AsSpan(index, length).Fill(true);
            found.Add(skill);
        }

        return InCatalogOrder(found);
    }

    /// <summary>
    /// The skills plus everything they imply, transitively: a CV with "ASP.NET Core" also shows ".NET". Used for
    /// the CV side only; a posting's requirements are what it actually names.
    /// </summary>
    public static IReadOnlyList<Skill> WithImplied(IEnumerable<Skill> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        var found = new HashSet<Skill>(skills);
        var pending = new Queue<Skill>(found);
        while (pending.TryDequeue(out var skill))
        {
            foreach (var implied in skill.Implies.Select(name => ByName[name]))
            {
                if (found.Add(implied))
                {
                    pending.Enqueue(implied);
                }
            }
        }

        return InCatalogOrder(found);
    }

    private static List<Skill> InCatalogOrder(HashSet<Skill> skills) =>
        SkillCatalog.All.Where(skills.Contains).ToList();

    private static AliasPattern Pattern(Skill skill, string alias, bool exact)
    {
        // Any run of whitespace between words: "React  Native", "Node\nJS".
        var body = Regex.Escape(alias).Replace(@"\ ", @"\s+", StringComparison.Ordinal);
        var options = RegexOptions.CultureInvariant | (exact ? RegexOptions.None : RegexOptions.IgnoreCase);
        var pattern = exact ? $"{ExactBefore}{body}{ExactAfter}" : $"{Before}{body}{After}";
        return new AliasPattern(skill, new Regex(pattern, options, MatchTimeout));
    }

    private sealed record AliasPattern(Skill Skill, Regex Regex);
}
