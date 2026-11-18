using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Responses;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Read;

internal static class ApplicationQueries
{
    private const string LikeEscape = "\\";
    private const string LikeAnyText = "%";
    private const string LikeAnyCharacter = "_";

    /// <summary>Applies search (company or role, case-insensitive), status and channel.</summary>
    public static IQueryable<Application> Filter(
        this IQueryable<Application> applications,
        ApplicationFilter filter,
        NoResponseClock noResponse)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikeAnyText + EscapeLike(filter.Search.Trim()) + LikeAnyText;
            applications = applications.Where(application =>
                EF.Functions.ILike(application.Company, pattern, LikeEscape)
                || EF.Functions.ILike(application.Role, pattern, LikeEscape));
        }

        if (filter.Channel is { } channel)
        {
            applications = applications.Where(application => application.Channel == channel);
        }

        return filter.Status switch
        {
            null => applications,
            ApplicationListStatus.NoResponse => applications.Where(noResponse.Matches()),
            ApplicationListStatus.Applied => applications.Where(noResponse.MatchesRecentlyApplied()),
            var status => applications.Where(application => application.Status == ToStatus(status.Value)),
        };
    }

    public static IQueryable<ApplicationListItem> ToListItems(
        this IQueryable<Application> applications,
        NoResponseClock noResponse) =>
        applications
            .OrderByDescending(application => application.UpdatedAt)
            .ThenByDescending(application => application.Id)
            .Select(application => new ApplicationListItem(
                application.Id,
                application.Company,
                application.Role,
                application.Channel,
                application.AppliedOn,
                application.Status,
                noResponse.DaysFor(application.Status, application.LastActivityOn),
                application.UpdatedAt));

    private static ApplicationStatus ToStatus(ApplicationListStatus status) => status switch
    {
        ApplicationListStatus.Interview => ApplicationStatus.Interview,
        ApplicationListStatus.Offer => ApplicationStatus.Offer,
        ApplicationListStatus.Rejected => ApplicationStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Handled before mapping."),
    };

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace(LikeAnyText, LikeEscape + LikeAnyText, StringComparison.Ordinal)
            .Replace(LikeAnyCharacter, LikeEscape + LikeAnyCharacter, StringComparison.Ordinal);
}
