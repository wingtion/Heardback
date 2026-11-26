namespace Heardback.Shared.Demo;

/// <summary>
/// Fills a new demo account with one module's sample data (ADR-0018). Every module with something to show
/// implements it; the Identity module runs them all right after creating the account, in a scope whose current user
/// is that account, so the data is owned by it exactly like data the user added themselves.
/// </summary>
public interface IDemoDataSeeder
{
    /// <summary>Adds what a "Try the demo" visitor starts with.</summary>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>A task that completes when the data is stored.</returns>
    Task SeedVisitorAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adds what one of the permanent demo neighbors has, the made-up accounts behind the community signals demo
    /// visitors see.
    /// </summary>
    /// <param name="number">Which neighbor, from 1 to <see cref="DemoNeighbors.Count"/>.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>A task that completes when the data is stored.</returns>
    Task SeedNeighborAsync(int number, CancellationToken cancellationToken);
}
