using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Heardback.Api.ClientIp;

/// <summary>
/// Serves only requests that came through Netlify's signed proxy rewrite, and takes the client IP from
/// Netlify's header. Anyone reaching the API host directly could set that header to anything, so without a
/// valid signature the header is never believed and the request is refused (ADR-0002).
/// </summary>
internal sealed partial class NetlifyProxyMiddleware : IMiddleware
{
    public const string SignatureHeader = "X-Nf-Sign";
    public const string Issuer = "netlify";

    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);

    private readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly TokenValidationParameters _validationParameters;
    private readonly string _clientIpHeader;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NetlifyProxyMiddleware> _logger;

    public NetlifyProxyMiddleware(
        IOptions<ClientIpSettings> settings,
        TimeProvider timeProvider,
        ILogger<NetlifyProxyMiddleware> logger)
    {
        var netlify = settings.Value.Netlify;
        _clientIpHeader = netlify.ClientIpHeader;
        _timeProvider = timeProvider;
        _logger = logger;
        _validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(netlify.SignatureSecret)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidIssuer = Issuer,
            ValidateIssuer = true,
            ValidateAudience = false,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            LifetimeValidator = IsWithinLifetime,
        };
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.Equals(HealthEndpoint.Path, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var refusal = await CheckAsync(context);
        if (refusal is { } reason)
        {
            LogRefused(_logger, reason);

            // The body is left empty so UseStatusCodePages turns it into a 403 ProblemDetails.
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }

    private async Task<ProxyRefusal?> CheckAsync(HttpContext context)
    {
        if (!TryGetSingle(context.Request.Headers, SignatureHeader, out var signature))
        {
            return ProxyRefusal.MissingSignature;
        }

        var validation = await _tokenHandler.ValidateTokenAsync(signature, _validationParameters);
        if (!validation.IsValid)
        {
            return ProxyRefusal.InvalidSignature;
        }

        if (!TryGetSingle(context.Request.Headers, _clientIpHeader, out var clientIp)
            || !IPAddress.TryParse(clientIp, out var address))
        {
            return ProxyRefusal.MissingClientIp;
        }

        context.Connection.RemoteIpAddress = address;
        return null;
    }

    // The token library would read the system clock; the app reads time only through TimeProvider.
    private bool IsWithinLifetime(
        DateTime? notBefore,
        DateTime? expires,
        SecurityToken securityToken,
        TokenValidationParameters parameters)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return expires is { } expiry
            && now - AllowedClockSkew < expiry
            && (notBefore is not { } start || start <= now + AllowedClockSkew);
    }

    private static bool TryGetSingle(IHeaderDictionary headers, string name, out string value)
    {
        value = string.Empty;
        if (!headers.TryGetValue(name, out var values) || values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            return false;
        }

        value = values[0]!.Trim();
        return true;
    }

    // Neither the client IP nor the signature is logged: the first is personal data, the second a credential.
    [LoggerMessage(Level = LogLevel.Information, Message = "Refused a request that did not come through the Netlify proxy: {Reason}")]
    private static partial void LogRefused(ILogger logger, ProxyRefusal reason);
}
