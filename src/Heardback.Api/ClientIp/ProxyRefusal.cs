namespace Heardback.Api.ClientIp;

/// <summary>Why <see cref="NetlifyProxyMiddleware"/> refused a request; logged, never shown to the caller.</summary>
internal enum ProxyRefusal
{
    MissingSignature,
    InvalidSignature,
    MissingClientIp,
}
