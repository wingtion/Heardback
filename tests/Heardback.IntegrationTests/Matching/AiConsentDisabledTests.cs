using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Matching;

/// <summary>No API keys configured: the consent endpoint says AI is off, so the UI can hide it.</summary>
public sealed class AiConsentDisabledTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task GetConsent_LlmDisabled_SaysNotAvailable()
    {
        var consent = await Client.GetAiConsentAsync(Alice);

        Assert.False(consent.Available);
        Assert.False(consent.Allowed);
    }
}
