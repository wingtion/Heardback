using Heardback.Modules.Insights.Community;
using Heardback.Shared.Http;
using Heardback.Shared.Paging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Insights.Endpoints;

internal static class CommunityEndpoints
{
    public const string BasePath = "/api/community";

    public static void MapCommunityRoutes(this IEndpointRouteBuilder routes)
    {
        // Signed-in users only (fallback policy). Every route starts from the caller's own applications, so nothing
        // can be asked about a posting or company the caller does not track (ADR-0013).
        var community = routes.MapGroup(BasePath)
            .WithTags("Community")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        community.MapGet("/applications/{id:guid}", GetForApplicationAsync)
            .WithSummary("Other members' numbers and the ghost-listing hint for one of your applications.")
            .Produces<ApplicationCommunityResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        community.MapGet("/badges", ListBadgesAsync)
            .WithSummary("Your applications whose posting has a Medium or High ghost-listing hint.")
            .Produces<PagedResponse<CommunityBadge>>()
            .ProducesValidationProblem();

        community.MapGet("/companies/{key}", GetForCompanyAsync)
            .WithSummary("Other members' numbers for a company you have applied to, with your applications there.")
            .Produces<CompanyCommunityResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetForApplicationAsync(
        Guid id,
        CommunitySignalsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.ForApplicationAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> ListBadgesAsync(
        [AsParameters] PageRequest page,
        CommunitySignalsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.BadgesAsync(page, cancellationToken));

    private static async Task<IResult> GetForCompanyAsync(
        string key,
        CommunitySignalsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.ForCompanyAsync(key, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
