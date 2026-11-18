namespace Heardback.Modules.Applications.Board;

/// <summary>The four columns in board order: Applied, Interview, Offer, Rejected.</summary>
internal sealed record BoardResponse(IReadOnlyList<BoardColumn> Columns);
