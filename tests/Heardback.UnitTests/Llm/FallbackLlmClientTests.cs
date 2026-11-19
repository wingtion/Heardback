using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Providers;
using Microsoft.Extensions.Logging;

namespace Heardback.UnitTests.Llm;

public sealed class FallbackLlmClientTests
{
    private readonly CapturingLogger<FallbackLlmClient> _logger = new();

    [Fact]
    public async Task CompleteAsync_NoProviderConfigured_IsDisabled()
    {
        var groq = new FakeProvider("groq", FakeProvider.AnswerFrom("groq"), isConfigured: false);
        var client = CreateClient(groq);

        var result = await client.CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.False(client.IsEnabled);
        Assert.Equal(LlmOutcome.Disabled, result.Outcome);
        Assert.Equal(0, groq.Calls);
    }

    [Fact]
    public async Task CompleteAsync_FirstProviderAnswers_DoesNotCallSecond()
    {
        var groq = new FakeProvider("groq", FakeProvider.AnswerFrom("groq"));
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"));

        var result = await CreateClient(groq, gemini)
            .CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("groq", result.Completion.Provider);
        Assert.Equal(0, gemini.Calls);
    }

    [Fact]
    public async Task CompleteAsync_FirstProviderTransient_FallsBackToSecond()
    {
        var groq = new FakeProvider("groq", ProviderReply.Transient(ProviderHttp.HttpStatus, 429));
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"));

        var result = await CreateClient(groq, gemini)
            .CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("gemini", result.Completion.Provider);
        Assert.Equal(1, groq.Calls);
    }

    [Fact]
    public async Task CompleteAsync_FirstProviderRejects_DoesNotFallBack()
    {
        var groq = new FakeProvider("groq", ProviderReply.Rejected(ProviderHttp.HttpStatus, 400));
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"));

        var result = await CreateClient(groq, gemini)
            .CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(LlmOutcome.Rejected, result.Outcome);
        Assert.Equal(0, gemini.Calls);
    }

    [Fact]
    public async Task CompleteAsync_AllProvidersTransient_IsUnavailableWithEarliestRetryAfter()
    {
        var groq = new FakeProvider(
            "groq", ProviderReply.Transient(ProviderHttp.HttpStatus, 429, TimeSpan.FromSeconds(30)));
        var gemini = new FakeProvider(
            "gemini", ProviderReply.Transient(ProviderHttp.HttpStatus, 429, TimeSpan.FromSeconds(5)));

        var result = await CreateClient(groq, gemini)
            .CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal(LlmOutcome.Unavailable, result.Outcome);
        Assert.Equal(TimeSpan.FromSeconds(5), result.RetryAfter);
    }

    [Fact]
    public async Task CompleteAsync_PersonalData_SkipsProviderThatMayNotReceiveIt()
    {
        var groq = new FakeProvider("groq", ProviderReply.Transient(ProviderHttp.Timeout));
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"), allowsPersonalData: false);

        var result = await CreateClient(groq, gemini)
            .CompleteAsync(LlmTestData.Request(LlmDataClass.Personal), TestContext.Current.CancellationToken);

        Assert.Equal(LlmOutcome.Unavailable, result.Outcome);
        Assert.Equal(0, gemini.Calls);
    }

    [Fact]
    public async Task CompleteAsync_PersonalDataAndNoProviderMayReceiveIt_IsNoEligibleProvider()
    {
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"), allowsPersonalData: false);

        var result = await CreateClient(gemini)
            .CompleteAsync(LlmTestData.Request(LlmDataClass.Personal), TestContext.Current.CancellationToken);

        Assert.Equal(LlmOutcome.NoEligibleProvider, result.Outcome);
        Assert.Equal(0, gemini.Calls);
    }

    [Fact]
    public async Task CompleteAsync_UnconfiguredProvider_IsSkipped()
    {
        var groq = new FakeProvider("groq", FakeProvider.AnswerFrom("groq"), isConfigured: false);
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"));

        var result = await CreateClient(groq, gemini)
            .CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Equal("gemini", result.Completion!.Provider);
        Assert.Equal(0, groq.Calls);
    }

    [Fact]
    public async Task CompleteAsync_EveryCall_LogsProviderModelPromptAndTokensButNoPromptText()
    {
        var groq = new FakeProvider("groq", ProviderReply.Transient(ProviderHttp.HttpStatus, 503));
        var gemini = new FakeProvider("gemini", FakeProvider.AnswerFrom("gemini"));

        await CreateClient(groq, gemini).CompleteAsync(LlmTestData.Request(), TestContext.Current.CancellationToken);

        Assert.Collection(
            _logger.Entries,
            failed =>
            {
                Assert.Equal(LogLevel.Warning, failed.Level);
                Assert.Contains("groq (groq-model) for posting-analysis v1 failed", failed.Message, StringComparison.Ordinal);
                Assert.Contains("status 503", failed.Message, StringComparison.Ordinal);
            },
            answered =>
            {
                Assert.Equal(LogLevel.Information, answered.Level);
                Assert.Contains("gemini (gemini-model) for posting-analysis v1 answered", answered.Message, StringComparison.Ordinal);
                Assert.Contains("tokens in 10 out 5", answered.Message, StringComparison.Ordinal);
            });
        Assert.All(_logger.Entries, entry =>
        {
            Assert.DoesNotContain("SECRET", entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("title", entry.Message, StringComparison.Ordinal);
        });
    }

    private FallbackLlmClient CreateClient(params ILlmProvider[] providers) =>
        new(providers, TimeProvider.System, _logger);
}
