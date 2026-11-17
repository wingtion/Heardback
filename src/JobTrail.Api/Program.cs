using JobTrail.Modules.Applications;
using JobTrail.Modules.Identity;
using JobTrail.Modules.Insights;
using JobTrail.Modules.Matching;
using JobTrail.Modules.Postings;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityModule()
    .AddApplicationsModule()
    .AddPostingsModule()
    .AddMatchingModule()
    .AddInsightsModule();

var app = builder.Build();

await app.RunAsync();
