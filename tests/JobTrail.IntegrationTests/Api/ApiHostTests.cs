using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JobTrail.IntegrationTests.Api;

public sealed class ApiHostTests
{
    private const string ProblemJson = "application/problem+json";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHealth_ApiRunning_ReturnsHealthy()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task GetUnknownRoute_Anonymous_ReturnsUnauthorizedProblemDetails()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        // The fallback policy also covers unmatched routes, so anonymous callers cannot probe which routes exist.
        using var response = await client.GetAsync(new Uri("/does-not-exist", UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem?.Status);
    }

    [Fact]
    public async Task GetHealth_UnhandledException_ReturnsProblemDetailsWithoutInternals()
    {
        await using var factory = new ApiFactory
        {
            TestServices = services => services.AddSingleton<HealthCheckService, ThrowingHealthCheckService>(),
        };
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), CancellationToken);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(ThrowingHealthCheckService.SensitiveMessage, body, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetOpenApiDocument_DevelopmentEnvironment_ReturnsDocument()
    {
        await using var factory = new ApiFactory { Environment = "Development" };
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApiDocument_NonDevelopmentEnvironment_IsNotExposed()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative), CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Startup_ClientIpSourceNotConfigured_FailsFast()
    {
        await using var factory = new ApiFactory();
        factory.Settings["ClientIp:Source"] = null;

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("ClientIp:Source", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("not-an-ip", null)]
    [InlineData(null, "10.0.0.0/99")]
    public async Task Startup_ForwardedHeadersWithoutValidTrustedProxy_FailsFast(string? proxy, string? network)
    {
        await using var factory = new ApiFactory();
        factory.Settings["ClientIp:Source"] = "ForwardedHeaders";
        factory.Settings["ClientIp:TrustedProxies:0"] = proxy;
        factory.Settings["ClientIp:TrustedNetworks:0"] = network;

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("TrustedProxies", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Identity:Tokens:SigningKey", "c2hvcnQta2V5", "SigningKey")]
    [InlineData("Identity:Tokens:SigningKey", "this is not base64!", "SigningKey")]
    [InlineData("Identity:Invites:BootstrapAdminCode", "SHORT", "BootstrapAdminCode")]
    [InlineData("Identity:Tokens:AbsoluteSessionLifetime", "7.00:00:00", "AbsoluteSessionLifetime")]
    public async Task Startup_WeakOrInconsistentAuthSettings_FailsFast(string key, string value, string expectedInMessage)
    {
        await using var factory = new ApiFactory();
        factory.Settings[key] = value;

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Startup_MissingSigningKey_FailsFastWithValidationError()
    {
        await using var factory = new ApiFactory();
        factory.Settings["Identity:Tokens:SigningKey"] = null;

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("SigningKey", exception.Message, StringComparison.Ordinal);
    }
}
