using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Heardback.IntegrationTests.Applications;

/// <summary>Test-side view of the Applications HTTP API. DTOs are declared here so tests pin the wire format.</summary>
internal static class ApplicationsApi
{
    public const string BasePath = "/api/applications";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> CreateApplicationAsync(
        this HttpClient client,
        string accessToken,
        object body) =>
        client.SendAuthorizedAsync(HttpMethod.Post, BasePath, accessToken, body);

    public static async Task<ApplicationDto> CreateApplicationOkAsync(
        this HttpClient client,
        string accessToken,
        object body)
    {
        using var response = await client.CreateApplicationAsync(accessToken, body);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ApplicationDto>();
    }

    public static Task<HttpResponseMessage> GetApplicationAsync(this HttpClient client, string accessToken, Guid id) =>
        client.SendAuthorizedAsync(HttpMethod.Get, $"{BasePath}/{id}", accessToken);

    public static Task<HttpResponseMessage> ListApplicationsAsync(
        this HttpClient client,
        string accessToken,
        string query = "") =>
        client.SendAuthorizedAsync(HttpMethod.Get, BasePath + query, accessToken);

    public static async Task<ApplicationPageDto> ListApplicationsOkAsync(
        this HttpClient client,
        string accessToken,
        string query = "")
    {
        using var response = await client.ListApplicationsAsync(accessToken, query);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<ApplicationPageDto>();
    }

    public static async Task<SummaryDto> GetSummaryAsync(this HttpClient client, string accessToken)
    {
        using var response = await client.SendAuthorizedAsync(HttpMethod.Get, $"{BasePath}/summary", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<SummaryDto>();
    }

    public static Task<HttpResponseMessage> FindTrackedAsync(this HttpClient client, string accessToken, string url) =>
        client.SendAuthorizedAsync(
            HttpMethod.Get, $"{BasePath}/tracked?url={Uri.EscapeDataString(url)}", accessToken);

    public static Task<HttpResponseMessage> DeleteApplicationAsync(this HttpClient client, string accessToken, Guid id) =>
        client.SendAuthorizedAsync(HttpMethod.Delete, $"{BasePath}/{id}", accessToken);

    public static Task<HttpResponseMessage> UpdateApplicationAsync(
        this HttpClient client,
        string accessToken,
        Guid id,
        object body) =>
        client.SendAuthorizedAsync(HttpMethod.Put, $"{BasePath}/{id}", accessToken, body);

    public static Task<HttpResponseMessage> AddEventAsync(
        this HttpClient client,
        string accessToken,
        Guid id,
        object body) =>
        client.SendAuthorizedAsync(HttpMethod.Post, $"{BasePath}/{id}/events", accessToken, body);

    public static async Task<BoardDto> GetBoardOkAsync(this HttpClient client, string accessToken, string query = "")
    {
        using var response = await client.SendAuthorizedAsync(HttpMethod.Get, $"{BasePath}/board{query}", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<BoardDto>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(CancellationToken)
        ?? throw new InvalidOperationException($"Empty {typeof(T).Name} response.");

    private static async Task<HttpResponseMessage> SendAuthorizedAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative))
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request, CancellationToken);
    }
}

internal sealed record ApplicationDto(
    Guid Id,
    string Company,
    string Role,
    string? Url,
    string? PostingText,
    string Channel,
    DateOnly AppliedOn,
    string Status,
    int? NoResponseDays,
    string? Notes,
    DateTimeOffset UpdatedAt,
    uint Version,
    List<ApplicationEventDto> Events);

internal sealed record ApplicationEventDto(Guid Id, string? Status, DateOnly OccurredOn, string? Note, bool IsSubmission);

internal sealed record ApplicationListItemDto(
    Guid Id,
    string Company,
    string Role,
    string Channel,
    DateOnly AppliedOn,
    string Status,
    int? NoResponseDays,
    DateTimeOffset UpdatedAt);

internal sealed record ApplicationPageDto(
    List<ApplicationListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

internal sealed record SummaryDto(int Total, int NoResponse);

internal sealed record TrackedDto(Guid Id, string Company, string Role, string Status, int? NoResponseDays, DateOnly AppliedOn);

internal sealed record BoardDto(List<BoardColumnDto> Columns);

internal sealed record BoardColumnDto(string Status, int TotalCount, List<ApplicationListItemDto> Items);

internal sealed record ProblemDto(int Status, string? Code, string? Detail, Dictionary<string, string[]>? Errors);
