using Heardback.Infrastructure.Llm.Quotas;

namespace Heardback.Modules.Matching.Tailoring;

/// <param name="State">Where the suggestions stand.</param>
/// <param name="Tailoring">The suggestions, when <paramref name="State"/> is <see cref="TailoringState.Ready"/>.</param>
/// <param name="MissingKeywords">
/// Posting skills the CV does not show, from the deterministic match (never from the LLM), so they cannot be made up.
/// </param>
/// <param name="Usage">The user's tailoring requests today, for "N of 10 left today".</param>
internal sealed record TailoringResponse(
    TailoringState State,
    TailoringDetails? Tailoring,
    IReadOnlyList<string> MissingKeywords,
    LlmUsage Usage);
