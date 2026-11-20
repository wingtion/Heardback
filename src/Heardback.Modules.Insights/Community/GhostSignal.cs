namespace Heardback.Modules.Insights.Community;

/// <summary>The ghost-listing hint for one application's posting (ADR-0013).</summary>
/// <param name="Score">Points, 0 to <see cref="GhostSignalCalculator.MaxScore"/>.</param>
/// <param name="MaxScore">The highest possible score, for "4 of 5".</param>
/// <param name="Level">Low, Medium or High.</param>
/// <param name="Reasons">Every point given, largest first; empty when nothing stood out.</param>
internal sealed record GhostSignal(int Score, int MaxScore, GhostLevel Level, IReadOnlyList<GhostReason> Reasons);
