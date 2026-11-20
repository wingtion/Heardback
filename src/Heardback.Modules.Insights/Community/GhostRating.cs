namespace Heardback.Modules.Insights.Community;

/// <summary>A ghost-listing hint without its reasons, for a badge.</summary>
/// <param name="Level">Low, Medium or High.</param>
/// <param name="Score">Points out of <see cref="GhostSignalCalculator.MaxScore"/>.</param>
internal sealed record GhostRating(GhostLevel Level, int Score);
