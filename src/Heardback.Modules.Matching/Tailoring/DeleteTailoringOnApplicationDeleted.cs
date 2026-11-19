using Heardback.Modules.Applications.Contracts;
using Heardback.Modules.Matching.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>Suggestions are made from the CV, so they go when the application they belong to is deleted.</summary>
internal sealed class DeleteTailoringOnApplicationDeleted(MatchingDbContext dbContext) : IApplicationDeletedHandler
{
    // The ownership filter limits the statement to the signed-in user's rows.
    public Task OnDeletedAsync(Guid applicationId, CancellationToken cancellationToken) =>
        dbContext.TailoringResults
            .Where(result => result.ApplicationId == applicationId)
            .ExecuteDeleteAsync(cancellationToken);
}
