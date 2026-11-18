namespace Heardback.Modules.Identity.Invites;

/// <summary>
/// One page of invite codes, plus how many codes in total can still be used. The page fields match
/// <see cref="Heardback.Shared.Paging.PagedResponse{T}"/> so clients read every list the same way.
/// </summary>
internal sealed record InviteCodeListResponse(
    IReadOnlyList<InviteCodeResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int ActiveCount);
