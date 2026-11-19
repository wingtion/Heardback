namespace Heardback.Modules.Applications.Analysis;

/// <param name="ContentHash">The hash the analysis is stored under; null when the application has no posting text.</param>
internal sealed record AnalysedApplication(string? ContentHash);
