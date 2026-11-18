using Heardback.Modules.Postings.Catalogue;
using Heardback.Modules.Postings.Contracts;
using Heardback.Modules.Postings.Persistence;
using Heardback.Shared.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Postings;

/// <summary>
/// Registration entry point for the Postings module. Together with the types in
/// <c>Heardback.Modules.Postings.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class PostingsModule
{
    /// <summary>Registers the Postings module's services, including <see cref="IJobPostings"/>.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPostingsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddModuleDbContext<PostingsDbContext>(PostingsDbContext.Schema);
        services.AddScoped<IJobPostings, JobPostings>();

        return services;
    }
}
