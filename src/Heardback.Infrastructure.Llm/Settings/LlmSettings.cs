using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Settings;

/// <summary>Provider settings, tried in order: Groq, then Gemini (PLAN.md, ADR-0008).</summary>
internal sealed class LlmSettings
{
    public const string SectionName = "Llm";

    [ValidateObjectMembers]
    public LlmProviderSettings Groq { get; set; } = new()
    {
        Model = "openai/gpt-oss-120b",
        BaseUrl = new Uri("https://api.groq.com/openai/v1/"),
        AllowsPersonalData = true,
        ReasoningEffort = "low",
    };

    /// <summary>Defaults to the unpaid tier's rules: no CV text (its terms allow product use and human review).</summary>
    [ValidateObjectMembers]
    public LlmProviderSettings Gemini { get; set; } = new()
    {
        Model = "gemini-3.8-flash",
        BaseUrl = new Uri("https://generativelanguage.googleapis.com/v1beta/"),
        AllowsPersonalData = false,
    };
}
