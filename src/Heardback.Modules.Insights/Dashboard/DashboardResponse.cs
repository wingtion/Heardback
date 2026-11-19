namespace Heardback.Modules.Insights.Dashboard;

/// <summary>The user's personal numbers (definitions in ADR-0011).</summary>
/// <param name="TotalApplications">All of the user's applications.</param>
/// <param name="Responded">Applications that got a response.</param>
/// <param name="ResponseRatePercent">Responded / all as a whole percent; null without applications.</param>
/// <param name="Weeks">Applications per week, the last 12 weeks, oldest first.</param>
/// <param name="Channels">Response rate per channel, most applications first.</param>
/// <param name="ResponseTime">Days to the first response.</param>
/// <param name="Funnel">How many applications reached each stage.</param>
internal sealed record DashboardResponse(
    int TotalApplications,
    int Responded,
    int? ResponseRatePercent,
    IReadOnlyList<WeeklyCount> Weeks,
    IReadOnlyList<ChannelResponse> Channels,
    ResponseTime ResponseTime,
    Funnel Funnel);
