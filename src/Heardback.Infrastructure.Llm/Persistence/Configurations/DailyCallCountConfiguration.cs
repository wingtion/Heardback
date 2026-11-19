using Heardback.Infrastructure.Llm.Quotas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Infrastructure.Llm.Persistence.Configurations;

internal sealed class DailyCallCountConfiguration : IEntityTypeConfiguration<DailyCallCount>
{
    public void Configure(EntityTypeBuilder<DailyCallCount> builder)
    {
        builder.ToTable("daily_call_counts");
        builder.HasKey(counts => counts.Day);
    }
}
