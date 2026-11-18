namespace Heardback.Modules.Matching.Cvs;

/// <summary>
/// The user's stored CV: the text exactly as kept, with personal details replaced by markers such as
/// <c>[email]</c>, so the user can check what Heardback stores.
/// </summary>
internal sealed record CvResponse(DateTimeOffset UploadedAt, int PageCount, string Text);
