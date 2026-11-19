using Heardback.Infrastructure.Llm.Quotas;

namespace Heardback.Modules.Applications.Analysis;

/// <param name="State">Where the analysis stands.</param>
/// <param name="Analysis">The fields, when <paramref name="State"/> is <see cref="PostingAnalysisState.Ready"/>.</param>
/// <param name="Usage">The user's posting analyses today, for "N of 30 left today".</param>
internal sealed record PostingAnalysisResponse(
    PostingAnalysisState State,
    PostingAnalysisFields? Analysis,
    LlmUsage Usage);
