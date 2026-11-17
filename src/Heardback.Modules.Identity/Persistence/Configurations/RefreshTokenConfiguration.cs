using Heardback.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Identity.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    // Hex-encoded SHA-256.
    private const int TokenHashLength = 64;

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.TokenHash).HasMaxLength(TokenHashLength).IsFixedLength();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.FamilyId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
