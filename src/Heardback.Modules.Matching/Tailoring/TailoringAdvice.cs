namespace Heardback.Modules.Matching.Tailoring;

/// <summary>One concrete change to the CV, as the model writes it.</summary>
/// <param name="CvSection">The CV section it is about, such as "Experience" or "Skills".</param>
/// <param name="CvQuote">A few words copied from the CV line it is about; checked against the CV.</param>
/// <param name="Advice">What to change there.</param>
internal sealed record TailoringAdvice(string CvSection, string CvQuote, string Advice);
