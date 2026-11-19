using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Samples;

/// <param name="Company">Company name.</param>
/// <param name="Role">Role or job title.</param>
/// <param name="Url">The posting link, if any.</param>
/// <param name="Channel">How the user applied.</param>
/// <param name="AppliedDaysAgo">Days before today the application was sent.</param>
/// <param name="PostingText">The pasted posting.</param>
/// <param name="Updates">Timeline updates after the submission, oldest first.</param>
internal sealed record SampleApplication(
    string Company,
    string Role,
    string? Url,
    ApplicationChannel Channel,
    int AppliedDaysAgo,
    string PostingText,
    IReadOnlyList<SampleUpdate> Updates);
