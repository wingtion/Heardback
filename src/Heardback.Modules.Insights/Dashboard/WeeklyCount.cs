namespace Heardback.Modules.Insights.Dashboard;

/// <param name="WeekStart">The Monday the week starts on.</param>
/// <param name="Applications">Applications sent that week, by applied date.</param>
internal sealed record WeeklyCount(DateOnly WeekStart, int Applications);
