namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// The model's answer for <see cref="TailoringPrompt"/>. The JSON schema sent to the provider is generated from
/// this record and <see cref="TailoringAdvice"/>, so changing them means a new prompt version.
/// </summary>
internal sealed record TailoringAnswer(string Summary, IReadOnlyList<TailoringAdvice> Suggestions);
