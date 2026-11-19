namespace Heardback.Modules.Insights.Dashboard;

/// <param name="Responded">How many responses the times are based on.</param>
/// <param name="AverageDays">Average days to the first response, one decimal; null without responses.</param>
/// <param name="MedianDays">Median days to the first response; null without responses.</param>
internal sealed record ResponseTime(int Responded, double? AverageDays, double? MedianDays);
