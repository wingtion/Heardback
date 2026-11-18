using Heardback.Modules.Postings.Contracts;
using Heardback.Modules.Postings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Postings.Persistence.Configurations;

internal sealed class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.HasKey(posting => posting.Id);
        builder.Property(posting => posting.Id).ValueGeneratedNever();
        builder.Property(posting => posting.Url).HasMaxLength(PostingLimits.UrlMaxLength);
        builder.Property(posting => posting.NormalizedUrl).HasMaxLength(PostingLimits.UrlMaxLength);
        builder.Property(posting => posting.Company).HasMaxLength(PostingLimits.CompanyMaxLength);
        builder.Property(posting => posting.Title).HasMaxLength(PostingLimits.TitleMaxLength);
        builder.Property(posting => posting.Text).HasMaxLength(PostingLimits.TextMaxLength);
        builder.Property(posting => posting.ContentHash).HasMaxLength(PostingContentHash.Length).IsFixedLength();

        // These two unique indexes make concurrent submissions of the same posting collapse into one row.
        // Postgres treats NULLs as distinct, so postings without a link or text never collide.
        builder.HasIndex(posting => posting.NormalizedUrl).IsUnique();
        builder.HasIndex(posting => posting.ContentHash).IsUnique();
    }
}
