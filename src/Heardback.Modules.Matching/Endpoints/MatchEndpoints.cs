using Heardback.Modules.Matching.MatchResults;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Matching.Endpoints;

internal static class MatchEndpoints
{
    // Under the application it describes, so the client finds it next to the application itself.
    public const string Path = "/api/applications/{id:guid}/match";

    public static void MapMatchEndpoints(this IEndpointRouteBuilder routes)
    {
        // Signed-in users only (fallback policy); only the caller's own applications and CV are read.
        routes.MapGet(Path, GetAsync)
            .WithTags("Matching")
            .WithSummary("Matched and missing skills between an application's posting and the user's CV.")
            .Produces<MatchResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAsync(Guid id, GetMatchHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
