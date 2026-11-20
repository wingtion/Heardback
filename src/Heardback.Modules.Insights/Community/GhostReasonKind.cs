using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Insights.Community;

/// <summary>Why a ghost-listing point was given (ADR-0013).</summary>
[JsonConverter(typeof(StrictStringEnumConverter<GhostReasonKind>))]
internal enum GhostReasonKind
{
    /// <summary>The posting was first seen long ago.</summary>
    LongListed,

    /// <summary>No other member heard back on this posting.</summary>
    NoPostingResponse,

    /// <summary>Few other members heard back on this posting.</summary>
    LowPostingResponse,

    /// <summary>Few applications to the company got an answer.</summary>
    LowCompanyResponse,

    /// <summary>The company takes long to answer.</summary>
    SlowCompanyResponse,

    /// <summary>The same role was posted as several separate postings.</summary>
    RoleReposted,
}
