using Heardback.Modules.Applications.Domain;
using Heardback.Shared.Results;

namespace Heardback.Modules.Applications;

/// <summary>Expected failures; messages match the copy in docs/design.md §14.</summary>
internal static class ApplicationsErrors
{
    private const string AppliedOnField = "appliedOn";
    private const string OccurredOnField = "occurredOn";
    private const string DateTooEarlyMessage = "Pick a date after 1 January 2000";

    public static readonly Error NotFound =
        Error.NotFound("applications.not_found", "This application doesn't exist or was deleted.");

    public static readonly Error AlreadyTracked =
        Error.Conflict("applications.already_tracked", "You already track this posting.");

    public static readonly Error ChangedElsewhere =
        Error.Conflict("applications.changed_elsewhere", "This application changed since you opened it.");

    public static readonly Error AppliedOnInFuture =
        Error.Validation("applications.date_in_future", AppliedOnField, "Pick today or an earlier date");

    public static readonly Error EventDateInFuture =
        Error.Validation(
            "applications.date_in_future", OccurredOnField, "The date can't be in the future. Pick today or earlier.");

    public static readonly Error AppliedOnTooEarly =
        Error.Validation("applications.date_too_early", AppliedOnField, DateTooEarlyMessage);

    public static readonly Error EmptyUpdate =
        Error.Validation("applications.empty_update", "status", "Choose a new status or write a note.");

    public static readonly Error SameStatus =
        Error.Validation("applications.same_status", "status", "The application already has this status.");

    // The timeline must stay in date order: the submission comes first, so moving it past a later status
    // event would silently change the status (decided 2026-11-18).
    public static Error AppliedAfterFirstUpdate(DateOnly firstUpdate) =>
        Error.Validation(
            "applications.applied_after_first_update",
            AppliedOnField,
            $"Pick a date on or before your first update ({ApplicationDates.Display(firstUpdate)})");

    public static Error UpdateBeforeApplied(DateOnly appliedOn) =>
        Error.Validation(
            "applications.update_before_applied",
            OccurredOnField,
            $"Pick a date on or after the applied date ({ApplicationDates.Display(appliedOn)})");
}
