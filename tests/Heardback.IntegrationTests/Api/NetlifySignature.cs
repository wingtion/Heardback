using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Heardback.IntegrationTests.Api;

/// <summary>Creates x-nf-sign tokens shaped like the ones Netlify's signed proxy rewrites send.</summary>
internal static class NetlifySignature
{
    public const string HeaderName = "X-Nf-Sign";
    public const string ClientIpHeader = "X-Nf-Client-Connection-Ip";

    /// <summary>Test-only secret, at least 32 characters. Never used outside tests.</summary>
    public const string Secret = "test-only-netlify-signature-secret-0123456789";

    public static string Create(
        string secret = Secret,
        string issuer = "netlify",
        DateTimeOffset? expires = null,
        bool includeExpiry = true)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Subject = new ClaimsIdentity(
            [
                new Claim("netlify_id", "4f1e9b3a-0000-4000-8000-000000000001"),
                new Claim("site_url", "https://heardback.netlify.app"),
                new Claim("deploy_context", "production"),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256),
        };

        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        if (includeExpiry)
        {
            descriptor.Expires = (expires ?? DateTimeOffset.UtcNow.AddMinutes(5)).UtcDateTime;
        }

        return handler.CreateToken(descriptor);
    }

    /// <summary>A token with "alg": "none" and no signature at all.</summary>
    public static string CreateUnsigned()
    {
        static string Encode(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));

        var expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        return $"{Encode("""{"alg":"none","typ":"JWT"}""")}.{Encode($$"""{"iss":"netlify","exp":{{expiry}}}""")}.";
    }
}
