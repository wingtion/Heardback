using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Postings.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Applications.Persistence.Configurations;

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    // Longest enum name stored as text, with room to grow.
    private const int EnumMaxLength = 32;

    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(application => application.Id);
        builder.Property(application => application.Id).ValueGeneratedNever();
        builder.Property(application => application.Company).HasMaxLength(PostingLimits.CompanyMaxLength);
        builder.Property(application => application.Role).HasMaxLength(PostingLimits.TitleMaxLength);
        builder.Property(application => application.Url).HasMaxLength(PostingLimits.UrlMaxLength);
        builder.Property(application => application.PostingText).HasMaxLength(PostingLimits.TextMaxLength);
        builder.Property(application => application.Notes).HasMaxLength(Application.NotesMaxLength);
        builder.Property(application => application.Channel).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(application => application.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(application => application.Version).IsRowVersion();

        builder.Property(application => application.NormalizedUrl).HasMaxLength(PostingLimits.UrlMaxLength);
        builder.Property(application => application.ContentHash)
            .HasMaxLength(PostingKeys.ContentHashLength)
            .IsFixedLength();

        // One application per user per posting, per link and per posting text; also stops a double submit from
        // creating two. NULLs are distinct, so applications without a link or text never collide.
        builder.HasIndex(application => new { application.UserId, application.PostingId }).IsUnique();
        builder.HasIndex(application => new { application.UserId, application.NormalizedUrl }).IsUnique();
        builder.HasIndex(application => new { application.UserId, application.ContentHash }).IsUnique();
        builder.HasIndex(application => new { application.UserId, application.UpdatedAt });

        builder.HasMany(application => application.Events)
            .WithOne()
            .HasForeignKey(timelineEvent => timelineEvent.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(application => application.Events)
            .HasField("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
