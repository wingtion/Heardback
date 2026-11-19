using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Infrastructure.Llm.Quotas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Infrastructure.Llm.Persistence.Configurations;

internal sealed class DailyUsageConfiguration : IEntityTypeConfiguration<DailyUsage>
{
    public void Configure(EntityTypeBuilder<DailyUsage> builder)
    {
        builder.ToTable("daily_usage");
        builder.HasKey(usage => new { usage.UserId, usage.Day, usage.Kind });
        builder.Property(usage => usage.Kind).HasMaxLength(AnalysisJob.KindMaxLength);
    }
}
