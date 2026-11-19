using Heardback.Modules.Matching.Tailoring;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Matching.Endpoints;

internal static class TailoringEndpoints
{
    public const string TailoringPath = "/api/applications/{id:guid}/tailoring";
    public const string ConsentPath = CvEndpoints.BasePath + "/ai-consent";

    public static void MapTailoringEndpoints(this IEndpointRouteBuilder routes)
    {
        // Signed-in users only (fallback policy); only the caller's own CV, consent and applications are read.
        var consent = routes.MapGroup(ConsentPath)
            .WithTags("CV")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        consent.MapGet(string.Empty, GetConsentAsync)
            .WithSummary("Whether the user allows sending their CV text to the AI provider.")
            .Produces<AiConsentResponse>();

        consent.MapPut(string.Empty, SetConsentAsync)
            .RequireRateLimiting(MatchingRateLimiting.CvWritePolicy)
            .WithSummary("Allow or withdraw sending CV text to the AI provider; withdrawing deletes all suggestions.")
            .Produces<AiConsentResponse>()
            .ProducesValidationProblem();

        var tailoring = routes.MapGroup(TailoringPath)
            .WithTags("Matching")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tailoring.MapGet(string.Empty, GetAsync)
            .WithSummary("AI suggestions for tailoring the CV to this application, or where they stand.")
            .Produces<TailoringResponse>();

        tailoring.MapPost(string.Empty, RequestAsync)
            .RequireRateLimiting(MatchingRateLimiting.CvWritePolicy)
            .WithSummary("Ask for tailoring suggestions; current ones are returned as they are.")
            .Produces<TailoringResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> GetConsentAsync(AiConsentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.GetAsync(cancellationToken));

    private static async Task<IResult> SetConsentAsync(
        [FromBody] SetAiConsentRequest request,
        AiConsentHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.SetAsync(request.Allowed!.Value, cancellationToken));

    private static async Task<IResult> GetAsync(Guid id, GetTailoringHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> RequestAsync(
        Guid id,
        RequestTailoringHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
