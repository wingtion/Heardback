namespace Heardback.Api.ClientIp;

/// <summary>Settings for <see cref="ClientIpSource.NetlifySignedProxy"/>, under <c>ClientIp:Netlify</c>.</summary>
internal sealed class NetlifyProxySettings
{
    public const int MinimumSecretLength = 32;

    /// <summary>
    /// The shared secret Netlify signs proxied requests with: the value of the environment variable named
    /// by <c>signed</c> in netlify.toml. Environment variable in production, never committed.
    /// </summary>
    public string SignatureSecret { get; set; } = string.Empty;

    /// <summary>The header in which Netlify passes the original client IP to the proxied site.</summary>
    public string ClientIpHeader { get; set; } = "X-Nf-Client-Connection-Ip";

    public bool IsValid() =>
        SignatureSecret.Length >= MinimumSecretLength && !string.IsNullOrWhiteSpace(ClientIpHeader);
}
