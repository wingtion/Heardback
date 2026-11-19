using Heardback.Modules.Applications.Samples;
using Heardback.Shared.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Endpoints;

internal static class SampleApplicationEndpoints
{
    public const string Path = ApplicationEndpoints.BasePath + "/samples";

    public static void MapSampleApplicationEndpoints(this IEndpointRouteBuilder routes)
    {
        // Admins only, and only where the setting turns it on (Development by default, never by default in
        // production; decided by the author on 2026-11-19). Samples go to the caller's own list.
        var samples = routes.MapGroup(Path)
            .WithTags("Applications")
            .RequireAuthorization(AuthorizationPolicies.Admin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        samples.MapGet(string.Empty, GetStatus)
            .WithSummary("Whether this server offers sample applications for trying the app out.")
            .Produces<SampleDataStatus>();

        samples.MapPost(string.Empty, AddAsync)
            .RequireRateLimiting(ApplicationsRateLimiting.WritePolicy)
            .WithSummary("Add realistic sample applications to your own list; ones you already track are skipped.")
            .Produces<SampleApplicationsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static Ok<SampleDataStatus> GetStatus(IOptions<ApplicationsSettings> settings) =>
        TypedResults.Ok(new SampleDataStatus(settings.Value.SampleDataEnabled));

    private static async Task<IResult> AddAsync(
        IOptions<ApplicationsSettings> settings,
        AddSampleApplicationsHandler handler,
        CancellationToken cancellationToken)
    {
        if (!settings.Value.SampleDataEnabled)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(await handler.HandleAsync(cancellationToken));
    }
}
