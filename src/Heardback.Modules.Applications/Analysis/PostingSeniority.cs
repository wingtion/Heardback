using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>Seniority as a posting states it; stored by name.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<PostingSeniority>))]
internal enum PostingSeniority
{
    Unknown,
    Intern,
    Junior,
    Mid,
    Senior,
    Lead,
}
