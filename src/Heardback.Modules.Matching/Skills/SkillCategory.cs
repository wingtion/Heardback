using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Matching.Skills;

/// <summary>Groups skills in the match panel, in this order.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<SkillCategory>))]
internal enum SkillCategory
{
    Language,
    Framework,
    Database,
    CloudAndDevOps,
    Testing,
    Practice,
    Tool,
}
