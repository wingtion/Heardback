using JobTrail.Shared.Persistence;
using JobTrail.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobTrail.Modules.Identity.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without the API's configuration or a running database.</summary>
internal sealed class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    // Never opened: generating migrations only needs the model.
    private const string DesignTimeConnectionString = "Host=localhost;Database=jobtrail_design_time";

    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        options.UseModuleDatabase(DesignTimeConnectionString, IdentityDbContext.Schema);

        return new IdentityDbContext(options.Options, AnonymousCurrentUser.Instance);
    }
}
