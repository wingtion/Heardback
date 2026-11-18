namespace Heardback.Modules.Applications.Contracts;

/// <summary>The posting details on one application, as the user entered them.</summary>
/// <param name="Company">Company name.</param>
/// <param name="Role">Role or job title.</param>
/// <param name="PostingText">The pasted job description, or <see langword="null"/> when the user added none.</param>
public sealed record ApplicationPosting(string Company, string Role, string? PostingText);
