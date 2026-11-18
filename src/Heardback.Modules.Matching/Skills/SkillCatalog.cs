namespace Heardback.Modules.Matching.Skills;

/// <summary>
/// The skills dictionary (PLAN.md Phase 3): what Heardback recognises in CVs and postings, with the ways people
/// write each skill. Plain English words ("express", "spring", "rest") are only matched in their technical
/// spelling or capitalisation, so ordinary sentences do not count as skills. Known edge: a capitalised word
/// at the start of a sentence ("Unity of purpose…", "Swift delivery…") still counts.
/// </summary>
internal static class SkillCatalog
{
    private const string DotNet = ".NET";
    private const string Sql = "SQL";
    private const string JavaScript = "JavaScript";
    private const string Git = "Git";
    private const string Python = "Python";
    private const string Java = "Java";
    private const string Css = "CSS";
    private const string Php = "PHP";
    private const string Ruby = "Ruby";
    private const string Dart = "Dart";
    private const string Kotlin = "Kotlin";
    private const string Csharp = "C#";
    private const string TypeScript = "TypeScript";
    private const string React = "React";
    private const string Azure = "Azure";
    private const string Docker = "Docker";
    private const string Unity = "Unity";
    private const string MachineLearning = "Machine learning";
    private const string CiCd = "CI/CD";
    private const string PostgreSql = "PostgreSQL";
    private const string UnitTesting = "Unit testing";

