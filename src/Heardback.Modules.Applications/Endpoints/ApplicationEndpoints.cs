using Heardback.Modules.Applications.Board;
using Heardback.Modules.Applications.Create;
using Heardback.Modules.Applications.Delete;
using Heardback.Modules.Applications.Read;
using Heardback.Modules.Applications.Responses;
using Heardback.Modules.Applications.Timeline;
using Heardback.Modules.Applications.Update;
using Heardback.Shared.Http;
using Heardback.Shared.Paging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Applications.Endpoints;

internal static class ApplicationEndpoints
{
    public const string BasePath = "/api/applications";

    public static void MapApplicationEndpoints(this IEndpointRouteBuilder routes)
    {
        // Signed-in users only (fallback policy); every query is limited to the caller's own applications.
        var applications = routes.MapGroup(BasePath)
            .WithTags("Applications")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        applications.MapGet(string.Empty, ListAsync)
            .WithSummary("List the user's applications, most recently changed first.")
            .Produces<PagedResponse<ApplicationListItem>>()
            .ProducesValidationProblem();

        applications.MapGet("/summary", GetSummaryAsync)
            .WithSummary("Count the user's applications and those with no response.")
            .Produces<ApplicationSummaryResponse>();

        applications.MapGet("/tracked", FindTrackedAsync)
            .WithSummary("Find the user's application for a posting link, if they already track it.")
            .Produces<TrackedApplicationResponse>()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        applications.MapGet("/{id:guid}", GetAsync)
            .WithSummary("One application with its timeline.")
            .Produces<ApplicationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        applications.MapPost(string.Empty, CreateAsync)
            .WithSummary("Add an application.")
            .Produces<ApplicationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        applications.MapPut("/{id:guid}", UpdateAsync)
            .WithSummary("Edit an application. Send the version it was read with; 409 when it changed since.")
            .Produces<ApplicationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        applications.MapDelete("/{id:guid}", DeleteAsync)
            .WithSummary("Delete an application and its timeline.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        applications.MapPost("/{id:guid}/events", AddEventAsync)
            .WithSummary("Add a status change, a note, or both to the timeline (Add update, board moves).")
            .Produces<ApplicationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        applications.MapGet("/board", GetBoardAsync)
            .WithSummary("The board: one column per status with the newest cards and the real totals.")
            .Produces<BoardResponse>()
            .ProducesValidationProblem();
    }

    private static async Task<IResult> AddEventAsync(
        Guid id,
        [FromBody] AddEventRequest request,
        AddEventHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> GetBoardAsync(
        [AsParameters] BoardFilter filter,
        GetBoardHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(filter, cancellationToken));

    private static async Task<IResult> ListAsync(
        [AsParameters] PageRequest page,
        [AsParameters] ApplicationFilter filter,
        ListApplicationsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(page, filter, cancellationToken));

    private static async Task<IResult> GetSummaryAsync(
        ApplicationSummaryHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HandleAsync(cancellationToken));

    private static async Task<IResult> FindTrackedAsync(
        [AsParameters] TrackedApplicationQuery query,
        FindTrackedApplicationHandler handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(query.Url!, cancellationToken) is { } tracked
            ? TypedResults.Ok(tracked)
            : TypedResults.NoContent();

    private static async Task<IResult> GetAsync(
        Guid id,
        GetApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateApplicationRequest request,
        CreateApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"{BasePath}/{result.Value.Id}", result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateApplicationRequest request,
        UpdateApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteApplicationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }
}
