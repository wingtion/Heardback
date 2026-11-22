namespace Heardback.Modules.Identity.Account;

/// <summary>
/// The whole data export file (ADR-0015). <see cref="FormatVersion"/> changes whenever a field is renamed or removed,
/// so an old file stays readable.
/// </summary>
internal sealed record AccountExport(
    int FormatVersion,
    DateTimeOffset ExportedAt,
    AccountProfileExport Account,
    IReadOnlyDictionary<string, object> Data);

/// <summary>The account itself in the data export.</summary>
internal sealed record AccountProfileExport(string Email, string DisplayName, bool IsAdmin, DateTimeOffset JoinedAt);
