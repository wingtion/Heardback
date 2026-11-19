using System.Net;
using System.Text;

namespace Heardback.UnitTests.Llm;

/// <summary>Answers every request with one canned response (or exception) and remembers what was sent.</summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage> _respond;

    private StubHttpHandler(Func<HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastBody { get; private set; }

    public static StubHttpHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(() => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    public static StubHttpHandler Status(HttpStatusCode status, Action<HttpResponseMessage>? configure = null) =>
        new(() =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent("{}") };
            configure?.Invoke(response);
            return response;
        });

    public static StubHttpHandler Throws(Exception exception) => new(() => throw exception);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return _respond();
    }
}
