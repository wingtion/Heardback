using System.Text.Json;
using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Settings;
using Microsoft.Extensions.Options;

namespace Heardback.UnitTests.Llm;

internal static class LlmTestData
{
    public const string Instructions = "SECRET-INSTRUCTIONS Extract the posting fields.";
    public const string Input = "SECRET-INPUT Senior .NET developer, remote.";
    public const string GroqKey = "groq-test-key";
    public const string GeminiKey = "gemini-test-key";

    public static readonly JsonElement Schema = JsonDocument.Parse(
        """{"type":"object","properties":{"title":{"type":"string"}},"required":["title"],"additionalProperties":false}""")
        .RootElement;

    public static LlmRequest Request(LlmDataClass dataClass = LlmDataClass.Public) =>
        new("posting-analysis", 1, dataClass, Instructions, Input, "posting_analysis", Schema, 512);

    public static IOptions<LlmSettings> Settings()
    {
        var settings = new LlmSettings();
        settings.Groq.ApiKey = GroqKey;
        settings.Gemini.ApiKey = GeminiKey;
        return Options.Create(settings);
    }

    public static HttpClient Client(HttpMessageHandler handler, string baseUrl) =>
        new(handler) { BaseAddress = new Uri(baseUrl) };
}
