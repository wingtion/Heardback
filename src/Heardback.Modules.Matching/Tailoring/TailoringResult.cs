using Heardback.Infrastructure.Llm;
using Heardback.Shared.Persistence;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// The latest tailoring suggestions for one of the user's applications. Made from the CV, so it is deleted with
/// the CV and when the user withdraws consent.
/// </summary>
internal sealed class TailoringResult : IUserOwned
{
    private TailoringResult()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ApplicationId { get; private set; }

    public string CvFingerprint { get; private set; } = string.Empty;

    public string PostingFingerprint { get; private set; } = string.Empty;

    public int PromptVersion { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public List<TailoringSuggestion> Suggestions { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public static TailoringResult Create(Guid applicationId, DateTimeOffset now) =>
        new() { Id = Guid.CreateVersion7(now), ApplicationId = applicationId };

    public void Replace(
        TailoringAnswer answer,
        LlmCompletion completion,
        string cvFingerprint,
        string postingFingerprint,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(completion);

        CvFingerprint = cvFingerprint;
        PostingFingerprint = postingFingerprint;
        PromptVersion = TailoringPrompt.CurrentVersion;
        Provider = completion.Provider;
        Model = completion.Model;
        Summary = answer.Summary.Trim();
        Suggestions = [.. answer.Suggestions.Select(suggestion =>
            new TailoringSuggestion(suggestion.CvSection.Trim(), suggestion.CvQuote.Trim(), suggestion.Advice.Trim()))];
        CreatedAt = now;
    }
}
