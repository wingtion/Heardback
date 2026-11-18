using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heardback.Modules.Postings.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without the API's configuration or a running database.</summary>
internal sealed class PostingsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PostingsDbContext>
{
    // Never opened: generating migrations only needs the model.
    private const string DesignTimeConnectionString = "Host=localhost;Database=heardback_design_time";

    public PostingsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostingsDbContext>();
        options.UseModuleDatabase(DesignTimeConnectionString, PostingsDbContext.Schema);

        return new PostingsDbContext(options.Options, AnonymousCurrentUser.Instance);
    }
}
