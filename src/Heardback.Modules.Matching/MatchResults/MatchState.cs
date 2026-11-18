using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Matching.MatchResults;

/// <summary>Whether a match could be worked out, and if not, what is missing.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<MatchState>))]
internal enum MatchState
{
    /// <summary>Matched and missing skills and a score.</summary>
    Ready,

    /// <summary>The user has not uploaded a CV; the posting's skills are all listed as missing.</summary>
    NoCv,

    /// <summary>The application has no posting text, and its role names no known skill.</summary>
    NoPostingText,

    /// <summary>The posting text names no skill from the dictionary.</summary>
    NoSkillsInPosting,
}
