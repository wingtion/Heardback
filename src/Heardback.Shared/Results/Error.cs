namespace Heardback.Shared.Results;

/// <summary>
/// An expected failure (not found, validation, forbidden, ...). <see cref="Code"/> is a stable,
/// machine-readable identifier such as <c>auth.invalid_credentials</c>; <see cref="Message"/> is safe to show users.
/// </summary>
public sealed record Error(ErrorKind Kind, string Code, string Message)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors = new Dictionary<string, string[]>();

    /// <summary>Per-field messages for <see cref="ErrorKind.Validation"/> errors, keyed by field name.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = NoFieldErrors;

    /// <summary>
    /// For <see cref="ErrorKind.QuotaExceeded"/>: when the limit resets. Sent as the <c>resetsAt</c> ProblemDetails
    /// extension and the <c>Retry-After</c> header, so clients can show the time in the user's zone.
    /// </summary>
    public DateTimeOffset? ResetsAt { get; init; }

    public static Error QuotaExceeded(string code, string message, DateTimeOffset resetsAt) =>
        new(ErrorKind.QuotaExceeded, code, message) { ResetsAt = resetsAt };

    public static Error Validation(string code, string field, string message) =>
        new(ErrorKind.Validation, code, message)
        {
            FieldErrors = new Dictionary<string, string[]> { [field] = [message] },
        };

    public static Error Unauthorized(string code, string message) => new(ErrorKind.Unauthorized, code, message);

    public static Error Forbidden(string code, string message) => new(ErrorKind.Forbidden, code, message);

    public static Error NotFound(string code, string message) => new(ErrorKind.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);
}
