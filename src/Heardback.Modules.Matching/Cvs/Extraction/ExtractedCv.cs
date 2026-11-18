namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>A CV's text, tidied but not yet stripped of personal details, and its page count.</summary>
internal sealed record ExtractedCv(string Text, int PageCount);
