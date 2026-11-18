using Heardback.Modules.Matching.Cvs;
using Heardback.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Matching.Endpoints;

internal static class CvEndpoints
{
    public const string BasePath = "/api/cv";

    // Room for the multipart boundary and part headers around the file itself.
    private const int MultipartOverheadBytes = 64 * 1024;

    public static void MapCvEndpoints(this IEndpointRouteBuilder routes)
    {
        var maxRequestBytes = routes.ServiceProvider.GetRequiredService<IOptions<MatchingSettings>>().Value.CvMaxBytes
            + MultipartOverheadBytes;

        // Signed-in users only (fallback policy); every query is limited to the caller's own CV.
        var cv = routes.MapGroup(BasePath)
            .WithTags("CV")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        cv.MapGet(string.Empty, GetAsync)
            .WithSummary("The user's stored CV text, or 204 when they have not uploaded one.")
            .Produces<CvResponse>()
            .Produces(StatusCodes.Status204NoContent);

        // The API authenticates with a bearer token from a header, never a cookie, so a form post from another
        // site cannot act as the user; antiforgery tokens are not needed.
        cv.MapPost(string.Empty, UploadAsync)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(maxRequestBytes))
            .RequireRateLimiting(MatchingRateLimiting.CvUploadPolicy)
            .WithSummary("Upload a CV PDF (multipart field \"file\"). Only its text is kept; it replaces any earlier CV.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<CvResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        cv.MapPut("/excluded-skills", ExcludeSkillsAsync)
            .RequireRateLimiting(MatchingRateLimiting.CvWritePolicy)
            .WithSummary("Set the skills the user says are not theirs; they count in no match.")
            .Produces<CvResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        cv.MapDelete(string.Empty, DeleteAsync)
            .RequireRateLimiting(MatchingRateLimiting.CvWritePolicy)
            .WithSummary("Delete the user's CV.")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> GetAsync(GetCvHandler handler, CancellationToken cancellationToken) =>
        await handler.HandleAsync(cancellationToken) is { } stored
            ? TypedResults.Ok(stored)
            : TypedResults.NoContent();

    private static async Task<IResult> UploadAsync(
        IFormFile? file,
        UploadCvHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(file, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> ExcludeSkillsAsync(
        [FromBody] ExcludeSkillsRequest request,
        ExcludeSkillsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> DeleteAsync(DeleteCvHandler handler, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
