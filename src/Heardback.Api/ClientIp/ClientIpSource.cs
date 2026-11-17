namespace Heardback.Api.ClientIp;

internal enum ClientIpSource
{
    /// <summary>Clients connect straight to the API; the connection address is the client's.</summary>
    Direct,

    /// <summary>Clients come through a proxy; take X-Forwarded-For, but only from trusted proxies.</summary>
    ForwardedHeaders,
}
