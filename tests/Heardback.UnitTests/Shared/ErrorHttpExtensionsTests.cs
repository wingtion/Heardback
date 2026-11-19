using Heardback.Shared.Http;
using Heardback.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.UnitTests.Shared;

public sealed class ErrorHttpExtensionsTests
{
    [Theory]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    public void ToProblem_NonValidationError_ReturnsProblemWithStatusAndCode(ErrorKind kind, int expectedStatus)
    {
        var error = new Error(kind, "sample.code", "Something expected went wrong.");

        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal("sample.code", problem.ProblemDetails.Extensions[ErrorHttpExtensions.ErrorCodeExtension]);
        Assert.Equal(error.Message, problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task ToProblem_QuotaExceeded_Is429WithResetTimeAndRetryAfterHeader()
    {
        var resetsAt = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);
        var error = Error.QuotaExceeded("sample.quota", "You've used today's limit.", resetsAt);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };

        var result = Assert.IsType<RetryAfterResult>(error.ToProblem());
        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status429TooManyRequests, httpContext.Response.StatusCode);
        Assert.Equal("Fri, 20 Nov 2026 00:00:00 GMT", httpContext.Response.Headers.RetryAfter.ToString());
        Assert.Equal(resetsAt, result.Problem.ProblemDetails.Extensions[ErrorHttpExtensions.ResetsAtExtension]);
        Assert.Equal("sample.quota", result.Problem.ProblemDetails.Extensions[ErrorHttpExtensions.ErrorCodeExtension]);
    }

    [Fact]
    public void ToProblem_ValidationError_ReturnsValidationProblemWithFieldErrors()
    {
        var error = Error.Validation("sample.invalid", "email", "Email is invalid.");

        var problem = Assert.IsType<ValidationProblem>(error.ToProblem());

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(["Email is invalid."], problem.ProblemDetails.Errors["email"]);
    }
}
