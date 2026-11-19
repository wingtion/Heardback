using Heardback.Infrastructure.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.UnitTests.Llm;

public sealed class LlmModuleTests
{
    [Fact]
    public void AddLlm_NoApiKeys_ClientIsDisabled()
    {
        using var services = BuildServices([]);

        Assert.False(services.GetRequiredService<ILlmClient>().IsEnabled);
    }

    [Theory]
    [InlineData("Llm:Groq:ApiKey")]
    [InlineData("Llm:Gemini:ApiKey")]
    public void AddLlm_AnyApiKey_ClientIsEnabled(string keySetting)
    {
        using var services = BuildServices(new() { [keySetting] = "key" });

        Assert.True(services.GetRequiredService<ILlmClient>().IsEnabled);
    }

    [Theory]
    [InlineData("Llm:Groq:Model", "")]
    [InlineData("Llm:Gemini:Timeout", "00:00:01")]
    [InlineData("Llm:Groq:Timeout", "01:00:00")]
    [InlineData("Llm:Groq:ReasoningEffort", "extreme")]
    public void AddLlm_InvalidSetting_FailsValidation(string setting, string value)
    {
        using var services = BuildServices(new() { [setting] = value });

        Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<ILlmClient>());
    }

    private static ServiceProvider BuildServices(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(TimeProvider.System)
            .AddLogging()
            .AddLlm()
            .BuildServiceProvider();
    }
}
