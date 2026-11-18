namespace Heardback.Modules.Applications.Responses;

/// <summary>"8 applications · 2 with no response" and the sidebar count.</summary>
internal sealed record ApplicationSummaryResponse(int Total, int NoResponse);
