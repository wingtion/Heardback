using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Matching.Cvs;

/// <summary>The complete list of skills the user says are not theirs; it replaces the earlier list.</summary>
internal sealed record ExcludeSkillsRequest(
    [property: Required, MaxLength(ExcludeSkillsRequest.MaxSkills)] IReadOnlyList<string>? Skills)
{
    /// <summary>More than the dictionary holds, so any honest list fits.</summary>
    public const int MaxSkills = 500;
}
