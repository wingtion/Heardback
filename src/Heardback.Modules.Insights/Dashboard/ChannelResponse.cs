namespace Heardback.Modules.Insights.Dashboard;

/// <param name="Channel">The channel by name, such as <c>LinkedInEasyApply</c>.</param>
/// <param name="Applications">Applications through it.</param>
/// <param name="Responded">Of those, how many got a response.</param>
/// <param name="ResponseRatePercent">Responded / applications as a whole percent.</param>
internal sealed record ChannelResponse(string Channel, int Applications, int Responded, int ResponseRatePercent);
