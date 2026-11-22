using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Llm;
using Heardback.IntegrationTests.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Heardback.IntegrationTests.Account;

/// <summary>Alice (admin) and Bob (member), with a canned LLM and the job worker driven by the test.</summary>
public abstract class AccountTestBase(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    protected const string PostingText = """
        We build shipment APIs with C#, .NET and PostgreSQL, deployed to Kubernetes on Azure.
        """;

    private const string PostingAnalysisAnswer = """
        {"company":"Northwind","title":"Backend Engineer","seniority":"Mid","workMode":"Remote",
         "skills":[".NET","PostgreSQL"],"location":"Istanbul"}
        """;

    private const string TailoringAnswer = """
        {"summary":"Strong .NET and PostgreSQL match.",
         "suggestions":[{"cvSection":"Skills","cvQuote":"Skills: C#","advice":"List C# before the other languages."}]}
        """;

    private readonly CannedLlmClient _llm = new()
    {
        Respond = request => CannedLlmClient.Answer(
            request.PromptId == "cv-tailoring" ? TailoringAnswer : PostingAnalysisAnswer),
    };

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Llm:Jobs:RunWorker"] = "false";
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILlmClient>(_llm);
    }

    protected async Task RunJobsAsync()
    {
        bool ran;
        do
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            ran = await scope.ServiceProvider.GetRequiredService<AnalysisJobRunner>().RunNextAsync(CancellationToken);
        }
        while (ran);
    }

    protected async Task<Guid> UserIdOfAsync(string email)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand("SELECT id FROM identity.users WHERE email = @email", connection);
        command.Parameters.AddWithValue("email", email);
        return (Guid)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    protected async Task<long> CountAsync(string sql, Guid id)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }
}
