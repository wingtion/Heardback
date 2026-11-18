namespace Heardback.Modules.Applications.Domain;

/// <summary>The details a user enters when adding or editing an application; the user's own copy (ADR-0004).</summary>
internal sealed record ApplicationDetails(
    string Company,
    string Role,
    string? Url,
    string? PostingText,
    ApplicationChannel Channel,
    DateOnly AppliedOn);
