using System.Reflection;
using Heardback.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Heardback.UnitTests.Architecture;

/// <summary>
/// Guards the per-user isolation rules for every module DbContext, so a new entity cannot silently
/// skip the ownership filter.
/// </summary>
public sealed class UserOwnershipConventionTests
{
    private const string UserIdProperty = nameof(IUserOwned.UserId);

    private static readonly IReadOnlyList<Type> ModuleContextTypes = ModuleCatalog.LoadFromTestOutput()
        .SelectMany(module => module.Load().GetTypes())
        .Where(type => type is { IsAbstract: false } && type.IsSubclassOf(typeof(ModuleDbContext)))
        .ToList();

    [Fact]
    public void ModuleDbContexts_EveryOne_HasDesignTimeFactory()
    {
        var withoutFactory = ModuleContextTypes.Where(type => FindDesignTimeFactory(type) is null).ToList();

        Assert.Empty(withoutFactory);
    }

    [Fact]
    public void EntitiesWithUserId_EveryModule_AreUserOwnedOrExplicitlyExempt()
    {
        var violations = AllEntityTypes()
            .Where(entity => entity.FindProperty(UserIdProperty) is not null
                && !typeof(IUserOwned).IsAssignableFrom(entity.ClrType)
                && entity.ClrType.GetCustomAttribute<NotUserOwnedAttribute>() is null)
            .Select(entity => entity.ClrType.FullName)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void UserOwnedEntities_EveryModule_HaveOwnershipQueryFilter()
    {
        var unfiltered = AllEntityTypes()
            .Where(entity => typeof(IUserOwned).IsAssignableFrom(entity.ClrType)
                && entity.BaseType is null
                && entity.FindDeclaredQueryFilter(ModuleDbContext.UserOwnershipFilter) is null)
            .Select(entity => entity.ClrType.FullName)
            .ToList();

        Assert.Empty(unfiltered);
    }

    private static IEnumerable<IEntityType> AllEntityTypes()
    {
        foreach (var contextType in ModuleContextTypes)
        {
            using var context = CreateContext(contextType);
            foreach (var entityType in context.Model.GetEntityTypes())
            {
                yield return entityType;
            }
        }
    }

    private static DbContext CreateContext(Type contextType)
    {
        var factoryType = FindDesignTimeFactory(contextType)
            ?? throw new InvalidOperationException($"{contextType.FullName} has no design-time factory.");
        var factory = Activator.CreateInstance(factoryType, nonPublic: true)!;
        var create = factoryType.GetMethod(nameof(IDesignTimeDbContextFactory<DbContext>.CreateDbContext))!;

        return (DbContext)create.Invoke(factory, [Array.Empty<string>()])!;
    }

    private static Type? FindDesignTimeFactory(Type contextType)
    {
        var factoryInterface = typeof(IDesignTimeDbContextFactory<>).MakeGenericType(contextType);
        return contextType.Assembly.GetTypes().SingleOrDefault(type => type.IsAssignableTo(factoryInterface));
    }
}
