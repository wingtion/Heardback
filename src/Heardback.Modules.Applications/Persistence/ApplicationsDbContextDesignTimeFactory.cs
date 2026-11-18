using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heardback.Modules.Applications.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without the API's configuration or a running database.</summary>
internal sealed class ApplicationsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationsDbContext>
{
    // Never opened: generating migrations only needs the model.
    private const string DesignTimeConnectionString = "Host=localhost;Database=heardback_design_time";

    public ApplicationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationsDbContext>();
        options.UseModuleDatabase(DesignTimeConnectionString, ApplicationsDbContext.Schema);

        return new ApplicationsDbContext(options.Options, AnonymousCurrentUser.Instance);
    }
}
