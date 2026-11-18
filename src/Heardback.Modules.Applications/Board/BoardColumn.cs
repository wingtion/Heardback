using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Responses;

namespace Heardback.Modules.Applications.Board;

/// <summary>
/// One column. <see cref="Items"/> holds at most the configured number of newest cards;
/// <see cref="TotalCount"/> is always the real total, so the client can show "Showing 200 of 340".
/// </summary>
internal sealed record BoardColumn(ApplicationStatus Status, int TotalCount, IReadOnlyList<ApplicationListItem> Items);
