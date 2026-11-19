using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Structured;

namespace Heardback.UnitTests.Llm;

public sealed class StructuredLlmTests
{
    private const string ValidAnswer =
        """{"title":"Senior .NET developer","seniority":"Senior","skills":[".NET","PostgreSQL"],"location":null}""";

    private readonly CapturingLogger<StructuredLlm> _logger = new();

    [Fact]
    public async Task RunAsync_ValidAnswer_ReturnsParsedValueAfterOneCall()
    {
        var client = new ScriptedLlmClient(ScriptedLlmClient.Answer(ValidAnswer));

        var result = await Run(client);

        Assert.True(result.IsSuccess);
        Assert.Equal("Senior .NET developer", result.Value.Title);
        Assert.Equal(TestSeniority.Senior, result.Value.Seniority);
        Assert.Equal([".NET", "PostgreSQL"], result.Value.Skills);
        Assert.Null(result.Value.Location);
        Assert.Equal("groq", result.Completion.Provider);
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task RunAsync_Request_CarriesPromptIdentityInputAndGeneratedSchema()
    {
        var client = new ScriptedLlmClient(ScriptedLlmClient.Answer(ValidAnswer));

        await Run(client, "Senior .NET developer, Istanbul");

        var request = Assert.Single(client.Requests);
        Assert.Equal("test-prompt", request.PromptId);
        Assert.Equal(2, request.PromptVersion);
        Assert.Equal(LlmDataClass.Public, request.DataClass);
        Assert.Equal(256, request.MaxOutputTokens);
        Assert.Equal("test-prompt_v2", request.SchemaName);
        Assert.Equal("<posting>\nSenior .NET developer, Istanbul\n</posting>", request.Input);
        Assert.Equal(new TestPrompt().Instructions, request.Instructions);
        Assert.True(System.Text.Json.JsonElement.DeepEquals(LlmJson.SchemaFor(typeof(TestAnswer)), request.ResponseSchema));
    }

    [Fact]
    public async Task RunAsync_InvalidThenValid_RetriesOnceAndSucceeds()
    {
        var client = new ScriptedLlmClient(
            ScriptedLlmClient.Answer("not json"),
            ScriptedLlmClient.Answer(ValidAnswer));

        var result = await Run(client);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, client.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_InvalidTwice_IsInvalidOutputWithoutThirdCall()
    {
        var client = new ScriptedLlmClient(
            ScriptedLlmClient.Answer("not json"),
            ScriptedLlmClient.Answer("""{"title":"x"}"""),
            ScriptedLlmClient.Answer(ValidAnswer));

        var result = await Run(client);

        Assert.Equal(LlmOutcome.InvalidOutput, result.Outcome);
        Assert.Null(result.Value);
        Assert.Equal(2, client.Requests.Count);
    }

    [Fact]
    public async Task RunAsync_AnswerBreaksPromptRules_IsRetriedAndThenInvalid()
    {
        const string tooManySkills = """{"title":"Dev","seniority":"Junior","skills":["a","b","c","d"],"location":null}""";
        var client = new ScriptedLlmClient(
            ScriptedLlmClient.Answer(tooManySkills),
            ScriptedLlmClient.Answer(tooManySkills));

        var result = await Run(client);

        Assert.Equal(LlmOutcome.InvalidOutput, result.Outcome);
        Assert.Equal(2, _logger.Entries.Count);
        Assert.All(_logger.Entries, entry => Assert.Contains("skills.too_many", entry.Message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(LlmOutcome.Disabled)]
    [InlineData(LlmOutcome.NoEligibleProvider)]
    [InlineData(LlmOutcome.Rejected)]
    public async Task RunAsync_ClientFailure_IsPassedThroughWithoutRetry(LlmOutcome outcome)
    {
        var client = new ScriptedLlmClient(LlmResult.Failure(outcome));

        var result = await Run(client);

        Assert.Equal(outcome, result.Outcome);
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task RunAsync_ProvidersUnavailable_PassesRetryAfterThrough()
    {
        var client = new ScriptedLlmClient(LlmResult.Unavailable(TimeSpan.FromSeconds(12)));

        var result = await Run(client);

        Assert.Equal(LlmOutcome.Unavailable, result.Outcome);
        Assert.Equal(TimeSpan.FromSeconds(12), result.RetryAfter);
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task RunAsync_InvalidAnswer_LogsProblemCodesButNotAnswerText()
    {
        const string secretAnswer = """{"title":"SECRET-ANSWER","seniority":"Senior","skills":[],"extra":"SECRET"}""";
        var client = new ScriptedLlmClient(
            ScriptedLlmClient.Answer(secretAnswer),
            ScriptedLlmClient.Answer(secretAnswer));

        await Run(client);

        Assert.Equal(2, _logger.Entries.Count);
        Assert.All(_logger.Entries, entry =>
        {
            Assert.Contains("test-prompt v2 from groq (groq-model)", entry.Message, StringComparison.Ordinal);
            Assert.Contains("not_valid_json", entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("SECRET", entry.Message, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("posting-analysis", 1, "posting-analysis_v1")]
    [InlineData("cv tailoring/advice", 3, "cv_tailoring_advice_v3")]
    public void SchemaName_PromptId_KeepsOnlyAllowedCharacters(string promptId, int version, string expected)
    {
        Assert.Equal(expected, StructuredLlm.SchemaName(promptId, version));
    }

    private Task<LlmStructuredResult<TestAnswer>> Run(ScriptedLlmClient client, string input = "posting text") =>
        new StructuredLlm(client, _logger).RunAsync(new TestPrompt(), input, TestContext.Current.CancellationToken);
}
