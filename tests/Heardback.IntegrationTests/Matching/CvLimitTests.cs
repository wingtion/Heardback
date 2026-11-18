using System.Globalization;
using System.Net;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Matching;

public sealed class CvLimitTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const int UploadsPerHour = 2;
    private const int MaxBytes = 4096;

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Matching:CvUploadsPerHour"] = UploadsPerHour.ToString(CultureInfo.InvariantCulture);
        settings["Matching:CvMaxBytes"] = MaxBytes.ToString(CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task Upload_LargerThanTheLimit_Returns400WithTheLimit()
    {
        var tooLarge = CvApi.Pdf("Ada Lovelace").Concat(new byte[MaxBytes]).ToArray();

        using var upload = await Client.UploadCvAsync(Alice, tooLarge);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("matching.cv_too_large", (await upload.ReadAsync<ProblemDto>()).Code);
    }

    [Fact]
    public async Task Upload_OverTheHourlyLimit_Returns429WithRetryAfter()
    {
        for (var upload = 0; upload < UploadsPerHour; upload++)
        {
            using var accepted = await Client.UploadCvAsync(Alice, CvApi.Pdf("Ada Lovelace", "Skills: C#"));
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using var rejected = await Client.UploadCvAsync(Alice, CvApi.Pdf("Ada Lovelace", "Skills: C#"));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    [Fact]
    public async Task Upload_RejectedFiles_CountTowardsTheLimit()
    {
        for (var upload = 0; upload < UploadsPerHour; upload++)
        {
            using var wrongFile = await Client.UploadCvAsync(Alice, "not a pdf"u8.ToArray());
        }

        using var rejected = await Client.UploadCvAsync(Alice, CvApi.Pdf("Ada Lovelace"));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task Upload_OneUserOverTheLimit_DoesNotLimitAnotherUserOrReads()
    {
        for (var upload = 0; upload <= UploadsPerHour; upload++)
        {
            using var response = await Client.UploadCvAsync(Alice, CvApi.Pdf("Ada Lovelace"));
        }

        using var bobs = await Client.UploadCvAsync(Bob, CvApi.Pdf("Bob"));
        using var alicesRead = await Client.GetCvAsync(Alice);

        Assert.Equal(HttpStatusCode.OK, bobs.StatusCode);
        Assert.Equal(HttpStatusCode.OK, alicesRead.StatusCode);
    }
}
