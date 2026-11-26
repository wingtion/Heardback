namespace Heardback.Api.ClientIp;

internal enum ClientIpSource
{
    /// <summary>Clients connect straight to the API; the connection address is the client's.</summary>
    Direct,

    /// <summary>Clients come through a proxy; take X-Forwarded-For, but only from trusted proxies.</summary>
    ForwardedHeaders,

    /// <summary>
    /// Clients come through Netlify's signed proxy rewrite. Only requests carrying a valid Netlify signature
    /// are served, and the client IP comes from Netlify's client IP header (ADR-0002).
    /// </summary>
    NetlifySignedProxy,
}
