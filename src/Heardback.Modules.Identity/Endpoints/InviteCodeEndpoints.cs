using Heardback.Modules.Identity.Invites;
using Heardback.Shared.Authorization;
using Heardback.Shared.Paging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Identity.Endpoints;

internal static class InviteCodeEndpoints
{
    public static void MapInviteCodeEndpoints(this IEndpointRouteBuilder routes)
    {
        var invites = routes.MapGroup("/api/admin/invite-codes")
            .WithTags("Admin")
            .RequireAuthorization(AuthorizationPolicies.Admin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        invites.MapPost(string.Empty, CreateAsync)
            .WithSummary("Create a single-use invite code.")
            .Produces<InviteCodeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        invites.MapGet(string.Empty, ListAsync)
            .WithSummary("List invite codes, newest first.")
            .Produces<InviteCodeListResponse>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateInviteCodeRequest? request,
        CreateInviteCodeHandler handler,
        CancellationToken cancellationToken)
    {
        var invite = await handler.HandleAsync(request, cancellationToken);
        return TypedResults.Created($"/api/admin/invite-codes/{invite.Id}", invite);
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] PageRequest page,
        ListInviteCodesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(page, cancellationToken));
}
