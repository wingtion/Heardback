using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Insights.Community;

/// <summary>How likely a posting is inactive, from the ghost-listing points (ADR-0013). A hint, not a verdict.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<GhostLevel>))]
internal enum GhostLevel
{
    /// <summary>0 or 1 points.</summary>
    Low,

    /// <summary>2 or 3 points.</summary>
    Medium,

    /// <summary>4 or 5 points.</summary>
    High,
}
