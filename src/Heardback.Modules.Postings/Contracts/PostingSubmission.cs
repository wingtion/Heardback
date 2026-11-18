namespace Heardback.Modules.Postings.Contracts;

/// <summary>What a user entered about a job posting, used to find or create the shared posting.</summary>
/// <param name="Url">The posting link, if any. Must be an absolute http(s) link (see <see cref="PostingUrlAttribute"/>).</param>
/// <param name="Company">Company name.</param>
/// <param name="Title">Role (job title).</param>
/// <param name="Text">The pasted job description, if any.</param>
public sealed record PostingSubmission(string? Url, string Company, string Title, string? Text);
