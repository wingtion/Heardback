using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Heardback.Infrastructure.Llm.Structured;

internal sealed partial class StructuredLlm(ILlmClient client, ILogger<StructuredLlm> logger) : IStructuredLlm
{
    /// <summary>The first answer plus one retry (PLAN.md section 5).</summary>
    private const int MaxAttempts = 2;

    private const string NotValidJson = "not_valid_json";

    public bool IsEnabled => client.IsEnabled;

    public async Task<LlmStructuredResult<TOutput>> RunAsync<TInput, TOutput>(
        ILlmPrompt<TInput, TOutput> prompt,
        TInput input,
        CancellationToken cancellationToken)
        where TOutput : class
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var request = new LlmRequest(
            prompt.Id,
            prompt.Version,
            prompt.DataClass,
            prompt.Instructions,
            prompt.BuildInput(input),
            SchemaName(prompt.Id, prompt.Version),
            LlmJson.SchemaFor(typeof(TOutput)),
            prompt.MaxOutputTokens);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var result = await client.CompleteAsync(request, cancellationToken);
            if (!result.IsSuccess)
            {
                return LlmStructuredResult<TOutput>.Failure(result.Outcome, result.RetryAfter);
            }

            var completion = result.Completion;
            var problems = LlmJson.TryRead<TOutput>(completion.Content, out var value)
                ? prompt.FindProblems(value!)
                : [NotValidJson];
            if (problems.Count == 0)
            {
                return LlmStructuredResult<TOutput>.Success(value!, completion);
            }

            LogInvalidAnswer(
                prompt.Id, prompt.Version, completion.Provider, completion.Model, attempt, string.Join(", ", problems));
        }

        return LlmStructuredResult<TOutput>.Failure(LlmOutcome.InvalidOutput);
    }

    /// <summary>Providers allow letters, digits, <c>_</c> and <c>-</c> in schema names.</summary>
    internal static string SchemaName(string promptId, int version) =>
        $"{UnsafeSchemaNameCharacters().Replace(promptId, "_")}_v{version}";

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex UnsafeSchemaNameCharacters();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "LLM answer for {PromptId} v{PromptVersion} from {Provider} ({Model}) was unusable on attempt "
            + "{Attempt}: {Problems}")]
    private partial void LogInvalidAnswer(
        string promptId, int promptVersion, string provider, string model, int attempt, string problems);
}
