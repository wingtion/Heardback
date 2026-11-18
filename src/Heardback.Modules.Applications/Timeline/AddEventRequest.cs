using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Timeline;

/// <summary>
/// "Add update" and board moves: a new status, a note, or both. A board move sends only the status, dated on
/// the user's today.
/// </summary>
internal sealed record AddEventRequest(
    ApplicationStatus? Status,
    [property: Required] DateOnly? OccurredOn,
    [property: MaxLength(ApplicationEvent.NoteMaxLength)] string? Note);
