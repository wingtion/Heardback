using Heardback.Shared.Results;

namespace Heardback.Modules.Insights.Community;

internal static class CommunityErrors
{
    // Same answer for another user's application as for a missing one, like every application route.
    public static readonly Error ApplicationNotFound =
        Error.NotFound("community.application_not_found", "This application doesn't exist or was deleted.");

    // Same answer whether nobody tracks the company or only other members do, so it cannot be used to probe.
    public static readonly Error CompanyNotFound =
        Error.NotFound("community.company_not_found", "You have no applications at this company.");
}
