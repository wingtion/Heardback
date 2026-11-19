using Heardback.Modules.Insights.Dashboard;
using Heardback.Modules.Insights.FollowUps;
using Heardback.Shared.Paging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Insights.Endpoints;

internal static class InsightsEndpoints
{
    public const string BasePath = "/api/insights";

    public static void MapInsightRoutes(this IEndpointRouteBuilder routes)
    {
        // Signed-in users only (fallback policy); every number comes from the caller's own applications.
        var insights = routes.MapGroup(BasePath)
            .WithTags("Insights")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        insights.MapGet("/dashboard", GetDashboardAsync)
            .WithSummary("Personal numbers: applications per week, response rates, time to first response, funnel.")
            .Produces<DashboardResponse>();

        insights.MapGet("/follow-ups", ListFollowUpsAsync)
            .WithSummary("Applications with no response for the threshold number of days, longest silence first.")
            .Produces<PagedResponse<FollowUpItem>>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetDashboardAsync(GetDashboardHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));

    private static async Task<IResult> ListFollowUpsAsync(
        [AsParameters] PageRequest page,
        ListFollowUpsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(page, cancellationToken));
}
