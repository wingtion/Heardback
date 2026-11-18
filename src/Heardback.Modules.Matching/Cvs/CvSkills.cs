using Heardback.Modules.Matching.Skills;

namespace Heardback.Modules.Matching.Cvs;

/// <summary>The skills a CV counts for: what its text names, minus what the user marked as not theirs.</summary>
internal static class CvSkills
{
    /// <summary>Named in the text and not excluded, in dictionary order (the CV page's "Skills found").</summary>
    public static IReadOnlyList<Skill> Found(string text, IReadOnlyCollection<string> excluded) =>
        SkillFinder.Find(text).Where(skill => !excluded.Contains(skill.Name)).ToList();

    /// <summary>
    /// What the CV shows for matching: the found skills and everything they imply, still without the excluded
    /// ones, so excluding ".NET" holds even though C# implies it.
    /// </summary>
    public static IReadOnlySet<Skill> Shown(string text, IReadOnlyCollection<string> excluded) =>
        SkillFinder.WithImplied(Found(text, excluded))
            .Where(skill => !excluded.Contains(skill.Name))
            .ToHashSet();

    /// <summary>The excluded names in dictionary order, for display.</summary>
    public static IReadOnlyList<string> InCatalogOrder(IReadOnlyCollection<string> names) =>
        SkillCatalog.All.Where(skill => names.Contains(skill.Name)).Select(skill => skill.Name).ToList();
}
