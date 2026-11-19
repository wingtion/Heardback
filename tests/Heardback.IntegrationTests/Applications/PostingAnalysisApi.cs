using System.Net.Http.Headers;

namespace Heardback.IntegrationTests.Applications;

/// <summary>Test-side view of the posting-analysis endpoints. DTOs are declared here so tests pin the wire format.</summary>
internal static class PostingAnalysisApi
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> GetPostingAnalysisAsync(this HttpClient client, string? accessToken, Guid id) =>
        client.SendAsync(HttpMethod.Get, id, accessToken);

    public static Task<HttpResponseMessage> RequestPostingAnalysisAsync(
        this HttpClient client,
        string? accessToken,
        Guid id) =>
        client.SendAsync(HttpMethod.Post, id, accessToken);

    public static async Task<PostingAnalysisDto> GetPostingAnalysisOkAsync(this HttpClient client, string accessToken, Guid id)
    {
        using var response = await client.GetPostingAnalysisAsync(accessToken, id);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<PostingAnalysisDto>();
    }

    public static async Task<PostingAnalysisDto> RequestPostingAnalysisOkAsync(
        this HttpClient client,
        string accessToken,
        Guid id)
    {
        using var response = await client.RequestPostingAnalysisAsync(accessToken, id);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<PostingAnalysisDto>();
    }

    private static async Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        HttpMethod method,
        Guid id,
        string? accessToken)
    {
        using var request = new HttpRequestMessage(
            method, new Uri($"{ApplicationsApi.BasePath}/{id}/posting-analysis", UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}

internal sealed record PostingAnalysisDto(string State, PostingAnalysisFieldsDto? Analysis, UsageDto Usage);

internal sealed record UsageDto(int Used, int Limit, int Remaining, DateTimeOffset ResetsAt);

internal sealed record PostingAnalysisFieldsDto(
    string? Company,
    string? Title,
    string Seniority,
    string WorkMode,
    string[] Skills,
    string? Location,
    DateTimeOffset AnalyzedAt);

internal sealed record QuotaProblemDto(int Status, string Code, string Detail, DateTimeOffset ResetsAt);
