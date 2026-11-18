namespace Heardback.Modules.Matching.Cvs;

/// <summary>
/// The user's stored CV: the text exactly as kept, with personal details replaced by markers such as
/// <c>[email]</c>, so the user can check what Heardback stores; the dictionary skills it names
/// (<see cref="Skills"/>) and those the user marked as not theirs (<see cref="ExcludedSkills"/>).
/// </summary>
internal sealed record CvResponse(
    DateTimeOffset UploadedAt,
    int PageCount,
    string Text,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> ExcludedSkills)
{
    // Skills are found on every read, so dictionary updates reach CVs uploaded earlier too.
    public static CvResponse From(DateTimeOffset uploadedAt, int pageCount, string text, IReadOnlyCollection<string> excluded) =>
        new(
            uploadedAt,
            pageCount,
            text,
            CvSkills.Found(text, excluded).Select(skill => skill.Name).ToList(),
            CvSkills.InCatalogOrder(excluded));
}
