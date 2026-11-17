using JobTrail.Modules.Applications;
using JobTrail.Modules.Identity;
using JobTrail.Modules.Insights;
using JobTrail.Modules.Matching;
using JobTrail.Modules.Postings;
using JobTrail.Shared.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDatabaseSettings();

builder.Services
    .AddIdentityModule()
    .AddApplicationsModule()
    .AddPostingsModule()
    .AddMatchingModule()
    .AddInsightsModule();

var app = builder.Build();

await app.Services.MigrateModuleDatabasesAsync(app.Lifetime.ApplicationStopping);

// Unhandled exceptions become a 500 ProblemDetails without internals; empty error
// responses (404, 405, ...) also get a ProblemDetails body.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();
