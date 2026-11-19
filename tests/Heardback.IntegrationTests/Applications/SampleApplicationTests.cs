using System.Net;
using System.Net.Http.Headers;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

/// <summary>Sample applications with the setting on (Alice is the admin, Bob a member).</summary>
public sealed class SampleApplicationTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Applications:SampleDataEnabled"] = "true";
    }

    [Fact]
    public async Task Status_Admin_IsEnabled()
    {
        using var response = await SendAsync(HttpMethod.Get, Alice);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.ReadAsync<SampleStatusDto>()).Enabled);
    }

    [Fact]
    public async Task Add_Admin_AddsEverySampleToTheirOwnListWithTimelines()
    {
        using var response = await SendAsync(HttpMethod.Post, Alice);
        var result = await response.ReadAsync<SamplesDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, result.Added);
        Assert.Equal(0, result.Skipped);
        var page = await Client.ListApplicationsOkAsync(Alice);
        Assert.Equal(8, page.TotalCount);
        var statuses = page.Items.Select(item => item.Status).ToList();
        Assert.Contains("Offer", statuses);
        Assert.Contains("Rejected", statuses);
        Assert.Contains("Interview", statuses);
        Assert.Contains("Applied", statuses);
        Assert.Equal(0, (await Client.ListApplicationsOkAsync(Bob)).TotalCount);
    }

    [Fact]
    public async Task Add_Twice_AddsNothingTheSecondTime()
    {
        using (await SendAsync(HttpMethod.Post, Alice))
        {
        }

        using var second = await SendAsync(HttpMethod.Post, Alice);
        var result = await second.ReadAsync<SamplesDto>();

        Assert.Equal(0, result.Added);
        Assert.Equal(8, result.Skipped);
        Assert.Equal(8, (await Client.ListApplicationsOkAsync(Alice)).TotalCount);
    }

    [Fact]
    public async Task StatusAndAdd_Member_AreForbidden()
    {
        using var status = await SendAsync(HttpMethod.Get, Bob);
        using var add = await SendAsync(HttpMethod.Post, Bob);

        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);
        Assert.Equal(0, await CountRowsAsync("applications.applications"));
    }

    [Fact]
    public async Task Add_Anonymous_IsUnauthorized()
    {
        using var add = await SendAsync(HttpMethod.Post, accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, add.StatusCode);
    }

    internal async Task<HttpResponseMessage> SendAsync(HttpMethod method, string? accessToken)
    {
        using var request = new HttpRequestMessage(method, new Uri($"{ApplicationsApi.BasePath}/samples", UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await Client.SendAsync(request, CancellationToken);
    }
}

/// <summary>The default: sample applications are off, also for admins.</summary>
public sealed class SampleApplicationsOffTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task StatusAndAdd_SettingOff_SayNotEnabledAndAddNothing()
    {
        using var statusRequest = new HttpRequestMessage(HttpMethod.Get, new Uri($"{ApplicationsApi.BasePath}/samples", UriKind.Relative));
        statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Alice);
        using var status = await Client.SendAsync(statusRequest, CancellationToken);
        using var addRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"{ApplicationsApi.BasePath}/samples", UriKind.Relative));
        addRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Alice);
        using var add = await Client.SendAsync(addRequest, CancellationToken);

        Assert.False((await status.ReadAsync<SampleStatusDto>()).Enabled);
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
        Assert.Equal(0, await CountRowsAsync("applications.applications"));
    }
}

internal sealed record SampleStatusDto(bool Enabled);

internal sealed record SamplesDto(int Added, int Skipped);
