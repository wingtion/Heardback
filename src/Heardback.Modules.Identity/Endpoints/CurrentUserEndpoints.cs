using Heardback.Modules.Identity.CurrentUser;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Identity.Endpoints;

internal static class CurrentUserEndpoints
{
    public static void MapCurrentUserEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/me", GetAsync)
            .WithTags("Auth")
            .WithSummary("The signed-in user's profile.")
            .Produces<CurrentUserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetAsync(GetCurrentUserHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
