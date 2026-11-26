using Heardback.Modules.Identity.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>Issues short-lived JWT access tokens. They carry only the user id and role, never PII.</summary>
internal sealed class AccessTokenIssuer(IOptions<TokenSettings> settings, TimeProvider timeProvider)
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken Issue(User user)
    {
        var tokenSettings = settings.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + tokenSettings.AccessTokenLifetime;

        var claims = new Dictionary<string, object>
        {
            [IdentityClaims.Subject] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
        };
        if (user.IsAdmin)
        {
            claims[IdentityClaims.Role] = IdentityClaims.AdminRole;
        }

        if (user.Kind == UserKind.DemoVisitor)
        {
            claims[IdentityClaims.AccountKind] = IdentityClaims.DemoAccount;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = tokenSettings.Issuer,
            Audience = tokenSettings.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(tokenSettings.GetSigningKeyBytes()),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }
}

internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
