namespace Heardback.Modules.Applications.Community;

/// <summary>
/// One settled application of another member, as read for grouping. It never leaves the module: only the counts
/// built from it do (ADR-0013).
/// </summary>
internal sealed record OtherApplication(
    Guid UserId,
    Guid PostingId,
    string? CompanyKey,
    DateOnly AppliedOn,
    DateOnly? FirstResponseOn);
