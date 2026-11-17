using JobTrail.Modules.Identity.Persistence;
using JobTrail.Shared.Results;
using JobTrail.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace JobTrail.Modules.Identity.CurrentUser;

internal sealed class GetCurrentUserHandler(IdentityDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<Result<CurrentUserResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetRequiredUserId();

        var response = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new CurrentUserResponse(user.Id, user.Email, user.DisplayName, user.IsAdmin))
            .SingleOrDefaultAsync(cancellationToken);

        return response is null ? IdentityErrors.UserNotFound : response;
    }
}
