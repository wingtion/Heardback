using Heardback.Modules.Applications.Domain;

namespace Heardback.UnitTests.Applications;

public sealed class ApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly Guid PostingId = Guid.NewGuid();

    [Fact]
    public void Create_ValidDetails_StartsAppliedWithASubmissionEvent()
    {
        var application = Create(Today.AddDays(-3));

        Assert.Equal(ApplicationStatus.Applied, application.Status);
        var submission = Assert.Single(application.Events);
        Assert.True(submission.IsSubmission);
        Assert.Equal(ApplicationStatus.Applied, submission.Status);
        Assert.Equal(Today.AddDays(-3), submission.OccurredOn);
        Assert.Equal(Today.AddDays(-3), application.LastActivityOn);
    }

    [Fact]
    public void Create_TrimsTextAndDropsBlankOptionalFields()
    {
        var details = new ApplicationDetails("  Northwind  ", " Backend ", "   ", "  ", ApplicationChannel.Referral, Today);

        var application = Application.Create(details, PostingId, Now).Value;

        Assert.Equal("Northwind", application.Company);
        Assert.Equal("Backend", application.Role);
        Assert.Null(application.Url);
        Assert.Null(application.PostingText);
    }

    [Fact]
    public void Create_TomorrowInUtc_IsAllowedForTimeZonesAheadOfUtc()
    {
        var result = Application.Create(Details(Today.AddDays(1)), PostingId, Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_TwoDaysAhead_ReturnsDateInFutureError()
    {
        var result = Application.Create(Details(Today.AddDays(2)), PostingId, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("applications.date_in_future", result.Error.Code);
        Assert.True(result.Error.FieldErrors.ContainsKey("appliedOn"));
    }

    [Fact]
    public void AddEvent_NewStatus_ChangesStatusAndLastActivity()
    {
        var application = Create(Today.AddDays(-10));

        var result = application.AddEvent(ApplicationStatus.Interview, Today, "Recruiter call", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ApplicationStatus.Interview, application.Status);
        Assert.Equal(Today, application.LastActivityOn);
        Assert.Equal("Recruiter call", result.Value.Note);
    }

    [Fact]
    public void AddEvent_NoteOnly_KeepsStatusAndMovesLastActivity()
    {
        var application = Create(Today.AddDays(-10));

        var result = application.AddEvent(null, Today, "Followed up by email", Now);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Status);
        Assert.Equal(ApplicationStatus.Applied, application.Status);
        Assert.Equal(Today, application.LastActivityOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void AddEvent_NoStatusAndNoNote_ReturnsEmptyUpdateError(string? note)
    {
        var application = Create(Today);

        var result = application.AddEvent(null, Today, note, Now);

        Assert.Equal("applications.empty_update", result.Error!.Code);
        Assert.Single(application.Events);
    }

    [Fact]
    public void AddEvent_CurrentStatus_ReturnsSameStatusError()
    {
        var application = Create(Today);

        var result = application.AddEvent(ApplicationStatus.Applied, Today, null, Now);

        Assert.Equal("applications.same_status", result.Error!.Code);
    }

    [Fact]
    public void AddEvent_DateTwoDaysAhead_ReturnsDateInFutureErrorOnOccurredOn()
    {
        var application = Create(Today);

        var result = application.AddEvent(ApplicationStatus.Interview, Today.AddDays(2), null, Now);

        Assert.Equal("applications.date_in_future", result.Error!.Code);
        Assert.True(result.Error.FieldErrors.ContainsKey("occurredOn"));
    }

    [Fact]
    public void AddEvent_BackDatedStatus_DoesNotOverrideALaterStatus()
    {
        var application = Create(Today.AddDays(-20));
        application.AddEvent(ApplicationStatus.Interview, Today.AddDays(-1), null, Now);

        application.AddEvent(ApplicationStatus.Offer, Today.AddDays(-7), null, Now.AddMinutes(1));

        Assert.Equal(ApplicationStatus.Interview, application.Status);
    }

    [Fact]
    public void AddEvent_TwoStatusesOnTheSameDate_LastEnteredWins()
    {
        var application = Create(Today.AddDays(-20));
        application.AddEvent(ApplicationStatus.Interview, Today, null, Now);

        application.AddEvent(ApplicationStatus.Rejected, Today, null, Now.AddMinutes(1));

        Assert.Equal(ApplicationStatus.Rejected, application.Status);
    }

    [Fact]
    public void Edit_NewAppliedDate_MovesTheSubmissionEvent()
    {
        var application = Create(Today.AddDays(-3));

        var result = application.Edit(Details(Today.AddDays(-5)), "Ask about on-call", PostingId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(Today.AddDays(-5), Assert.Single(application.Events).OccurredOn);
        Assert.Equal(Today.AddDays(-5), application.LastActivityOn);
        Assert.Equal("Ask about on-call", application.Notes);
    }

    [Fact]
    public void Edit_AppliedDateTwoDaysAhead_ReturnsErrorAndChangesNothing()
    {
        var application = Create(Today.AddDays(-3));

        var result = application.Edit(Details(Today.AddDays(2)) with { Company = "Changed" }, null, PostingId, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("Northwind Logistics", application.Company);
    }

    [Fact]
    public void Edit_AppliedDateAfterALaterStatusUpdate_ReturnsErrorAndKeepsTheStatus()
    {
        var application = Create(new DateOnly(2026, 11, 8));
        application.AddEvent(ApplicationStatus.Interview, new DateOnly(2026, 11, 12), null, Now);

        var result = application.Edit(Details(new DateOnly(2026, 11, 16)), null, PostingId, Now);

        Assert.Equal("applications.applied_after_first_update", result.Error!.Code);
        Assert.Equal(["Pick a date on or before your first update (12 Nov 2026)"], result.Error.FieldErrors["appliedOn"]);
        Assert.Equal(ApplicationStatus.Interview, application.Status);
        Assert.Equal(new DateOnly(2026, 11, 8), application.AppliedOn);
    }

    [Fact]
    public void Edit_AppliedDateOnTheFirstUpdateDate_IsAllowed()
    {
        var application = Create(Today.AddDays(-10));
        application.AddEvent(null, Today.AddDays(-4), "Followed up", Now);

        var result = application.Edit(Details(Today.AddDays(-4)), null, PostingId, Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddEvent_DateBeforeTheAppliedDate_ReturnsErrorOnOccurredOn()
    {
        var application = Create(new DateOnly(2026, 11, 12));

        var result = application.AddEvent(ApplicationStatus.Interview, new DateOnly(2026, 11, 10), null, Now);

        Assert.Equal("applications.update_before_applied", result.Error!.Code);
        Assert.Equal(["Pick a date on or after the applied date (12 Nov 2026)"], result.Error.FieldErrors["occurredOn"]);
        Assert.Equal(ApplicationStatus.Applied, application.Status);
    }

    [Fact]
    public void Create_AppliedBefore2000_ReturnsDateTooEarlyError()
    {
        var result = Application.Create(Details(new DateOnly(1999, 12, 31)), PostingId, Now);

        Assert.Equal("applications.date_too_early", result.Error!.Code);
        Assert.Equal(["Pick a date after 1 January 2000"], result.Error.FieldErrors["appliedOn"]);
    }

    [Fact]
    public void Create_AppliedOnFirstJanuary2000_IsAllowed()
    {
        Assert.True(Application.Create(Details(new DateOnly(2000, 1, 1)), PostingId, Now).IsSuccess);
    }

    [Theory]
    [InlineData(nameof(ApplicationStatus.Applied), 14, 14)]
    [InlineData(nameof(ApplicationStatus.Applied), 20, 20)]
    [InlineData(nameof(ApplicationStatus.Applied), 13, null)]
    [InlineData(nameof(ApplicationStatus.Interview), 30, null)]
    public void DaysWithoutResponse_StatusAndAge_ReturnDaysOnlyForOldAppliedOnes(string statusName, int daysAgo, int? expected)
    {
        var status = Enum.Parse<ApplicationStatus>(statusName);

        var days = NoResponseRule.DaysWithoutResponse(status, Today.AddDays(-daysAgo), Today, thresholdDays: 14);

        Assert.Equal(expected, days);
    }

    [Theory]
    [InlineData(nameof(ApplicationStatus.Applied), 14, true)]
    [InlineData(nameof(ApplicationStatus.Applied), 13, false)]
    [InlineData(nameof(ApplicationStatus.Offer), 30, false)]
    public void Matches_SameApplication_AgreesWithDaysWithoutResponse(string statusName, int daysAgo, bool expected)
    {
        var status = Enum.Parse<ApplicationStatus>(statusName);
        var application = Create(Today.AddDays(-daysAgo));
        if (status != ApplicationStatus.Applied)
        {
            application.AddEvent(status, Today.AddDays(-daysAgo), null, Now);
        }

        var matches = NoResponseRule.Matches(Today, thresholdDays: 14).Compile()(application);

        Assert.Equal(expected, matches);
        Assert.Equal(expected, NoResponseRule.DaysWithoutResponse(
            application.Status, application.LastActivityOn, Today, thresholdDays: 14) is not null);
    }

    private static Application Create(DateOnly appliedOn) => Application.Create(Details(appliedOn), PostingId, Now).Value;

    private static ApplicationDetails Details(DateOnly appliedOn) =>
        new("Northwind Logistics", "Backend Engineer (.NET)", null, null, ApplicationChannel.LinkedInEasyApply, appliedOn);
}
