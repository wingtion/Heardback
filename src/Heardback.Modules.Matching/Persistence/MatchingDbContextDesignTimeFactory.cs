using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heardback.Modules.Matching.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without the API's configuration or a running database.</summary>
internal sealed class MatchingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MatchingDbContext>
{
    // Never opened: generating migrations only needs the model.
    private const string DesignTimeConnectionString = "Host=localhost;Database=heardback_design_time";

    public MatchingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MatchingDbContext>();
        options.UseModuleDatabase(DesignTimeConnectionString, MatchingDbContext.Schema);

        return new MatchingDbContext(options.Options, AnonymousCurrentUser.Instance);
    }
}
