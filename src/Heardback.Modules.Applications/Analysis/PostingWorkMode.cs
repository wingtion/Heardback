using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>Where the work happens, as a posting states it; stored by name.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<PostingWorkMode>))]
internal enum PostingWorkMode
{
    Unknown,
    Onsite,
    Hybrid,
    Remote,
}
