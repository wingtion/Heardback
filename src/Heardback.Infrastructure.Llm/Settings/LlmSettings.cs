using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Settings;

/// <summary>Provider settings, tried in order: Groq, then Gemini (PLAN.md, ADR-0008).</summary>
internal sealed class LlmSettings
{
    public const string SectionName = "Llm";

    /// <summary>
    /// App-wide cap on LLM calls per UTC day, kept below the providers' free daily limits (Groq: about 1,000
    /// requests per model). When reached, jobs wait until midnight UTC.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Range(1, 1_000_000)]
    public int MaxCallsPerDay { get; set; } = 800;

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
        Model = "gemini-3.1-flash-lite",
        BaseUrl = new Uri("https://generativelanguage.googleapis.com/v1beta/"),
        AllowsPersonalData = false,
    };
}
