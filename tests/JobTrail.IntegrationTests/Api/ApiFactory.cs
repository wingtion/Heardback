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

    // Satisfies startup validation for tests that never touch the database; never opened.
    private const string UnusedConnectionString = "Host=localhost;Database=jobtrail_unused";

    public string Environment { get; init; } = TestingEnvironment;

    /// <summary>A real database to run against; migrations are applied on startup when set.</summary>
    public string? ConnectionString { get; init; }

    public Action<IServiceCollection>? TestServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.UseSetting("Database:ConnectionString", ConnectionString ?? UnusedConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", (ConnectionString is not null).ToString());

        if (TestServices is not null)
        {
            builder.ConfigureTestServices(TestServices);
        }
    }
}
