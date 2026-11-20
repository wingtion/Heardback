using Heardback.Modules.Identity.Account;
using Heardback.Modules.Identity.Authentication;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Identity.Endpoints;

internal static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var account = routes.MapGroup("/api/account").WithTags("Account");

        // Checks a password, so it shares the per-IP budget of sign-in.
        account.MapDelete(string.Empty, DeleteAsync)
            .WithSummary("Delete the signed-in user's account and all their data. Needs the current password.")
            .RequireRateLimiting(AuthRateLimiting.CredentialsPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> DeleteAsync(
        [FromBody] DeleteAccountRequest request,
        DeleteAccountHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        RefreshTokenCookie.Delete(response);
        return TypedResults.NoContent();
    }
}
