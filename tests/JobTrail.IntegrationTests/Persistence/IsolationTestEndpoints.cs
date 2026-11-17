using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.IntegrationTests.Persistence;

/// <summary>
/// Test-only endpoints over <see cref="IsolationTestDbContext"/>, appended after the real pipeline. They run
/// behind the real authentication, fallback authorization and ICurrentUser, so they prove per-user isolation
/// end to end until a feature module stores user data (Phase 2).
/// </summary>
internal sealed class IsolationTestEndpoints : IStartupFilter
{
    public const string Path = "/test/notes";

    public static void Register(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IsolationTestDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IStartupFilter, IsolationTestEndpoints>();
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            next(app);
            app.Map(Path, branch => branch.Run(HandleAsync));
        };

    private static async Task HandleAsync(HttpContext context)
    {
        var dbContext = context.RequestServices.GetRequiredService<IsolationTestDbContext>();

        if (HttpMethods.IsPost(context.Request.Method))
        {
            dbContext.Notes.Add(new TestNote(context.Request.Query["text"].ToString()));
            await dbContext.SaveChangesAsync(context.RequestAborted);
            context.Response.StatusCode = (int)HttpStatusCode.Created;
            return;
        }

        var texts = await dbContext.Notes.OrderBy(note => note.Text).Select(note => note.Text).ToListAsync(context.RequestAborted);
        await context.Response.WriteAsJsonAsync(texts, context.RequestAborted);
    }
}
