using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Matching.Account;
using Heardback.Modules.Matching.Cvs;
using Heardback.Modules.Matching.Cvs.Extraction;
using Heardback.Modules.Matching.Endpoints;
using Heardback.Modules.Matching.MatchResults;
using Heardback.Modules.Matching.Persistence;
using Heardback.Modules.Matching.Tailoring;
using Heardback.Shared.Demo;
using Heardback.Shared.Persistence;
using Heardback.Shared.Privacy;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Matching;

/// <summary>
/// Registration entry point for the Matching module. Together with the types in
/// <c>Heardback.Modules.Matching.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class MatchingModule
{
    /// <summary>Registers the Matching module's services. Needs the Applications module.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddMatchingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<MatchingSettings>()
            .BindConfiguration(MatchingSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddModuleDbContext<MatchingDbContext>(MatchingDbContext.Schema);
        services.AddMatchingRateLimiting();

        services.AddScoped<CvTextExtractor>();
        services.AddScoped<UploadCvHandler>();
        services.AddScoped<GetCvHandler>();
        services.AddScoped<DeleteCvHandler>();
        services.AddScoped<ExcludeSkillsHandler>();
        services.AddScoped<GetMatchHandler>();

        services.AddSingleton<TailoringPrompt>();
        services.AddScoped<TailoringSources>();
        services.AddScoped<IAnalysisJobHandler, TailoringJobHandler>();
        services.AddScoped<GetTailoringHandler>();
        services.AddScoped<RequestTailoringHandler>();
        services.AddScoped<AiConsentHandler>();
        services.AddScoped<IApplicationDeletedHandler, DeleteTailoringOnApplicationDeleted>();
        services.AddScoped<IUserDataEraser, MatchingDataEraser>();
        services.AddScoped<IDemoDataSeeder, MatchingDemoSeeder>();
        services.AddScoped<IUserDataExporter, MatchingDataExporter>();

        return services;
    }

    /// <summary>Maps the Matching module's HTTP endpoints (<c>/api/cv</c>, <c>/api/applications/{id}/match</c>).</summary>
    /// <param name="routes">The application's endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapMatchingEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapCvEndpoints();
        routes.MapMatchEndpoints();
        routes.MapTailoringEndpoints();

        return routes;
    }
}
