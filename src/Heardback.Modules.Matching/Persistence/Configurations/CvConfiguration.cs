using Heardback.Modules.Matching.Cvs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Matching.Persistence.Configurations;

internal sealed class CvConfiguration : IEntityTypeConfiguration<Cv>
{
    public void Configure(EntityTypeBuilder<Cv> builder)
    {
        builder.HasKey(cv => cv.Id);
        builder.Property(cv => cv.Id).ValueGeneratedNever();
        builder.Property(cv => cv.Text).HasMaxLength(Cv.TextMaxLength);

        // One CV per user; also stops two uploads at once from leaving two rows.
        builder.HasIndex(cv => cv.UserId).IsUnique();
    }
}
