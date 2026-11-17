namespace Heardback.Shared.Results;

/// <summary>The category of an expected failure; decides the HTTP status it maps to.</summary>
public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
}
