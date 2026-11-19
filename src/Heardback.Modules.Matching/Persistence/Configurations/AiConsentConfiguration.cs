using Heardback.Modules.Matching.Tailoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Matching.Persistence.Configurations;

internal sealed class AiConsentConfiguration : IEntityTypeConfiguration<AiConsent>
{
    public void Configure(EntityTypeBuilder<AiConsent> builder)
    {
        builder.HasKey(consent => consent.UserId);
        builder.Property(consent => consent.UserId).ValueGeneratedNever();
    }
}
