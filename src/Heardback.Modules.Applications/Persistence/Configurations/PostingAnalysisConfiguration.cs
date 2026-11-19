using Heardback.Modules.Applications.Analysis;
using Heardback.Modules.Postings.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Applications.Persistence.Configurations;

internal sealed class PostingAnalysisConfiguration : IEntityTypeConfiguration<PostingAnalysis>
{
    private const int EnumMaxLength = 32;
    private const int ProviderMaxLength = 32;
    private const int ModelMaxLength = 100;

    public void Configure(EntityTypeBuilder<PostingAnalysis> builder)
    {
        builder.HasKey(analysis => analysis.ContentHash);
        builder.Property(analysis => analysis.ContentHash)
            .HasMaxLength(PostingKeys.ContentHashLength)
            .IsFixedLength()
            .ValueGeneratedNever();
        builder.Property(analysis => analysis.Provider).HasMaxLength(ProviderMaxLength);
        builder.Property(analysis => analysis.Model).HasMaxLength(ModelMaxLength);
        builder.Property(analysis => analysis.Company).HasMaxLength(PostingAnalysisPrompt.TextMaxLength);
        builder.Property(analysis => analysis.Title).HasMaxLength(PostingAnalysisPrompt.TextMaxLength);
        builder.Property(analysis => analysis.Location).HasMaxLength(PostingAnalysisPrompt.TextMaxLength);
        builder.Property(analysis => analysis.Seniority).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(analysis => analysis.WorkMode).HasConversion<string>().HasMaxLength(EnumMaxLength);
    }
}
