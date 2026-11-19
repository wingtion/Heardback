using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Heardback.Shared.Http;

/// <summary>A ProblemDetails response with a <c>Retry-After</c> header (an HTTP date, RFC 9110).</summary>
public sealed class RetryAfterResult(ProblemHttpResult problem, DateTimeOffset retryAt) : IResult
{
    public ProblemHttpResult Problem { get; } = problem;

    public DateTimeOffset RetryAt { get; } = retryAt;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Headers.RetryAfter = RetryAt.ToUniversalTime().ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        return Problem.ExecuteAsync(httpContext);
    }
}
