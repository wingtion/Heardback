using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Heardback.Api.ClientIp;

internal static class ClientIpResolution
{
    public static IServiceCollection AddClientIpResolution(this IServiceCollection services)
    {
        services.AddOptions<ClientIpSettings>()
            .BindConfiguration(ClientIpSettings.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                settings => settings.HasValidTrustedSources(),
                "ClientIp:Source=ForwardedHeaders needs at least one valid ClientIp:TrustedProxies address "
                + "or ClientIp:TrustedNetworks CIDR range.")
            .ValidateOnStart();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ClientIpSettings>>((options, settings) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                options.ForwardLimit = settings.Value.ForwardLimit;

                // Trust only what is configured; the framework default (loopback) is not our proxy.
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var proxy in settings.Value.TrustedProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }

                foreach (var network in settings.Value.TrustedNetworks)
                {
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }
            });

        return services;
    }

    /// <summary>Must run before anything that reads the client IP, such as rate limiting.</summary>
    public static IApplicationBuilder UseClientIpResolution(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<ClientIpSettings>>().Value;
        return settings.Source == ClientIpSource.ForwardedHeaders ? app.UseForwardedHeaders() : app;
    }
}
