using Heardback.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace Heardback.Shared.Http;

/// <summary>Turns expected failures into RFC 9457 ProblemDetails responses.</summary>
public static class ErrorHttpExtensions
{
    public const string ErrorCodeExtension = "code";

    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var extensions = new Dictionary<string, object?> { [ErrorCodeExtension] = error.Code };

        return error.Kind == ErrorKind.Validation
            ? TypedResults.ValidationProblem(
                error.FieldErrors.ToDictionary(),
                detail: error.Message,
                extensions: extensions)
            : TypedResults.Problem(
                statusCode: ToStatusCode(error.Kind),
                detail: error.Message,
                extensions: extensions);
    }

    public static int ToStatusCode(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unmapped error kind."),
    };
}
