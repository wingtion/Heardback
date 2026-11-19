using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Insights.Dashboard;

namespace Heardback.UnitTests.Insights;

public sealed class DashboardCalculatorTests
{
    // A Thursday.
    private static readonly DateOnly Today = new(2026, 11, 19);

    [Fact]
    public void Calculate_NoApplications_IsEmptyButComplete()
    {
        var dashboard = DashboardCalculator.Calculate([], Today);

        Assert.Equal(0, dashboard.TotalApplications);
        Assert.Null(dashboard.ResponseRatePercent);
        Assert.Equal(DashboardCalculator.WeeksShown, dashboard.Weeks.Count);
        Assert.All(dashboard.Weeks, week => Assert.Equal(0, week.Applications));
        Assert.Empty(dashboard.Channels);
        Assert.Equal(new ResponseTime(0, null, null), dashboard.ResponseTime);
        Assert.Equal(new Funnel(0, 0, 0, 0), dashboard.Funnel);
    }

    [Theory]
    [InlineData("2026-11-16", "2026-11-16")] // Monday
    [InlineData("2026-11-19", "2026-11-16")] // Thursday
    [InlineData("2026-11-22", "2026-11-16")] // Sunday
    [InlineData("2026-11-23", "2026-11-23")] // next Monday
    public void WeekStart_AnyDay_IsTheMondayOnOrBefore(string date, string monday)
    {
        Assert.Equal(DateOnly.Parse(monday, System.Globalization.CultureInfo.InvariantCulture), DashboardCalculator.WeekStart(DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Calculate_Weeks_CountByAppliedDateOverTheLastTwelveWeeks()
    {
        var dashboard = DashboardCalculator.Calculate(
            [
                Application(Today),
                Application(Today.AddDays(-3)), // Monday of this week
                Application(Today.AddDays(-4)), // Sunday of last week
                Application(Today.AddDays(-7 * 11)),
                Application(Today.AddDays(-7 * 12)), // too old to show
            ],
            Today);

        Assert.Equal(new DateOnly(2026, 8, 31), dashboard.Weeks[0].WeekStart);
        Assert.Equal(new DateOnly(2026, 11, 16), dashboard.Weeks[^1].WeekStart);
        Assert.Equal(2, dashboard.Weeks[^1].Applications);
        Assert.Equal(1, dashboard.Weeks[^2].Applications);
        Assert.Equal(1, dashboard.Weeks[0].Applications);
        Assert.Equal(5, dashboard.TotalApplications);
    }

    [Fact]
    public void Calculate_Response_IsTheFirstStatusChangeIncludingRejectionButNotANote()
    {
        var dashboard = DashboardCalculator.Calculate(
            [
                Application(Today.AddDays(-10), changes: [Change(ApplicationStage.Rejected, -2)]),
                Application(Today.AddDays(-10), changes: [Change(ApplicationStage.Interview, -5)]),
                Application(Today.AddDays(-10)),
                Application(Today.AddDays(-10), changes: [Change(ApplicationStage.Applied, -6)]),
            ],
            Today);

        Assert.Equal(2, dashboard.Responded);
        Assert.Equal(50, dashboard.ResponseRatePercent);
    }

    [Fact]
    public void Calculate_Channels_RatePerChannelMostApplicationsFirst()
    {
        var dashboard = DashboardCalculator.Calculate(
            [
                Application(Today, "LinkedInEasyApply"),
                Application(Today, "LinkedInEasyApply", [Change(ApplicationStage.Rejected, 0)]),
                Application(Today, "LinkedInEasyApply"),
                Application(Today, "Referral", [Change(ApplicationStage.Interview, 0)]),
            ],
            Today);

        Assert.Equal(
            [new ChannelResponse("LinkedInEasyApply", 3, 1, 33), new ChannelResponse("Referral", 1, 1, 100)],
            dashboard.Channels);
    }

    [Fact]
    public void Calculate_ResponseTime_AverageAndMedianOverRespondedOnly()
    {
        var applied = Today.AddDays(-30);
        var dashboard = DashboardCalculator.Calculate(
            [
                Application(applied, changes: [Change(ApplicationStage.Interview, -27)]), // 3 days
                Application(applied, changes: [Change(ApplicationStage.Rejected, -20)]), // 10 days
                Application(applied, changes: [Change(ApplicationStage.Offer, -26), Change(ApplicationStage.Rejected, -1)]), // 4 days
                Application(applied, changes: [Change(ApplicationStage.Interview, -18)]), // 12 days
                Application(applied), // no response: not counted
            ],
            Today);

        Assert.Equal(new ResponseTime(4, 7.3, 7.0), dashboard.ResponseTime);
    }

    [Fact]
    public void Calculate_ResponseTime_SameDayAndOddCount()
    {
        var dashboard = DashboardCalculator.Calculate(
            [Application(Today, changes: [Change(ApplicationStage.Interview, 0)])],
            Today);

        Assert.Equal(new ResponseTime(1, 0, 0), dashboard.ResponseTime);
    }

    [Fact]
    public void Calculate_ResponseBeforeTheAppliedDate_CountsAsSameDay()
    {
        var dashboard = DashboardCalculator.Calculate(
            [Application(Today, changes: [Change(ApplicationStage.Interview, -3)])],
            Today);

        Assert.Equal(0, dashboard.ResponseTime.MedianDays);
    }

    [Fact]
    public void Calculate_Funnel_CountsTheHighestStageEverReached()
    {
        var dashboard = DashboardCalculator.Calculate(
            [
                Application(Today, changes: [Change(ApplicationStage.Interview, 0), Change(ApplicationStage.Rejected, 0)]),
                Application(Today, changes: [Change(ApplicationStage.Offer, 0)]),
                Application(Today, changes: [Change(ApplicationStage.Rejected, 0)]),
                Application(Today),
            ],
            Today);

        Assert.Equal(new Funnel(Applied: 4, Interview: 2, Offer: 1, Rejected: 2), dashboard.Funnel);
    }

    private static ApplicationHistory Application(
        DateOnly appliedOn,
        string channel = "CompanySite",
        IReadOnlyList<ApplicationStatusChange>? changes = null) =>
        new(Guid.NewGuid(), "Northwind", "Backend Engineer", channel, appliedOn, appliedOn, null, changes ?? []);

    private static ApplicationStatusChange Change(ApplicationStage stage, int daysFromToday) =>
        new(stage, Today.AddDays(daysFromToday));
}
