using JobTrail.Api.RateLimiting;
using JobTrail.Modules.Applications;
using JobTrail.Modules.Identity;
using JobTrail.Modules.Insights;
using JobTrail.Modules.Matching;
using JobTrail.Modules.Postings;
using JobTrail.Shared.Http;
using JobTrail.Shared.Persistence;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDatabaseSettings();
builder.Services.AddRateLimitRejection();

// Every endpoint requires a signed-in user unless it is explicitly marked AllowAnonymous.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services
    .AddIdentityModule()
    .AddApplicationsModule()
    .AddPostingsModule()
    .AddMatchingModule()
    .AddInsightsModule();

var app = builder.Build();

await app.Services.MigrateModuleDatabasesAsync(app.Lifetime.ApplicationStopping);

// Unhandled exceptions become a 500 ProblemDetails without internals; empty error
// responses (401, 403, 404, 405, 429, ...) also get a ProblemDetails body.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Rate limits are partitioned by Connection.RemoteIpAddress. Behind a proxy (Fly.io, Netlify) that is the
// proxy's address until forwarded headers are configured for the real hosting setup (Phase 8, ADR-0002).
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();

// All module endpoints hang off one root group so request validation covers every one of them.
var api = app.MapGroup(string.Empty).WithRequestValidation();
api.MapIdentityEndpoints();

await app.RunAsync();
