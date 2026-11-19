using Heardback.Modules.Matching.Tailoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Matching.Persistence.Configurations;

internal sealed class TailoringResultConfiguration : IEntityTypeConfiguration<TailoringResult>
{
    private const int ProviderMaxLength = 32;
    private const int ModelMaxLength = 100;

    public void Configure(EntityTypeBuilder<TailoringResult> builder)
    {
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Id).ValueGeneratedNever();
        builder.Property(result => result.CvFingerprint).HasMaxLength(TailoringFingerprint.Length).IsFixedLength();
        builder.Property(result => result.PostingFingerprint)
            .HasMaxLength(TailoringFingerprint.Length)
            .IsFixedLength();
        builder.Property(result => result.Provider).HasMaxLength(ProviderMaxLength);
        builder.Property(result => result.Model).HasMaxLength(ModelMaxLength);
        builder.Property(result => result.Summary).HasMaxLength(TailoringPrompt.SummaryMaxLength);
        builder.OwnsMany(result => result.Suggestions, suggestions => suggestions.ToJson());

        // One result per application; a new request replaces it.
        builder.HasIndex(result => new { result.UserId, result.ApplicationId }).IsUnique();
    }
}
