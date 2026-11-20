using Heardback.Modules.Applications.Contracts;

namespace Heardback.Modules.Applications.Domain;

/// <summary>Maps the internal status to the one other modules see.</summary>
internal static class ApplicationStages
{
    public static ApplicationStage From(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => ApplicationStage.Applied,
        ApplicationStatus.Interview => ApplicationStage.Interview,
        ApplicationStatus.Offer => ApplicationStage.Offer,
        ApplicationStatus.Rejected => ApplicationStage.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status."),
    };
}
