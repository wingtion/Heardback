namespace Heardback.Modules.Insights.Community;

/// <summary>A ghost-risk badge for one of the viewer's applications in the list or board.</summary>
/// <param name="ApplicationId">The viewer's application.</param>
/// <param name="Level">Medium or High; Low gets no badge.</param>
/// <param name="Score">Points out of <see cref="GhostSignalCalculator.MaxScore"/>.</param>
internal sealed record CommunityBadge(Guid ApplicationId, GhostLevel Level, int Score);
