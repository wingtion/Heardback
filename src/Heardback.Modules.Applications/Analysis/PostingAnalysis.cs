using Heardback.Infrastructure.Llm;

namespace Heardback.Modules.Applications.Analysis;

/// <summary>
/// Structured fields an LLM read from posting text, stored once per content hash and shared by every application
/// with the same company, role and text (ADR-0008). Holds no user id: a user only ever reaches it through the
/// content hash of their own application, so nobody sees an analysis of text other than their own.
/// </summary>
internal sealed class PostingAnalysis
{
    private PostingAnalysis()
    {
    }

    /// <summary>The applications' content hash this analysis belongs to; primary key.</summary>
    public string ContentHash { get; private set; } = string.Empty;

    public int PromptVersion { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string? Company { get; private set; }

    public string? Title { get; private set; }

    public PostingSeniority Seniority { get; private set; }

    public PostingWorkMode WorkMode { get; private set; }

    public List<string> Skills { get; private set; } = [];

    public string? Location { get; private set; }

    public DateTimeOffset AnalyzedAt { get; private set; }

    public static PostingAnalysis Create(string contentHash, PostingAnalysisAnswer answer, LlmCompletion completion, DateTimeOffset now)
    {
        var analysis = new PostingAnalysis { ContentHash = contentHash };
        analysis.Replace(answer, completion, now);
        return analysis;
    }

    /// <summary>Overwrites an analysis made with an older prompt version.</summary>
    public void Replace(PostingAnalysisAnswer answer, LlmCompletion completion, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(completion);

        PromptVersion = PostingAnalysisPrompt.CurrentVersion;
        Provider = completion.Provider;
        Model = completion.Model;
        Company = answer.Company?.Trim();
        Title = answer.Title?.Trim();
        Seniority = answer.Seniority;
        WorkMode = answer.WorkMode;
        Skills = [.. answer.Skills.Select(skill => skill.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)];
        Location = answer.Location?.Trim();
        AnalyzedAt = now;
    }
}
