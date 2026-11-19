using Heardback.Infrastructure.Llm.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Infrastructure.Llm.Persistence.Configurations;

internal sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public const string TableName = "analysis_jobs";

    /// <summary>At most one unfinished job per kind and subject. The SQL in AnalysisJobs repeats this predicate.</summary>
    public const string OneUnfinishedJobIndex = "ux_analysis_jobs_one_unfinished";

    public const string UnfinishedFilter = "status IN ('Pending', 'Running')";

    private const int StatusMaxLength = 16;

    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(job => job.Id);

        builder.Property(job => job.Kind).HasMaxLength(AnalysisJob.KindMaxLength);
        builder.Property(job => job.SubjectKey).HasMaxLength(AnalysisJob.SubjectKeyMaxLength);
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        builder.Property(job => job.LastError).HasMaxLength(AnalysisJob.ErrorMaxLength);

        builder.HasIndex(job => new { job.Kind, job.SubjectKey })
            .IsUnique()
            .HasFilter(UnfinishedFilter)
            .HasDatabaseName(OneUnfinishedJobIndex);

        // Claims look for due pending jobs and expired leases.
        builder.HasIndex(job => new { job.Status, job.NextRunAt });

        // Reading a subject's latest job.
        builder.HasIndex(job => new { job.Kind, job.SubjectKey, job.CreatedAt });
    }
}
