using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>Where an application's posting analysis stands, for the detail panel.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<PostingAnalysisState>))]
internal enum PostingAnalysisState
{
    /// <summary>AI features are off (no provider configured).</summary>
    Unavailable,

    /// <summary>The application has no posting text to analyse.</summary>
    NoPostingText,

    /// <summary>Nobody asked for an analysis of this text yet.</summary>
    NotRequested,

    /// <summary>Waiting in the queue (also while waiting to retry).</summary>
    Queued,

    /// <summary>A worker is analysing it now.</summary>
    Running,

    /// <summary>Queued, but the app-wide daily AI limit is used up; it runs after midnight UTC.</summary>
    WaitingForCapacity,

    /// <summary>The analysis is available.</summary>
    Ready,

    /// <summary>The last try failed; it can be requested again.</summary>
    Failed,
}
