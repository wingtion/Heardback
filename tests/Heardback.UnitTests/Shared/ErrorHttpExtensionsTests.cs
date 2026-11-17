using Heardback.Shared.Http;
using Heardback.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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
    public void ToProblem_ValidationError_ReturnsValidationProblemWithFieldErrors()
    {
        var error = Error.Validation("sample.invalid", "email", "Email is invalid.");

        var problem = Assert.IsType<ValidationProblem>(error.ToProblem());

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(["Email is invalid."], problem.ProblemDetails.Errors["email"]);
    }
}
