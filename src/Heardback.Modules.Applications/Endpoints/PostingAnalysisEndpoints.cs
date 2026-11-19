using Heardback.Modules.Applications.Analysis;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Heardback.Modules.Applications.Endpoints;

internal static class PostingAnalysisEndpoints
{
    public const string Path = ApplicationEndpoints.BasePath + "/{id:guid}/posting-analysis";

    public static void MapPostingAnalysisEndpoints(this IEndpointRouteBuilder routes)
    {
        // Signed-in users only (fallback policy); only the caller's own applications are read.
        var analysis = routes.MapGroup(Path)
            .WithTags("Applications")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        analysis.MapGet(string.Empty, GetAsync)
            .WithSummary("The AI analysis of the application's posting text, or where it stands.")
            .Produces<PostingAnalysisResponse>();

        analysis.MapPost(string.Empty, RequestAsync)
            .RequireRateLimiting(ApplicationsRateLimiting.WritePolicy)
            .WithSummary("Ask for an AI analysis of the posting text; ready at once when the same text was analysed.")
            .Produces<PostingAnalysisResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        GetPostingAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> RequestAsync(
        Guid id,
        RequestPostingAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
