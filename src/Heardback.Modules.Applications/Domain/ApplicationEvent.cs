using Heardback.Shared.Persistence;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// One entry in an application's timeline: a status change, a note, or both. The first event of every
/// application is its submission (<see cref="IsSubmission"/>), dated on the applied date.
/// </summary>
internal sealed class ApplicationEvent : IUserOwned
{
    public const int NoteMaxLength = 280;

    private ApplicationEvent()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ApplicationId { get; private set; }

    /// <summary>The new status, or <see langword="null"/> for a note-only update.</summary>
    public ApplicationStatus? Status { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public string? Note { get; private set; }

    /// <summary>True for the event created with the application; it follows the applied date.</summary>
    public bool IsSubmission { get; private set; }

    /// <summary>When it was entered; orders events that share a date.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public static ApplicationEvent Submission(Guid applicationId, DateOnly appliedOn, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            ApplicationId = applicationId,
            Status = ApplicationStatus.Applied,
            OccurredOn = appliedOn,
            IsSubmission = true,
            CreatedAt = now,
        };

    public static ApplicationEvent Update(
        Guid applicationId,
        ApplicationStatus? status,
        DateOnly occurredOn,
        string? note,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            ApplicationId = applicationId,
            Status = status,
            OccurredOn = occurredOn,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = now,
        };

    public void MoveSubmissionTo(DateOnly appliedOn)
    {
        if (IsSubmission)
        {
            OccurredOn = appliedOn;
        }
    }
}
