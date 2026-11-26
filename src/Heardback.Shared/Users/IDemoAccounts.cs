namespace Heardback.Shared.Users;

/// <summary>
/// Which accounts are demo accounts (ADR-0018). Community statistics use it so members never see demo data and demo
/// visitors never see members' data.
/// </summary>
public interface IDemoAccounts
{
    /// <summary>The ids of every demo visitor and demo neighbor that exists now.</summary>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The demo account ids, by kind.</returns>
    Task<DemoAccountIds> ListAsync(CancellationToken cancellationToken);
}
