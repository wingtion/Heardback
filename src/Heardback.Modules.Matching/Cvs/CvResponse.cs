using Heardback.Modules.Matching.Skills;

namespace Heardback.Modules.Matching.Cvs;

/// <summary>
/// The user's stored CV: the text exactly as kept, with personal details replaced by markers such as
/// <c>[email]</c>, so the user can check what Heardback stores, and the dictionary skills it names.
/// </summary>
internal sealed record CvResponse(DateTimeOffset UploadedAt, int PageCount, string Text, IReadOnlyList<string> Skills)
{
    public static CvResponse From(DateTimeOffset uploadedAt, int pageCount, string text) =>
        new(uploadedAt, pageCount, text, SkillFinder.Find(text).Select(skill => skill.Name).ToList());
}
