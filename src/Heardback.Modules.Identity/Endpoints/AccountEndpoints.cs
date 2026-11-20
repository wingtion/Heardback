using System.Globalization;
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
    private const string ExportFileDateFormat = "yyyy-MM-dd";

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

        account.MapGet("/export", ExportAsync)
            .WithSummary("Download everything the signed-in user can see about themselves as one JSON file.")
            .RequireRateLimiting(AccountRateLimiting.ExportPolicy)
            .Produces<AccountExport>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> ExportAsync(
        ExportAccountHandler handler,
        TimeProvider timeProvider,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        var day = timeProvider.GetUtcNow().ToString(ExportFileDateFormat, CultureInfo.InvariantCulture);
        response.Headers.ContentDisposition = $"attachment; filename=\"heardback-export-{day}.json\"";
        response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(result.Value);
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
