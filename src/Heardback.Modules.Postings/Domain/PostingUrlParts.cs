namespace Heardback.Modules.Postings.Domain;

/// <param name="Key">Comparison key: no scheme, no www, no tracking parameters, parameters sorted.</param>
/// <param name="CleanUrl">The link to keep: as typed, minus credentials, tracking parameters and the fragment.</param>
internal sealed record PostingUrlParts(string Key, string CleanUrl);
