using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Modules.Applications.Analysis;
using Heardback.Modules.Applications.Board;
using Heardback.Modules.Applications.Community;
using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Applications.Create;
using Heardback.Modules.Applications.Delete;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Endpoints;
using Heardback.Modules.Applications.Persistence;
using Heardback.Modules.Applications.Read;
using Heardback.Modules.Applications.Samples;
using Heardback.Modules.Applications.Timeline;
using Heardback.Modules.Applications.Update;
using Heardback.Shared.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Applications;

/// <summary>
/// Registration entry point for the Applications module. Together with the types in
/// <c>Heardback.Modules.Applications.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class ApplicationsModule
{
    /// <summary>Registers the Applications module's services. Needs the Postings module and the LLM client.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplicationsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ApplicationsSettings>()
            .BindConfiguration(ApplicationsSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddModuleDbContext<ApplicationsDbContext>(ApplicationsDbContext.Schema);
        services.AddApplicationsRateLimiting();

        services.AddScoped<NoResponseClock>();
        services.AddScoped<PostingLinks>();
        services.AddScoped<CreateApplicationHandler>();
        services.AddScoped<GetApplicationHandler>();
        services.AddScoped<ListApplicationsHandler>();
        services.AddScoped<ApplicationSummaryHandler>();
        services.AddScoped<FindTrackedApplicationHandler>();
        services.AddScoped<UpdateApplicationHandler>();
        services.AddScoped<DeleteApplicationHandler>();
        services.AddScoped<UnusedPostingCleanup>();
        services.AddScoped<AddEventHandler>();
        services.AddScoped<GetBoardHandler>();
        services.AddScoped<IApplicationPostings, ApplicationPostings>();
        services.AddScoped<IApplicationHistory, ApplicationHistoryReader>();
        services.AddScoped<ICommunityOutcomes, CommunityOutcomesReader>();

        services.AddSingleton<PostingAnalysisPrompt>();
        services.AddScoped<IAnalysisJobHandler, PostingAnalysisJobHandler>();
        services.AddScoped<GetPostingAnalysisHandler>();
        services.AddScoped<RequestPostingAnalysisHandler>();
        services.AddScoped<AddSampleApplicationsHandler>();

        return services;
    }

    /// <summary>Maps the Applications module's HTTP endpoints (<c>/api/applications/*</c>).</summary>
    /// <param name="routes">The application's endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapApplicationsEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapApplicationEndpoints();
        routes.MapPostingAnalysisEndpoints();
        routes.MapSampleApplicationEndpoints();

        return routes;
    }
}
