using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Heardback.IntegrationTests.Api;

/// <summary>
/// Hosts the API in memory. Defaults to a non-Development environment so tests see production behaviour
/// (no developer exception page, no OpenAPI endpoints).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestingEnvironment = "Testing";
    public const string BootstrapAdminCode = "TEST-BTSP-ADMN";

    /// <summary>32 bytes of test-only key material, base64. Never used outside tests.</summary>
    public const string SigningKey = "dGVzdC1vbmx5LXNpZ25pbmcta2V5LTMyLWJ5dGVzISE=";

    // Satisfies startup validation for tests that never touch the database; never opened.
    private const string UnusedConnectionString = "Host=localhost;Database=heardback_unused";

    public ApiFactory()
    {
        // The refresh cookie is Secure; the test client only sends it over https.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public string Environment { get; init; } = TestingEnvironment;

    /// <summary>A real database to run against; migrations are applied on startup when set.</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Replaces the system clock, so tests can move time forward.</summary>
    public FakeTimeProvider? Clock { get; init; }

    public IDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>
    {
        ["Identity:Tokens:SigningKey"] = SigningKey,
        ["Identity:Invites:BootstrapAdminCode"] = BootstrapAdminCode,
        ["ClientIp:Source"] = "Direct",
    };

    public Action<IServiceCollection>? TestServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.UseSetting("Database:ConnectionString", ConnectionString ?? UnusedConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", (ConnectionString is not null).ToString());
        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
            if (Clock is not null)
            {
                services.AddSingleton<TimeProvider>(Clock);
            }

            TestServices?.Invoke(services);
        });
    }
}
