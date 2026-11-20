using Heardback.Modules.Applications.Community;

namespace Heardback.UnitTests.Applications;

public sealed class CommunityGroupingTests
{
    private const int MinimumMembers = 3;
    private static readonly DateOnly AppliedOn = new(2026, 10, 1);
    private static readonly Guid PostingId = Guid.NewGuid();

    [Fact]
    public void Outcome_FewerMembersThanMinimum_IsNull()
    {
        var group = new[] { Application(Guid.NewGuid()), Application(Guid.NewGuid()) };

        var outcome = CommunityGrouping.Outcome(group, MinimumMembers);

        Assert.Null(outcome);
    }

    [Fact]
    public void Outcome_ManyApplicationsFromFewMembers_IsNull()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var group = new[] { Application(first), Application(first), Application(first), Application(second) };

        var outcome = CommunityGrouping.Outcome(group, MinimumMembers);

        Assert.Null(outcome);
    }

    [Fact]
    public void Outcome_EnoughMembers_CountsMembersApplicationsAndResponses()
    {
        var repeat = Guid.NewGuid();
        var group = new[]
        {
            Application(repeat, respondedAfterDays: 4),
            Application(repeat),
            Application(Guid.NewGuid(), respondedAfterDays: 10),
            Application(Guid.NewGuid()),
        };

        var outcome = CommunityGrouping.Outcome(group, MinimumMembers);

        Assert.NotNull(outcome);
        Assert.Equal(3, outcome.Members);
        Assert.Equal(4, outcome.Applications);
        Assert.Equal(2, outcome.Responded);
        Assert.Equal(7.0, outcome.AverageDaysToResponse);
        Assert.Equal(7.0, outcome.MedianDaysToResponse);
    }

    [Fact]
    public void Outcome_OddNumberOfResponses_MedianIsTheMiddleDay()
    {
        var group = new[]
        {
            Application(Guid.NewGuid(), respondedAfterDays: 2),
            Application(Guid.NewGuid(), respondedAfterDays: 3),
            Application(Guid.NewGuid(), respondedAfterDays: 30),
        };

        var outcome = CommunityGrouping.Outcome(group, MinimumMembers);

        Assert.Equal(3.0, outcome!.MedianDaysToResponse);
        Assert.Equal(35 / 3.0, outcome.AverageDaysToResponse);
    }

    [Fact]
    public void Outcome_NobodyResponded_HasNoTimes()
    {
        var group = new[] { Application(Guid.NewGuid()), Application(Guid.NewGuid()), Application(Guid.NewGuid()) };

        var outcome = CommunityGrouping.Outcome(group, MinimumMembers);

        Assert.Equal(0, outcome!.Responded);
        Assert.Null(outcome.AverageDaysToResponse);
        Assert.Null(outcome.MedianDaysToResponse);
    }

    [Fact]
    public void Outcome_ResponseDatedBeforeApplying_CountsAsSameDay()
    {
        var group = new[]
        {
            Application(Guid.NewGuid(), respondedAfterDays: -3),
            Application(Guid.NewGuid(), respondedAfterDays: 0),
            Application(Guid.NewGuid(), respondedAfterDays: 0),
        };

        var outcome = CommunityGrouping.Outcome(group, MinimumMembers);

        Assert.Equal(0.0, outcome!.AverageDaysToResponse);
    }

    private static OtherApplication Application(Guid userId, int? respondedAfterDays = null) =>
        new(userId, PostingId, "northwind", AppliedOn, respondedAfterDays is { } days ? AppliedOn.AddDays(days) : null);
}
