namespace Heardback.Modules.Insights.Community;

/// <summary>One ghost-listing point and the plain sentence that explains it.</summary>
/// <param name="Kind">Which rule gave it.</param>
/// <param name="Points">Points it adds.</param>
/// <param name="Text">The explanation shown to the user.</param>
internal sealed record GhostReason(GhostReasonKind Kind, int Points, string Text);
