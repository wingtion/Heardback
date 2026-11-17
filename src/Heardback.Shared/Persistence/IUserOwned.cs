namespace Heardback.Shared.Persistence;

/// <summary>
/// Marks an entity that belongs to exactly one user. <see cref="ModuleDbContext"/> hides other users' rows from
/// every query and stamps <see cref="UserId"/> on insert, so entities should not set it themselves.
/// </summary>
public interface IUserOwned
{
    Guid UserId { get; }
}
