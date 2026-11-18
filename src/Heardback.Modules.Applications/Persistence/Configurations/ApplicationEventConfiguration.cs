using Heardback.Modules.Applications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Applications.Persistence.Configurations;

internal sealed class ApplicationEventConfiguration : IEntityTypeConfiguration<ApplicationEvent>
{
    private const int EnumMaxLength = 32;

    public void Configure(EntityTypeBuilder<ApplicationEvent> builder)
    {
        builder.HasKey(timelineEvent => timelineEvent.Id);
        builder.Property(timelineEvent => timelineEvent.Id).ValueGeneratedNever();
        builder.Property(timelineEvent => timelineEvent.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(timelineEvent => timelineEvent.Note).HasMaxLength(ApplicationEvent.NoteMaxLength);
    }
}
