using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Persistence;
using Heardback.Shared.Results;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// One job a user applied for. It keeps the user's own copy of the posting details and points to the shared
/// posting only to recognise the same job across users (ADR-0004). Status comes from the timeline.
/// </summary>
internal sealed class Application : IUserOwned
{
    public const int NotesMaxLength = 4000;

    private readonly List<ApplicationEvent> _events = [];

    private Application()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>
    /// The shared posting in the Postings module (no foreign key across module schemas). Set by
    /// <see cref="LinkTo"/> before the first save.
    /// </summary>
    public Guid PostingId { get; private set; }

    public string Company { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;

    public string? Url { get; private set; }

    public string? PostingText { get; private set; }

    /// <summary>
    /// Comparison key of <see cref="Url"/> (<see cref="PostingKeys.NormalizeUrl"/>). Unique per user, so "already
    /// tracked" never depends on what other users submitted to the shared catalogue.
    /// </summary>
    public string? NormalizedUrl { get; private set; }

    /// <summary>Fingerprint of company, role and posting text (<see cref="PostingKeys.ContentHash"/>); unique per user.</summary>
    public string? ContentHash { get; private set; }

    public ApplicationChannel Channel { get; private set; }

    public DateOnly AppliedOn { get; private set; }

    /// <summary>Status of the latest status event; kept in step with <see cref="Events"/>.</summary>
    public ApplicationStatus Status { get; private set; }

    /// <summary>Date of the latest event; the "No response" rule counts from here.</summary>
    public DateOnly LastActivityOn { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the user last changed anything, timeline included ("Last update").</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Row version (Postgres <c>xmin</c>); a stale value makes a save fail instead of overwriting.</summary>
    public uint Version { get; private set; }

    public IReadOnlyList<ApplicationEvent> Events => _events;

    public static Result<Application> Create(ApplicationDetails details, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (ValidateAppliedOn(details.AppliedOn, now) is { } invalidDate)
        {
            return invalidDate;
        }

        var application = new Application
        {
            Id = Guid.CreateVersion7(now),
            CreatedAt = now,
        };
        application.ApplyDetails(details, now);
        application._events.Add(ApplicationEvent.Submission(application.Id, details.AppliedOn, now));
        application.RefreshFromTimeline();

        return application;
    }

    /// <summary>Changes the details. Needs <see cref="Events"/> loaded, because the submission event moves with the applied date.</summary>
    public Result Edit(ApplicationDetails details, string? notes, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (ValidateAppliedOn(details.AppliedOn, now) is { } invalidDate)
        {
            return invalidDate;
        }

        var firstUpdate = _events.Where(timelineEvent => !timelineEvent.IsSubmission)
            .Select(timelineEvent => (DateOnly?)timelineEvent.OccurredOn)
            .Min();
        if (details.AppliedOn > firstUpdate)
        {
            return ApplicationsErrors.AppliedAfterFirstUpdate(firstUpdate.Value);
        }

        ApplyDetails(details, now);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        foreach (var timelineEvent in _events)
        {
            timelineEvent.MoveSubmissionTo(details.AppliedOn);
        }

        RefreshFromTimeline();
        return Result.Success();
    }

    /// <summary>Adds a status change, a note, or both. Needs <see cref="Events"/> loaded.</summary>
    public Result<ApplicationEvent> AddEvent(
        ApplicationStatus? status,
        DateOnly occurredOn,
        string? note,
        DateTimeOffset now)
    {
        if (status is null && string.IsNullOrWhiteSpace(note))
        {
            return ApplicationsErrors.EmptyUpdate;
        }

        if (status == Status)
        {
            return ApplicationsErrors.SameStatus;
        }

        if (!ApplicationDates.IsTodayOrEarlier(occurredOn, now))
        {
            return ApplicationsErrors.EventDateInFuture;
        }

        // An update dated before the submission would sit under it in the timeline and could not change the status.
        if (occurredOn < AppliedOn)
        {
            return ApplicationsErrors.UpdateBeforeApplied(AppliedOn);
        }

        var timelineEvent = ApplicationEvent.Update(Id, status, occurredOn, note, now);
        _events.Add(timelineEvent);
        UpdatedAt = now;
        RefreshFromTimeline();

        return timelineEvent;
    }

    private static Error? ValidateAppliedOn(DateOnly appliedOn, DateTimeOffset now) =>
        !ApplicationDates.IsTodayOrEarlier(appliedOn, now) ? ApplicationsErrors.AppliedOnInFuture
        : ApplicationDates.IsTooEarly(appliedOn) ? ApplicationsErrors.AppliedOnTooEarly
        : null;

    /// <summary>Points the application at the shared posting its details match.</summary>
    public void LinkTo(Guid postingId) => PostingId = postingId;

    private void ApplyDetails(ApplicationDetails details, DateTimeOffset now)
    {
        Company = details.Company.Trim();
        Role = details.Role.Trim();
        Url = string.IsNullOrWhiteSpace(details.Url) ? null : details.Url.Trim();
        PostingText = string.IsNullOrWhiteSpace(details.PostingText) ? null : details.PostingText.Trim();
        NormalizedUrl = PostingKeys.NormalizeUrl(Url);
        ContentHash = PostingKeys.ContentHash(Company, Role, PostingText);
        Channel = details.Channel;
        AppliedOn = details.AppliedOn;
        UpdatedAt = now;
    }

    // The latest status event by date decides the status, so a back-dated update never contradicts the
    // timeline the user sees. Events on the same date are ordered by when they were entered.
    private void RefreshFromTimeline()
    {
        var latestStatusEvent = _events
            .Where(timelineEvent => timelineEvent.Status is not null)
            .OrderBy(timelineEvent => timelineEvent.OccurredOn)
            .ThenBy(timelineEvent => timelineEvent.CreatedAt)
            .Last();

        Status = latestStatusEvent.Status!.Value;
        LastActivityOn = _events.Max(timelineEvent => timelineEvent.OccurredOn);
    }
}
