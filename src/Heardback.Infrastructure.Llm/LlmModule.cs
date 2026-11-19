using Heardback.Infrastructure.Llm.Providers;
using Heardback.Infrastructure.Llm.Providers.Gemini;
using Heardback.Infrastructure.Llm.Providers.Groq;
using Heardback.Infrastructure.Llm.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm;

/// <summary>Registration entry point for the LLM client (ADR-0008).</summary>
public static class LlmModule
{
    /// <summary>
    /// Registers <see cref="ILlmClient"/> with Groq first and Gemini second. A provider without an API key is
    /// skipped; with no key at all, <see cref="ILlmClient.IsEnabled"/> is false and the app runs without LLM.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddLlm(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<LlmSettings>()
            .BindConfiguration(LlmSettings.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<LlmSettings>, LlmSettingsValidator>();

        AddProvider<GroqProvider>(services, settings => settings.Groq);
        AddProvider<GeminiProvider>(services, settings => settings.Gemini);

        services.AddTransient<ILlmClient, FallbackLlmClient>();

        return services;
    }

    // Registration order is fallback order.
    private static void AddProvider<TProvider>(
        IServiceCollection services,
        Func<LlmSettings, LlmProviderSettings> select)
        where TProvider : class, ILlmProvider
    {
        services.AddHttpClient<TProvider>((serviceProvider, client) =>
        {
            var settings = select(serviceProvider.GetRequiredService<IOptions<LlmSettings>>().Value);
            client.BaseAddress = settings.BaseUrl;
            client.Timeout = settings.Timeout;
            client.MaxResponseContentBufferSize = ProviderHttp.MaxResponseBytes;
        });
        services.AddTransient<ILlmProvider>(serviceProvider => serviceProvider.GetRequiredService<TProvider>());
    }
}
