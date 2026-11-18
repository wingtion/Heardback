namespace Heardback.Modules.Matching.Skills;

/// <summary>
/// One skill in the dictionary. <see cref="Aliases"/> match regardless of case. <see cref="ExactAliases"/> match
/// only as written and with stricter edges, for names that are also ordinary words or letters ("C", "Go",
/// "REST"). <see cref="Implies"/> lists skills that having this one also shows ("ASP.NET Core" shows ".NET").
/// </summary>
internal sealed record Skill(
    string Name,
    SkillCategory Category,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> ExactAliases,
    IReadOnlyList<string> Implies);
