using System.Net.Http.Headers;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Heardback.IntegrationTests.Matching;

/// <summary>Test-side view of the CV HTTP API. DTOs are declared here so tests pin the wire format.</summary>
internal static class CvApi
{
    public const string BasePath = "/api/cv";
    public const string PdfContentType = "application/pdf";

    private const string FileField = "file";
    private const string FileName = "cv.pdf";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>A one-page PDF showing <paramref name="lines"/> top to bottom.</summary>
    public static byte[] Pdf(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        for (var index = 0; index < lines.Length; index++)
        {
            page.AddText(lines[index], 11, new PdfPoint(50, 780 - (index * 16)), font);
        }

        return builder.Build();
    }

    public static Task<HttpResponseMessage> UploadCvAsync(
        this HttpClient client,
        string? accessToken,
        byte[] file,
        string contentType = PdfContentType)
    {
        var form = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(part, FileField, FileName);
        return client.SendAsync(HttpMethod.Post, accessToken, form);
    }

    public static Task<HttpResponseMessage> UploadWithoutFileAsync(this HttpClient client, string accessToken)
    {
        var form = new MultipartFormDataContent { { new StringContent("no file"), "note" } };
        return client.SendAsync(HttpMethod.Post, accessToken, form);
    }

    public static Task<HttpResponseMessage> GetCvAsync(this HttpClient client, string accessToken) =>
        client.SendAsync(HttpMethod.Get, accessToken);

    public static Task<HttpResponseMessage> DeleteCvAsync(this HttpClient client, string accessToken) =>
        client.SendAsync(HttpMethod.Delete, accessToken);

    private static async Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        HttpMethod method,
        string? accessToken,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(BasePath, UriKind.Relative)) { Content = content };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}

internal sealed record CvDto(DateTimeOffset UploadedAt, int PageCount, string Text);
