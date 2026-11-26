using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Heardback.Api.ClientIp;

/// <summary>
/// Where the real client IP comes from. Per-IP rate limits depend on it: behind a proxy, the connection
/// address is the proxy's, and every user would share one budget. There is deliberately no default, so a
/// deployment that has not decided fails to start (ADR-0002).
/// </summary>
internal sealed class ClientIpSettings
{
    public const string SectionName = "ClientIp";

    [Required(ErrorMessage =
        "ClientIp:Source must be set to Direct, ForwardedHeaders or NetlifySignedProxy (see ADR-0002).")]
    public ClientIpSource? Source { get; set; }

    public NetlifyProxySettings Netlify { get; set; } = new();

    /// <summary>Proxy addresses allowed to set X-Forwarded-For, e.g. <c>10.0.0.5</c>.</summary>
    public string[] TrustedProxies { get; set; } = [];

    /// <summary>Proxy networks allowed to set X-Forwarded-For, in CIDR form, e.g. <c>172.16.0.0/12</c>.</summary>
    public string[] TrustedNetworks { get; set; } = [];

    /// <summary>How many proxy hops to unwind from X-Forwarded-For.</summary>
    [Range(1, 5)]
    public int ForwardLimit { get; set; } = 1;

    public bool HasValidTrustedSources() =>
        Source != ClientIpSource.ForwardedHeaders
        || ((TrustedProxies.Length > 0 || TrustedNetworks.Length > 0)
            && TrustedProxies.All(proxy => IPAddress.TryParse(proxy, out _))
            && TrustedNetworks.All(network => IPNetwork.TryParse(network, out _)));

    public bool HasValidNetlifySettings() => Source != ClientIpSource.NetlifySignedProxy || Netlify.IsValid();
}
