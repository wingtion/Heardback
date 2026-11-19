namespace Heardback.Modules.Applications.Analysis;

/// <summary>What the posting-analysis prompt reads: the details the content hash was computed from.</summary>
internal sealed record PostingAnalysisInput(string Company, string Role, string PostingText);
