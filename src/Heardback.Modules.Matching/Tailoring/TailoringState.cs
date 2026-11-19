using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>Where an application's tailoring suggestions stand, for the match panel.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TailoringState>))]
internal enum TailoringState
{
    /// <summary>AI features are off (no provider configured).</summary>
    Unavailable,

    /// <summary>The application has no posting text.</summary>
    NoPostingText,

    /// <summary>The user has not uploaded a CV.</summary>
    NoCv,

    /// <summary>The user has not allowed sending their CV text to the AI provider.</summary>
    ConsentRequired,

    /// <summary>Ready to be asked for.</summary>
    NotRequested,

    /// <summary>
    /// Waiting in the queue (also while waiting to retry). Out-of-date suggestions being made again are still
    /// returned in this state and the next two.
    /// </summary>
    Queued,

    /// <summary>A worker is writing them now.</summary>
    Running,

    /// <summary>Queued, but the app-wide daily AI limit is used up; it runs after midnight UTC.</summary>
    WaitingForCapacity,

    /// <summary>The suggestions are available (see <see cref="TailoringDetails.IsOutdated"/>).</summary>
    Ready,

    /// <summary>The last try failed; it can be asked for again.</summary>
    Failed,
}