    public static IReadOnlyList<Skill> All { get; } =
    [
        // Languages
        Define(Csharp, SkillCategory.Language, ["C#", "C Sharp", "CSharp"], implies: [DotNet]),
        Define("F#", SkillCategory.Language, ["F#"], implies: [DotNet]),
        Define("C++", SkillCategory.Language, ["C++", "CPP"]),
        Define("C", SkillCategory.Language, exact: ["C"]),
        Define(Java, SkillCategory.Language, ["Java"]),
        Define(JavaScript, SkillCategory.Language, ["JavaScript", "Java Script", "ES6", "ECMAScript"], exact: ["JS"]),
        Define(TypeScript, SkillCategory.Language, ["TypeScript"], implies: [JavaScript]),
        Define(Python, SkillCategory.Language, ["Python"]),
        Define("Go", SkillCategory.Language, ["Golang"], exact: ["Go"]),
        Define("Rust", SkillCategory.Language, exact: ["Rust"]),
        Define(Kotlin, SkillCategory.Language, ["Kotlin"]),
        Define("Swift", SkillCategory.Language, ["SwiftUI"], exact: ["Swift"]),
        Define(Php, SkillCategory.Language, ["PHP"]),
        Define(Ruby, SkillCategory.Language, exact: ["Ruby"]),
        Define(Dart, SkillCategory.Language, exact: ["Dart"]),
        Define("Scala", SkillCategory.Language, exact: ["Scala"]),
        Define("R", SkillCategory.Language, exact: ["R"]),
        Define(Sql, SkillCategory.Language, ["SQL"]),
        Define("T-SQL", SkillCategory.Language, ["T-SQL", "TSQL", "Transact-SQL"], implies: [Sql]),
        Define("PL/SQL", SkillCategory.Language, ["PL/SQL", "PLSQL"], implies: [Sql]),
        Define("HTML", SkillCategory.Language, ["HTML", "HTML5"]),
        Define(Css, SkillCategory.Language, ["CSS", "CSS3"]),
        Define("Sass", SkillCategory.Language, ["Sass", "SCSS"], implies: [Css]),
        Define("Bash", SkillCategory.Language, ["Bash", "Shell scripting"]),
        Define("PowerShell", SkillCategory.Language, ["PowerShell"]),

        // Frameworks and libraries
        Define(DotNet, SkillCategory.Framework, [".NET", "dotnet", ".NET Core", ".NET Framework"]),
        Define("ASP.NET Core", SkillCategory.Framework, ["ASP.NET Core", "ASP.NET", "ASP.NET MVC", "ASP.NET Web API"],
            implies: [DotNet]),
        Define("Entity Framework", SkillCategory.Framework, ["Entity Framework", "EF Core", "Entity Framework Core"],
            implies: [DotNet]),
        Define("Blazor", SkillCategory.Framework, ["Blazor"], implies: [DotNet]),
        Define("WPF", SkillCategory.Framework, ["WPF"], implies: [DotNet]),
        Define("LINQ", SkillCategory.Framework, ["LINQ"], implies: [DotNet]),
        Define("SignalR", SkillCategory.Framework, ["SignalR"], implies: [DotNet]),
        Define("Angular", SkillCategory.Framework, ["Angular", "Angular 2+"], implies: [TypeScript]),
        Define("AngularJS", SkillCategory.Framework, ["AngularJS", "Angular.js"], implies: [JavaScript]),
        Define(React, SkillCategory.Framework, ["React", "React.js", "ReactJS"], implies: [JavaScript]),
        Define("React Native", SkillCategory.Framework, ["React Native"], implies: [JavaScript]),
        Define("Redux", SkillCategory.Framework, ["Redux"], implies: [React]),
        Define("Next.js", SkillCategory.Framework, ["Next.js", "NextJS"], implies: [React]),
        Define("Vue.js", SkillCategory.Framework, ["Vue", "Vue.js", "VueJS"], implies: [JavaScript]),
        Define("Svelte", SkillCategory.Framework, ["Svelte", "SvelteKit"], implies: [JavaScript]),
        Define("jQuery", SkillCategory.Framework, ["jQuery"], implies: [JavaScript]),
        Define("Node.js", SkillCategory.Framework, ["Node.js", "NodeJS", "Node JS"], implies: [JavaScript]),
        Define("Express", SkillCategory.Framework, ["Express.js", "ExpressJS"], implies: ["Node.js"]),
        Define("NestJS", SkillCategory.Framework, ["NestJS", "Nest.js"], implies: ["Node.js", TypeScript]),
        Define("Spring Boot", SkillCategory.Framework, ["Spring Boot", "Spring Framework", "Spring MVC"],
            implies: [Java]),
        Define("Hibernate", SkillCategory.Framework, ["Hibernate"], implies: [Java]),
        Define("Django", SkillCategory.Framework, ["Django"], implies: [Python]),
        Define("Flask", SkillCategory.Framework, ["Flask"], implies: [Python]),
        Define("FastAPI", SkillCategory.Framework, ["FastAPI"], implies: [Python]),
        Define("Laravel", SkillCategory.Framework, ["Laravel"], implies: [Php]),
        Define("Ruby on Rails", SkillCategory.Framework, ["Ruby on Rails", "Rails"], implies: [Ruby]),
        Define("Flutter", SkillCategory.Framework, ["Flutter"], implies: [Dart]),
        Define("Android", SkillCategory.Framework, ["Android"]),
        Define("iOS", SkillCategory.Framework, exact: ["iOS"]),
        Define("Tailwind CSS", SkillCategory.Framework, ["Tailwind", "Tailwind CSS", "TailwindCSS"], implies: [Css]),
        Define("Bootstrap", SkillCategory.Framework, exact: ["Bootstrap"]),
        Define(Unity, SkillCategory.Framework, ["Unity3D", "Unity 3D"], exact: [Unity], implies: [Csharp]),
        Define("Pandas", SkillCategory.Framework, ["Pandas"], implies: [Python]),
        Define("NumPy", SkillCategory.Framework, ["NumPy"], implies: [Python]),
        Define("TensorFlow", SkillCategory.Framework, ["TensorFlow"], implies: [MachineLearning]),
        Define("PyTorch", SkillCategory.Framework, ["PyTorch"], implies: [MachineLearning]),
        Define("MassTransit", SkillCategory.Framework, ["MassTransit"], implies: [DotNet]),
        Define("MediatR", SkillCategory.Framework, ["MediatR"], implies: [DotNet]),
        Define("AutoMapper", SkillCategory.Framework, ["AutoMapper"], implies: [DotNet]),
        Define("Dapper", SkillCategory.Framework, ["Dapper"], implies: [DotNet]),
        Define("Serilog", SkillCategory.Framework, ["Serilog"], implies: [DotNet]),
        Define("GraphQL", SkillCategory.Framework, ["GraphQL"]),
        Define("gRPC", SkillCategory.Framework, ["gRPC"]),

        // Databases and messaging
        Define(PostgreSql, SkillCategory.Database, ["PostgreSQL", "Postgres"], implies: [Sql]),
        Define("SQL Server", SkillCategory.Database, ["SQL Server", "MS SQL", "MSSQL", "Microsoft SQL Server"],
            implies: [Sql]),
        Define("MySQL", SkillCategory.Database, ["MySQL", "MariaDB"], implies: [Sql]),
        Define("Oracle Database", SkillCategory.Database, ["Oracle Database", "Oracle DB"], implies: [Sql]),
        Define("SQLite", SkillCategory.Database, ["SQLite"], implies: [Sql]),
        Define("pgvector", SkillCategory.Database, ["pgvector"], implies: [PostgreSql]),
        Define("MongoDB", SkillCategory.Database, ["MongoDB", "Mongo"]),
        Define("Redis", SkillCategory.Database, ["Redis"]),
        Define("Elasticsearch", SkillCategory.Database, ["Elasticsearch", "Elastic Search", "OpenSearch"]),
        Define("Cassandra", SkillCategory.Database, ["Cassandra"]),
        Define("RabbitMQ", SkillCategory.Database, ["RabbitMQ"]),
        Define("Kafka", SkillCategory.Database, ["Kafka", "Apache Kafka"]),
        Define("NoSQL", SkillCategory.Database, ["NoSQL"]),

        // Cloud and DevOps
        Define(Docker, SkillCategory.CloudAndDevOps, ["Docker", "Docker Compose"]),
        Define("Kubernetes", SkillCategory.CloudAndDevOps, ["Kubernetes", "K8s"], implies: [Docker]),
        Define("AWS", SkillCategory.CloudAndDevOps, ["AWS", "Amazon Web Services"]),
        Define(Azure, SkillCategory.CloudAndDevOps, ["Azure", "Microsoft Azure"]),
        Define("Google Cloud", SkillCategory.CloudAndDevOps, ["Google Cloud", "GCP", "Google Cloud Platform"]),
        Define("Terraform", SkillCategory.CloudAndDevOps, ["Terraform"]),
        Define("Linux", SkillCategory.CloudAndDevOps, ["Linux", "Ubuntu"]),
        Define("Nginx", SkillCategory.CloudAndDevOps, ["Nginx"]),
        Define("Fly.io", SkillCategory.CloudAndDevOps, ["Fly.io"]),
        Define("Netlify", SkillCategory.CloudAndDevOps, ["Netlify"]),
        Define("Vercel", SkillCategory.CloudAndDevOps, ["Vercel"]),
        Define("OpenTelemetry", SkillCategory.CloudAndDevOps, ["OpenTelemetry", "OTel"]),
        Define("Prometheus", SkillCategory.CloudAndDevOps, ["Prometheus"]),
        Define("Grafana", SkillCategory.CloudAndDevOps, ["Grafana"]),
        Define(CiCd, SkillCategory.CloudAndDevOps, ["CI/CD", "CICD", "Continuous integration", "Continuous delivery"]),
        Define("GitHub Actions", SkillCategory.CloudAndDevOps, ["GitHub Actions"], implies: [CiCd, Git]),
        Define("Azure DevOps", SkillCategory.CloudAndDevOps, ["Azure DevOps"], implies: [CiCd, Azure]),
        Define("Jenkins", SkillCategory.CloudAndDevOps, ["Jenkins"], implies: [CiCd]),
        Define(Git, SkillCategory.CloudAndDevOps, ["Git"]),
        Define("GitHub", SkillCategory.CloudAndDevOps, ["GitHub"], implies: [Git]),
        Define("GitLab", SkillCategory.CloudAndDevOps, ["GitLab"], implies: [Git]),

        // Testing
        Define(UnitTesting, SkillCategory.Testing, ["Unit testing", "Unit tests", "xUnit", "NUnit", "MSTest", "JUnit"]),
        Define("TDD", SkillCategory.Testing, ["TDD", "Test-driven development", "Test driven development"]),
        Define("Jest", SkillCategory.Testing, exact: ["Jest"]),
        Define("Cypress", SkillCategory.Testing, ["Cypress"]),
        Define("Playwright", SkillCategory.Testing, ["Playwright"]),
        Define("Selenium", SkillCategory.Testing, ["Selenium"]),
        Define("Moq", SkillCategory.Testing, ["Moq", "NSubstitute"], implies: [UnitTesting]),
        Define("Testcontainers", SkillCategory.Testing, ["Testcontainers", "Test containers"]),

        // Practices and architecture
        Define("REST APIs", SkillCategory.Practice, ["RESTful", "REST API", "REST APIs", "Web API", "Web APIs"],
            exact: ["REST"]),
        Define("Microservices", SkillCategory.Practice, ["Microservices", "Microservice", "Micro-services"]),
        Define("Domain-driven design", SkillCategory.Practice, ["Domain-driven design", "Domain driven design", "DDD"]),
        Define("Clean Architecture", SkillCategory.Practice, ["Clean Architecture"]),
        Define("Design patterns", SkillCategory.Practice, ["Design patterns", "SOLID"]),
        Define("OOP", SkillCategory.Practice, ["OOP", "Object-oriented programming", "Object oriented programming"]),
        Define("Agile", SkillCategory.Practice, ["Agile", "Scrum", "Kanban"]),
        Define("OAuth", SkillCategory.Practice, ["OAuth", "OAuth2", "OAuth 2.0", "OpenID Connect", "OIDC"]),
        Define("JWT", SkillCategory.Practice, ["JWT", "JSON Web Token", "JSON Web Tokens"]),
        Define("OpenAPI", SkillCategory.Practice, ["OpenAPI", "Swagger"]),
        Define(MachineLearning, SkillCategory.Practice, ["Machine learning", "Deep learning"], exact: ["ML"]),

        // Tools
        Define("Jira", SkillCategory.Tool, ["Jira"]),
        Define("Figma", SkillCategory.Tool, ["Figma"]),
        Define("Power BI", SkillCategory.Tool, ["Power BI", "PowerBI"]),
        Define("Excel", SkillCategory.Tool, exact: ["Excel", "MS Excel"]),
    ];

    private static Skill Define(
        string name,
        SkillCategory category,
        IReadOnlyList<string>? aliases = null,
        IReadOnlyList<string>? exact = null,
        IReadOnlyList<string>? implies = null) =>
        new(name, category, aliases ?? [], exact ?? [], implies ?? []);
}
