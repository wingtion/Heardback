using Heardback.Modules.Identity.Authentication;
using Heardback.Modules.Identity.Demo;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Identity.Endpoints;

internal static class DemoEndpoints
{
    public const string Path = "/api/demo";

    public static void MapDemoEndpoints(this IEndpointRouteBuilder routes)
    {
        // Anonymous on purpose: this is how visitors without an invite see the app (ADR-0018). Limited per IP, and
        // the handler caps how many demo visitors exist at once.
        routes.MapPost(Path, StartAsync)
            .WithTags("Demo")
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimiting.DemoPolicy)
            .WithSummary("Start a temporary demo account with sample data and sign in to it.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> StartAsync(
        StartDemoHandler handler,
        HttpRequest request,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(RefreshTokenCookie.Read(request), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        RefreshTokenCookie.Append(response, result.Value.RefreshToken);
        return TypedResults.Ok(AuthResponse.From(result.Value));
    }
}
