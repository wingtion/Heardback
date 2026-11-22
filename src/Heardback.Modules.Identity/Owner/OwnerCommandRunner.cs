using System.Globalization;
using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Recovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Identity.Owner;

/// <summary>
/// Commands the owner runs on the server (for example over <c>fly ssh console</c>), never over HTTP: a reset code
/// for any user, admins included, and the list of admins (ADR-0015). They print a code rather than take a password,
/// so no password ends up in shell history.
/// </summary>
internal sealed partial class OwnerCommandRunner(
    IdentityDbContext dbContext,
    ResetCodeIssuer issuer,
    ILogger<OwnerCommandRunner> logger)
{
    public const string ResetCodeCommand = "reset-code";
    public const string ListAdminsCommand = "list-admins";

    public const int Succeeded = 0;
    public const int Failed = 1;
    public const int Usage = 2;

    public static bool IsCommand(IReadOnlyList<string> args) =>
        args.Count > 0 && args[0] is ResetCodeCommand or ListAdminsCommand;

    public async Task<int> RunAsync(IReadOnlyList<string> args, TextWriter output, CancellationToken cancellationToken)
    {
        return args switch
        {
            [ListAdminsCommand] => await ListAdminsAsync(output, cancellationToken),
            [ResetCodeCommand, var email] => await IssueResetCodeAsync(email, output, cancellationToken),
            _ => await WriteUsageAsync(output),
        };
    }

    private async Task<int> ListAdminsAsync(TextWriter output, CancellationToken cancellationToken)
    {
        var admins = await dbContext.Users.AsNoTracking()
            .Where(user => user.IsAdmin)
            .OrderBy(user => user.CreatedAt)
            .Select(user => new { user.DisplayName, user.Email })
            .ToListAsync(cancellationToken);

        foreach (var admin in admins)
        {
            await output.WriteLineAsync($"{admin.DisplayName} <{admin.Email}>");
        }

        if (admins.Count == 0)
        {
            await output.WriteLineAsync("There are no admins.");
        }

        return Succeeded;
    }

    private async Task<int> IssueResetCodeAsync(string email, TextWriter output, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var userId = await dbContext.Users.AsNoTracking()
            .Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is null)
        {
            await output.WriteLineAsync("No account has this email.");
            return Failed;
        }

        var code = await issuer.IssueAsync(userId.Value, issuedByUserId: null, cancellationToken);
        LogIssued(userId.Value);

        var expires = code.ExpiresAt.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
        await output.WriteLineAsync($"Reset code: {code.Code}");
        await output.WriteLineAsync($"Works once, until {expires}. Enter it on the sign-in page under \"Forgot your password?\".");
        return Succeeded;
    }

    private static async Task<int> WriteUsageAsync(TextWriter output)
    {
        await output.WriteLineAsync($"Usage: Heardback.Api {ResetCodeCommand} <email> | Heardback.Api {ListAdminsCommand}");
        return Usage;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Owner issued a reset code for user {UserId}")]
    private partial void LogIssued(Guid userId);
}
