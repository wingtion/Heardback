using Heardback.Modules.Identity.Authentication;
using Heardback.Modules.Identity.Members;
using Heardback.Modules.Identity.Recovery;
using Heardback.Shared.Authorization;
using Heardback.Shared.Http;
using Heardback.Shared.Paging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Identity.Endpoints;

internal static class MemberEndpoints
{
    public static void MapMemberEndpoints(this IEndpointRouteBuilder routes)
    {
        var members = routes.MapGroup("/api/admin/members")
            .WithTags("Admin")
            .RequireAuthorization(AuthorizationPolicies.Admin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        members.MapGet(string.Empty, ListAsync)
            .WithSummary("List members with their email, oldest first.")
            .Produces<PagedResponse<MemberResponse>>()
            .ProducesValidationProblem();

        members.MapPost("/{id:guid}/reset-code", IssueResetCodeAsync)
            .WithSummary("Issue a one-time password reset code for a member (not for admins). Replaces an open one.")
            .RequireRateLimiting(AccountRateLimiting.ResetCodePolicy)
            .Produces<ResetCodeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] PageRequest page,
        ListMembersHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(page, cancellationToken));

    private static async Task<IResult> IssueResetCodeAsync(
        Guid id,
        IssueResetCodeHandler handler,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(result.Value);
    }
}
