using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heardback.Infrastructure.Llm.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without the API's configuration or a running database.</summary>
internal sealed class LlmDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LlmDbContext>
{
    // Never opened: generating migrations only needs the model.
    private const string DesignTimeConnectionString = "Host=localhost;Database=heardback_design_time";

    public LlmDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LlmDbContext>();
        options.UseModuleDatabase(DesignTimeConnectionString, LlmDbContext.Schema);

        return new LlmDbContext(options.Options, AnonymousCurrentUser.Instance);
    }
}
