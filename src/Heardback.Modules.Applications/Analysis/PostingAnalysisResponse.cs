namespace Heardback.Modules.Applications.Analysis;

/// <param name="State">Where the analysis stands.</param>
/// <param name="Analysis">The fields, when <paramref name="State"/> is <see cref="PostingAnalysisState.Ready"/>.</param>
internal sealed record PostingAnalysisResponse(PostingAnalysisState State, PostingAnalysisFields? Analysis);
