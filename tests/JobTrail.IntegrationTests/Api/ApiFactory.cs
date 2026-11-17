using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.IntegrationTests.Api;

/// <summary>
/// Hosts the API in memory. Defaults to a non-Development environment so tests see production behaviour
/// (no developer exception page, no OpenAPI endpoints).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";
    public const string FrontendOrigin = "http://localhost:4200";

    public string Environment { get; init; } = TestingEnvironment;

    public bool ConfigureCorsOrigins { get; init; } = true;

    public Action<IServiceCollection>? TestServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);

        if (ConfigureCorsOrigins)
        {
            builder.UseSetting("Cors:AllowedOrigins:0", FrontendOrigin);
        }

        if (TestServices is not null)
        {
            builder.ConfigureTestServices(TestServices);
        }
    }
}
