using Heardback.Shared.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Heardback.Modules.Identity.Authentication;

internal static class AuthenticationSetup
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<TokenSettings>()
            .BindConfiguration(TokenSettings.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                settings => settings.HasValidSigningKey(),
                $"{TokenSettings.SectionName}:SigningKey must be base64 of at least {TokenSettings.MinimumSigningKeyBytes} bytes.")
            .Validate(
                settings => settings.AbsoluteSessionLifetime >= settings.RefreshTokenLifetime,
                $"{TokenSettings.SectionName}:AbsoluteSessionLifetime must not be shorter than RefreshTokenLifetime.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<TokenSettings>, TimeProvider>(ConfigureJwtBearer);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(IdentityClaims.AdminRole));

        services.AddSingleton<AccessTokenIssuer>();

        return services;
    }

    private static void ConfigureJwtBearer(
        JwtBearerOptions options,
        IOptions<TokenSettings> settings,
        TimeProvider timeProvider)
    {
        var tokenSettings = settings.Value;

        // Keep "sub" and "role" as issued instead of remapping them to long WS-* claim URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = tokenSettings.Issuer,
            ValidAudience = tokenSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(tokenSettings.GetSigningKeyBytes()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = IdentityClaims.Subject,
            RoleClaimType = IdentityClaims.Role,
            ClockSkew = TimeSpan.Zero,

            // Validate expiry against the injected clock so lifetimes are testable and consistent with issuing.
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                return expires is not null && now < expires && (notBefore is null || now >= notBefore);
            },
        };
    }
}
