using Heardback.Api;
using Heardback.Api.ClientIp;
using Heardback.Api.RateLimiting;
using Heardback.Infrastructure.Llm;
using Heardback.Modules.Applications;
using Heardback.Modules.Identity;
using Heardback.Modules.Insights;
using Heardback.Modules.Matching;
using Heardback.Modules.Postings;
using Heardback.Shared.Http;
using Heardback.Shared.Persistence;
using Microsoft.AspNetCore.Authorization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDatabaseSettings();
builder.Services.AddClientIpResolution();
builder.Services.AddRateLimitRejection();
builder.Services.AddLlm();

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

// "reset-code <email>" or "list-admins" runs an owner command and exits; the web server never starts (ADR-0015).
if (await app.Services.TryRunOwnerCommandAsync(args, Console.Out, CancellationToken.None) is { } exitCode)
{
    return exitCode;
}

await app.Services.MigrateModuleDatabasesAsync(app.Lifetime.ApplicationStopping);

// Unhandled exceptions become a 500 ProblemDetails without internals; empty error
// responses (401, 403, 404, 405, 429, ...) also get a ProblemDetails body.
app.UseExceptionHandler();
app.UseStatusCodePages();

// The real client IP before anything else reads it: per-IP rate limits depend on it (ClientIp settings,
// ADR-0002). Behind Netlify this also refuses requests that did not come through the signed proxy.
app.UseClientIpResolution();

app.UseAuthentication();
app.UseAuthorization();

// After authentication, so per-user limits know the user; after authorization, so anonymous calls to
// protected endpoints are turned away without using up anyone's limit. Auth endpoints are anonymous and
// limited per IP, which works at either position.
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks(HealthEndpoint.Path).AllowAnonymous();

// All module endpoints hang off one root group so request validation covers every one of them.
var api = app.MapGroup(string.Empty).WithRequestValidation();
api.MapIdentityEndpoints();
api.MapApplicationsEndpoints();
api.MapMatchingEndpoints();
api.MapInsightsEndpoints();

await app.RunAsync();
return 0;
