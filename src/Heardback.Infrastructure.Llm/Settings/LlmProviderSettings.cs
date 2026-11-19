using System.ComponentModel.DataAnnotations;

namespace Heardback.Infrastructure.Llm.Settings;

/// <summary>Connection settings for one provider. The API key comes from user secrets or environment variables.</summary>
internal sealed class LlmProviderSettings
{
    /// <summary>Without a key the provider is skipped. Never logged.</summary>
    public string? ApiKey { get; set; }

    [Required]
    public string Model { get; set; } = string.Empty;

    /// <summary>API root, ending with a slash so relative paths resolve below it.</summary>
    [Required]
    public Uri? BaseUrl { get; set; }

    [Range(typeof(TimeSpan), "00:00:05", "00:05:00")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether requests with CV text may go to this provider. False for any tier that may use requests to
    /// improve its products or let people read them (ADR-0008).
    /// </summary>
    public bool AllowsPersonalData { get; set; }

    /// <summary>Reasoning effort for reasoning models (Groq only); null leaves the provider's default.</summary>
    [AllowedValues(null, "low", "medium", "high")]
    public string? ReasoningEffort { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
