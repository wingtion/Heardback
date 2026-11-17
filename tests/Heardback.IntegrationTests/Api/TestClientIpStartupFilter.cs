using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Heardback.IntegrationTests.Api;

/// <summary>
/// TestServer has no real network connection, so tests choose the client IP with a header. Runs before the
/// app's own middleware, so rate limiting sees the chosen address.
/// </summary>
internal sealed class TestClientIpStartupFilter : IStartupFilter
{
    public const string HeaderName = "X-Test-Client-IP";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(HeaderName, out var value)
                    && IPAddress.TryParse(value, out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                return nextMiddleware(context);
            });
            next(app);
        };
}
