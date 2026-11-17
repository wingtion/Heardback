using Heardback.Modules.Identity.Persistence;
using Heardback.Shared.Results;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Identity.CurrentUser;

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
