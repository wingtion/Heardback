using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Insights.Community;

namespace Heardback.UnitTests.Insights;

public sealed class GhostSignalCalculatorTests
{
    private const string RoleKey = "backend engineer";
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly CommunitySettings Settings = new();

    private static readonly TrackedApplication Application = new(
        Guid.NewGuid(), Guid.NewGuid(), "Northwind", "northwind", "Backend Engineer", RoleKey,
        ApplicationStage.Applied, new DateOnly(2026, 10, 1), NoResponseDays: 50);

    [Fact]
    public void Calculate_NoGroupPassedTheMinimum_IsNull()
    {
        var signal = GhostSignalCalculator.Calculate(Application, posting: null, company: null, Now, Settings);

        Assert.Null(signal);
    }

    [Fact]
    public void Calculate_HealthyPostingAndCompany_IsLowWithoutReasons()
    {
        var posting = Posting(Outcome(members: 4, responded: 3, averageDays: 5), listedDaysAgo: 10);
        var company = Company(Outcome(members: 5, responded: 4, averageDays: 6), rolePostings: 1);

        var signal = GhostSignalCalculator.Calculate(Application, posting, company, Now, Settings);

        Assert.NotNull(signal);
        Assert.Equal(0, signal.Score);
        Assert.Equal(GhostLevel.Low, signal.Level);
        Assert.Empty(signal.Reasons);
    }

    [Fact]
    public void Calculate_EveryRuleFires_ScoreIsCappedAtFive()
    {
        var posting = Posting(Outcome(members: 3, responded: 0, averageDays: null), listedDaysAgo: 45);
        var company = Company(Outcome(members: 6, responded: 1, averageDays: 30, applications: 8), rolePostings: 3);

        var signal = GhostSignalCalculator.Calculate(Application, posting, company, Now, Settings);

        Assert.Equal(GhostSignalCalculator.MaxScore, signal!.Score);
        Assert.Equal(GhostLevel.High, signal.Level);
        Assert.Equal(
            [
                GhostReasonKind.NoPostingResponse, GhostReasonKind.LongListed, GhostReasonKind.LowCompanyResponse,
                GhostReasonKind.SlowCompanyResponse, GhostReasonKind.RoleReposted,
            ],
            signal.Reasons.Select(reason => reason.Kind));
    }

    [Fact]
    public void Calculate_NobodyHeardBackOnPosting_GivesTwoPointsAndSaysWhy()
    {
        var posting = Posting(Outcome(members: 3, responded: 0, averageDays: null), listedDaysAgo: 5);

        var signal = GhostSignalCalculator.Calculate(Application, posting, company: null, Now, Settings);

        var reason = Assert.Single(signal!.Reasons);
        Assert.Equal(GhostReasonKind.NoPostingResponse, reason.Kind);
        Assert.Equal(2, reason.Points);
        Assert.Equal("None of the 3 other members who applied heard back.", reason.Text);
        Assert.Equal(GhostLevel.Medium, signal.Level);
    }

    [Fact]
    public void Calculate_FewHeardBackOnPosting_GivesOnePoint()
    {
        var posting = Posting(Outcome(members: 5, responded: 1, averageDays: 4), listedDaysAgo: 5);

        var signal = GhostSignalCalculator.Calculate(Application, posting, company: null, Now, Settings);

        var reason = Assert.Single(signal!.Reasons);
        Assert.Equal(GhostReasonKind.LowPostingResponse, reason.Kind);
        Assert.Equal("Only 1 of the 5 other members who applied heard back.", reason.Text);
    }

    [Fact]
    public void Calculate_ExactlyAtLowResponseThreshold_IsNotLow()
    {
        var company = Company(Outcome(members: 4, responded: 1, averageDays: 3), rolePostings: 1);

        var signal = GhostSignalCalculator.Calculate(Application, posting: null, company, Now, Settings);

        Assert.Empty(signal!.Reasons);
    }

    [Fact]
    public void Calculate_LongListedPosting_SaysHowLong()
    {
        var posting = Posting(Outcome(members: 3, responded: 3, averageDays: 2), listedDaysAgo: 30);

        var signal = GhostSignalCalculator.Calculate(Application, posting, company: null, Now, Settings);

        var reason = Assert.Single(signal!.Reasons);
        Assert.Equal(GhostReasonKind.LongListed, reason.Kind);
        Assert.Equal("First added here 30 days ago.", reason.Text);
    }

    [Fact]
    public void Calculate_SlowCompany_RoundsTheAverageToOneDecimal()
    {
        var company = Company(Outcome(members: 3, responded: 3, averageDays: 21.25), rolePostings: 1);

        var signal = GhostSignalCalculator.Calculate(Application, posting: null, company, Now, Settings);

        var reason = Assert.Single(signal!.Reasons);
        Assert.Equal(GhostReasonKind.SlowCompanyResponse, reason.Kind);
        Assert.Equal("This company took 21.3 days on average to answer other members.", reason.Text);
    }

    [Fact]
    public void Calculate_RoleWithoutKey_NeverCountsAsReposted()
    {
        var withoutKey = Application with { RoleKey = null };
        var company = Company(Outcome(members: 3, responded: 3, averageDays: 2), rolePostings: 9);

        var signal = GhostSignalCalculator.Calculate(withoutKey, posting: null, company, Now, Settings);

        Assert.Empty(signal!.Reasons);
    }

    [Theory]
    [InlineData(0, nameof(GhostLevel.Low))]
    [InlineData(1, nameof(GhostLevel.Low))]
    [InlineData(2, nameof(GhostLevel.Medium))]
    [InlineData(3, nameof(GhostLevel.Medium))]
    [InlineData(4, nameof(GhostLevel.High))]
    [InlineData(5, nameof(GhostLevel.High))]
    public void LevelFor_Score_GivesLevel(int score, string expected)
    {
        var level = GhostSignalCalculator.LevelFor(score);

        Assert.Equal(expected, level.ToString());
    }

    private static CommunityOutcome Outcome(int members, int responded, double? averageDays, int? applications = null) =>
        new(members, applications ?? members, responded, averageDays, averageDays);

    private static PostingOutcome Posting(CommunityOutcome outcome, int listedDaysAgo) =>
        new(outcome, Now.AddDays(-listedDaysAgo));

    private static CompanyOutcome Company(CommunityOutcome outcome, int rolePostings) =>
        new(outcome, new Dictionary<string, int> { [RoleKey] = rolePostings });
}
