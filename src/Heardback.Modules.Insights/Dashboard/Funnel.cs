namespace Heardback.Modules.Insights.Dashboard;

/// <summary>How many applications ever reached each stage (the highest stage, not today's status).</summary>
/// <param name="Applied">All applications.</param>
/// <param name="Interview">Reached an interview or an offer.</param>
/// <param name="Offer">Reached an offer.</param>
/// <param name="Rejected">Were rejected at some point.</param>
internal sealed record Funnel(int Applied, int Interview, int Offer, int Rejected);
