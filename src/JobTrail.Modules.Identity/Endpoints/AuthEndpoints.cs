using JobTrail.Modules.Identity.Login;
using JobTrail.Modules.Identity.Registration;
using JobTrail.Modules.Identity.Sessions;
using JobTrail.Shared.Http;
using JobTrail.Shared.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JobTrail.Modules.Identity.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        auth.MapPost("/register", RegisterAsync)
            .WithSummary("Create an account with an invite code and sign in.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        auth.MapPost("/login", LoginAsync)
            .WithSummary("Sign in with email and password.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        RegisterUserHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return ToSessionResult(result, response, session => TypedResults.Created("/api/me", AuthResponse.From(session)));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        LoginHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return ToSessionResult(result, response, session => TypedResults.Ok(AuthResponse.From(session)));
    }

    private static IResult ToSessionResult(
        Result<IssuedSession> result,
        HttpResponse response,
        Func<IssuedSession, IResult> onSuccess)
    {
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        RefreshTokenCookie.Append(response, result.Value.RefreshToken);
        return onSuccess(result.Value);
    }
}
