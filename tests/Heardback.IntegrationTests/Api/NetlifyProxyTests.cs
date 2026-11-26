using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Heardback.IntegrationTests.Api;

/// <summary>
/// Behind Netlify, only requests signed by Netlify's proxy rewrite are served, so a caller reaching the API
/// host directly cannot pick its own client IP (ADR-0002).
/// </summary>
public sealed class NetlifyProxyTests : IAsyncDisposable
{
    private const string ProblemJson = "application/problem+json";
    private static readonly Uri ProtectedRoute = new("/does-not-exist", UriKind.Relative);

    private readonly ApiFactory _factory = CreateFactory();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Request_WithoutSignature_ReturnsForbiddenProblemDetails()
    {
        using var response = await SendAsync(signature: null, clientIp: "203.0.113.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(StatusCodes.Status403Forbidden, problem?.Status);
    }

    [Fact]
    public async Task Request_ValidSignature_ReachesTheApp()
    {
        using var response = await SendAsync(NetlifySignature.Create(), clientIp: "203.0.113.1");

        // Past the proxy check, the fallback policy answers an anonymous caller as usual.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_SignedWithAnotherSecret_ReturnsForbidden()
    {
        var signature = NetlifySignature.Create(secret: "a-different-secret-that-is-also-long-enough");

        using var response = await SendAsync(signature, clientIp: "203.0.113.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_UnsignedAlgNoneToken_ReturnsForbidden()
    {
        using var response = await SendAsync(NetlifySignature.CreateUnsigned(), clientIp: "203.0.113.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_ExpiredSignature_ReturnsForbidden()
    {
        var signature = NetlifySignature.Create(expires: DateTimeOffset.UtcNow.AddMinutes(-5));

        using var response = await SendAsync(signature, clientIp: "203.0.113.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_SignatureWithoutExpiry_ReturnsForbidden()
    {
        using var response = await SendAsync(NetlifySignature.Create(includeExpiry: false), clientIp: "203.0.113.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_WrongIssuer_ReturnsForbidden()
    {
        using var response = await SendAsync(NetlifySignature.Create(issuer: "someone-else"), clientIp: "203.0.113.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-ip")]
    public async Task Request_ValidSignatureWithoutUsableClientIp_ReturnsForbidden(string? clientIp)
    {
        using var response = await SendAsync(NetlifySignature.Create(), clientIp);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WithoutSignature_ReturnsHealthy()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public async Task Startup_NetlifyWithoutStrongSecret_FailsFast(string? secret)
    {
        await using var misconfigured = new ApiFactory();
        misconfigured.Settings["ClientIp:Source"] = "NetlifySignedProxy";
        misconfigured.Settings["ClientIp:Netlify:SignatureSecret"] = secret;

        var exception = Assert.Throws<OptionsValidationException>(() => misconfigured.CreateClient());

        Assert.Contains("SignatureSecret", exception.Message, StringComparison.Ordinal);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    internal static ApiFactory CreateFactory()
    {
        var created = new ApiFactory();
        ConfigureNetlify(created.Settings);
        return created;
    }

    internal static void ConfigureNetlify(IDictionary<string, string?> settings)
    {
        settings["ClientIp:Source"] = "NetlifySignedProxy";
        settings["ClientIp:Netlify:SignatureSecret"] = NetlifySignature.Secret;
    }

    private async Task<HttpResponseMessage> SendAsync(string? signature, string? clientIp)
    {
        using var client = _factory.CreateClient();
        if (signature is not null)
        {
            client.DefaultRequestHeaders.Add(NetlifySignature.HeaderName, signature);
        }

        if (clientIp is not null)
        {
            client.DefaultRequestHeaders.Add(NetlifySignature.ClientIpHeader, clientIp);
        }

        return await client.GetAsync(ProtectedRoute, CancellationToken);
    }
}
