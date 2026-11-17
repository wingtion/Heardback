using System.Linq.Expressions;
using System.Reflection;
using JobTrail.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace JobTrail.Shared.Persistence;

/// <summary>
/// Base class for every module's DbContext. Enforces per-user data isolation for <see cref="IUserOwned"/>
/// entities: queries only see the current user's rows, inserts are stamped with the current user's id,
/// and updates or deletes of another user's rows fail.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    public const string UserOwnershipFilter = "UserOwnership";

    private static readonly PropertyInfo CurrentUserIdProperty =
        typeof(ModuleDbContext).GetProperty(nameof(CurrentUserId), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly ICurrentUser _currentUser;

    protected ModuleDbContext(DbContextOptions options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // Filters read this through the context instance, so EF evaluates it per query instead of
    // baking the first user's id into the cached model.
    private Guid? CurrentUserId => _currentUser.UserId;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceUserOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceUserOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Configure the module's schema and entities here.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    // Sealed so a module cannot skip the ownership filters.
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureModel(modelBuilder);
        ApplyUserOwnershipFilters(modelBuilder);
    }

    private void ApplyUserOwnershipFilters(ModelBuilder modelBuilder)
    {
        var userOwnedRoots = modelBuilder.Model.GetEntityTypes()
            .Where(type => type.BaseType is null && typeof(IUserOwned).IsAssignableFrom(type.ClrType))
            .ToList();

        foreach (var entityType in userOwnedRoots)
        {
            var entity = Expression.Parameter(entityType.ClrType, "entity");
            var ownerId = Expression.Convert(Expression.Property(entity, nameof(IUserOwned.UserId)), typeof(Guid?));
            var currentUserId = Expression.Property(Expression.Constant(this), CurrentUserIdProperty);
            var filter = Expression.Lambda(Expression.Equal(ownerId, currentUserId), entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(UserOwnershipFilter, filter);
        }
    }

    private void EnforceUserOwnership()
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwned>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    StampOwner(entry);
                    break;
                case EntityState.Modified or EntityState.Deleted:
                    EnsureOwnedByCurrentUser(entry);
                    break;
            }
        }
    }

    private void StampOwner(EntityEntry<IUserOwned> entry)
    {
        var currentUserId = _currentUser.GetRequiredUserId();
        var owner = entry.Property<Guid>(nameof(IUserOwned.UserId));

        if (owner.CurrentValue != Guid.Empty && owner.CurrentValue != currentUserId)
        {
            throw new InvalidOperationException(
                $"Cannot create {entry.Metadata.DisplayName()} owned by a different user.");
        }

        owner.CurrentValue = currentUserId;
    }

    private void EnsureOwnedByCurrentUser(EntityEntry<IUserOwned> entry)
    {
        var currentUserId = _currentUser.GetRequiredUserId();
        var owner = entry.Property<Guid>(nameof(IUserOwned.UserId));

        if (owner.OriginalValue != currentUserId || owner.CurrentValue != currentUserId)
        {
            throw new InvalidOperationException(
                $"Cannot {entry.State.ToString().ToLowerInvariant()} {entry.Metadata.DisplayName()} owned by a different user.");
        }
    }
}
