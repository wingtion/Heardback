using Heardback.Shared.Results;

namespace Heardback.Modules.Applications;

/// <summary>Expected failures; messages match the copy in docs/design.md §14.</summary>
internal static class ApplicationsErrors
{
    public static readonly Error NotFound =
        Error.NotFound("applications.not_found", "This application doesn't exist or was deleted.");

    public static readonly Error AlreadyTracked =
        Error.Conflict("applications.already_tracked", "You already track this posting.");

    public static readonly Error ChangedElsewhere =
        Error.Conflict("applications.changed_elsewhere", "This application changed since you opened it.");

    public static readonly Error AppliedOnInFuture =
        Error.Validation("applications.date_in_future", "appliedOn", "Pick today or an earlier date");

    public static readonly Error EventDateInFuture =
        Error.Validation(
            "applications.date_in_future", "occurredOn", "The date can't be in the future. Pick today or earlier.");

    public static readonly Error EmptyUpdate =
        Error.Validation("applications.empty_update", "status", "Choose a new status or write a note.");

    public static readonly Error SameStatus =
        Error.Validation("applications.same_status", "status", "The application already has this status.");
}
