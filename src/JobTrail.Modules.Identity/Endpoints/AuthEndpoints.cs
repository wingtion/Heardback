using JobTrail.Modules.Identity.Authentication;
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
            .RequireRateLimiting(AuthRateLimiting.CredentialsPolicy)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/login", LoginAsync)
            .WithSummary("Sign in with email and password.")
            .RequireRateLimiting(AuthRateLimiting.CredentialsPolicy)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/refresh", RefreshAsync)
            .WithSummary("Exchange the refresh cookie for a new access token; rotates the cookie.")
            .RequireRateLimiting(AuthRateLimiting.RefreshPolicy)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        // Anonymous and hits the database, so it shares the refresh budget.
        auth.MapPost("/logout", LogoutAsync)
            .WithSummary("End this session and clear the refresh cookie.")
            .RequireRateLimiting(AuthRateLimiting.RefreshPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshSessionHandler handler,
        HttpRequest request,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(RefreshTokenCookie.Read(request), cancellationToken);

        // A superseded refresh lost to a concurrent one; the browser's cookie already holds the winner's
        // token, so clearing it here would sign the user out. Every other failure clears the dead cookie.
        if (!result.IsSuccess && result.Error.Code != IdentityErrors.RefreshSuperseded.Code)
        {
            RefreshTokenCookie.Delete(response);
        }

        return ToSessionResult(result, response, session => TypedResults.Ok(AuthResponse.From(session)));
    }

    private static async Task<IResult> LogoutAsync(
        LogoutHandler handler,
        HttpRequest request,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(RefreshTokenCookie.Read(request), cancellationToken);
        RefreshTokenCookie.Delete(response);
        return TypedResults.NoContent();
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
