using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Samples;

/// <summary>
/// What demo accounts get on top of <see cref="SampleApplications"/> (ADR-0018). Every company and link is made up
/// (.example domains), so nothing here can match a real posting that members track.
/// </summary>
internal static class DemoApplications
{
    /// <summary>
    /// Extra applications for a demo visitor, spread over eight weeks so the Overview charts have something to show.
    /// </summary>
    public static readonly IReadOnlyList<SampleApplication> VisitorExtras =
    [
        new(
            "Brightline Payments",
            "Backend Developer (C#)",
            "https://brightline.example/careers/backend-csharp",
            ApplicationChannel.LinkedInEasyApply,
            AppliedDaysAgo: 52,
            """
            Brightline Payments processes card payments for small shops. Backend Developer, C# and ASP.NET Core,
            PostgreSQL, RabbitMQ. Hybrid in Istanbul. 3+ years of experience.
            """,
            [new(ApplicationStatus.Rejected, DaysAgo: 40, "Automated rejection email")]),
        new(
            "Quillon Software",
            "Full Stack Developer",
            "https://quillon.example/jobs/full-stack",
            ApplicationChannel.CompanySite,
            AppliedDaysAgo: 46,
            """
            Quillon builds document workflow tools. Full Stack Developer with Angular and .NET, Azure DevOps and SQL
            Server. Remote in Turkey.
            """,
            []),
        new(
            "Meridian Retail",
            "Software Engineer",
            "https://meridian-retail.example/careers/software-engineer",
            ApplicationChannel.Referral,
            AppliedDaysAgo: 38,
            """
            Meridian Retail runs 300 stores. Software Engineer for the e-commerce team: TypeScript, Node.js, React,
            AWS. Referral bonus programme.
            """,
            [
                new(ApplicationStatus.Interview, DaysAgo: 33, "First interview with the hiring manager"),
                new(ApplicationStatus.Rejected, DaysAgo: 24, "Went with a more senior candidate"),
            ]),
        new(
            "Solstice Energy",
            "Platform Engineer",
            "https://solstice-energy.example/jobs/platform",
            ApplicationChannel.LinkedInEasyApply,
            AppliedDaysAgo: 31,
            """
            Solstice Energy is hiring a Platform Engineer: Kubernetes, Terraform, GitHub Actions, observability with
            OpenTelemetry. Remote in Europe.
            """,
            []),
        new(
            "Pinecrest Bank",
            "Junior Software Developer",
            "https://pinecrest.example/careers/junior-developer",
            ApplicationChannel.KariyerNet,
            AppliedDaysAgo: 26,
            """
            Pinecrest Bank Junior Software Developer: Java, Spring Boot, Oracle. On-site in Istanbul, graduate
            programme with training.
            """,
            [new(ApplicationStatus.Interview, DaysAgo: 17, "Online assessment, then a call")]),
        new(
            "Orbit Mobility",
            "Backend Engineer",
            "https://orbit-mobility.example/jobs/backend",
            ApplicationChannel.DirectMessage,
            AppliedDaysAgo: 19,
            """
            Orbit Mobility builds a ride-sharing app. Backend Engineer with Go or C#, PostgreSQL, Redis and gRPC.
            Hybrid in Izmir.
            """,
            [new(null, DaysAgo: 9, "Messaged the recruiter again")]),
        new(
            "Vantage Health",
            ".NET Developer",
            "https://vantage-health.example/careers/dotnet",
            ApplicationChannel.CompanySite,
            AppliedDaysAgo: 12,
            """
            Vantage Health .NET Developer: ASP.NET Core Web API, Entity Framework Core, PostgreSQL, Docker, xUnit.
            Remote in Turkey.
            """,
            [new(ApplicationStatus.Interview, DaysAgo: 4, "Technical interview booked")]),
        new(
            "Kestrel Studio",
            "Angular Developer",
            "https://kestrel-studio.example/jobs/angular",
            ApplicationChannel.LinkedInEasyApply,
            AppliedDaysAgo: 2,
            """
            Kestrel Studio Angular Developer: Angular, signals, RxJS, Tailwind CSS, accessibility. Remote.
            """,
            []),
    ];

    /// <summary>
    /// The demo neighbors' applications, to the postings in <see cref="SampleApplications"/>. Applied long enough ago
    /// to count in the community statistics, and arranged so a visitor sees a posting where almost nobody heard back,
    /// one where nobody did, a responsive company, and one posting with too few members to show anything.
    /// </summary>
    public static readonly IReadOnlyList<NeighborApplication> Neighbors =
    [
        new(1, "Northwind Logistics", AppliedDaysAgo: 48, [new(ApplicationStatus.Interview, DaysAgo: 41, "Phone screen")]),
        new(2, "Northwind Logistics", AppliedDaysAgo: 44, []),
        new(3, "Northwind Logistics", AppliedDaysAgo: 39, []),
        new(4, "Northwind Logistics", AppliedDaysAgo: 33, []),

        new(1, "Contoso", AppliedDaysAgo: 45, []),
        new(2, "Contoso", AppliedDaysAgo: 37, []),
        new(3, "Contoso", AppliedDaysAgo: 29, []),

        new(2, "Halden Games", AppliedDaysAgo: 58, [new(ApplicationStatus.Rejected, DaysAgo: 52, "Rejected after the CV review")]),
        new(3, "Halden Games", AppliedDaysAgo: 54, [new(ApplicationStatus.Interview, DaysAgo: 47, "Portfolio call")]),
        new(4, "Halden Games", AppliedDaysAgo: 50, [new(ApplicationStatus.Rejected, DaysAgo: 45, "Position filled")]),

        new(1, "Fabrikam Health", AppliedDaysAgo: 42, [new(ApplicationStatus.Interview, DaysAgo: 35, "Technical interview")]),
        new(3, "Fabrikam Health", AppliedDaysAgo: 36, [new(ApplicationStatus.Rejected, DaysAgo: 30, "Not enough experience")]),
        new(4, "Fabrikam Health", AppliedDaysAgo: 31, []),

        new(2, "Tidewater Analytics", AppliedDaysAgo: 40, []),
        new(4, "Tidewater Analytics", AppliedDaysAgo: 35, [new(ApplicationStatus.Rejected, DaysAgo: 28, "Rejected by email")]),
    ];

    /// <summary>The sample posting a neighbor applied to, with the neighbor's own dates and updates.</summary>
    public static SampleApplication For(NeighborApplication application) =>
        SampleApplications.All.Single(sample => sample.Company == application.Company) with
        {
            AppliedDaysAgo = application.AppliedDaysAgo,
            Updates = application.Updates,
        };
}
