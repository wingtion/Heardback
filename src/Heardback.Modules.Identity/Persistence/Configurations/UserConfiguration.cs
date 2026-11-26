using Heardback.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Identity.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private const int KindMaxLength = 20;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).HasMaxLength(User.EmailMaxLength);
        builder.Property(user => user.NormalizedEmail).HasMaxLength(User.EmailMaxLength);
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
        builder.Property(user => user.DisplayName).HasMaxLength(User.DisplayNameMaxLength);
        builder.Property(user => user.Kind).HasConversion<string>().HasMaxLength(KindMaxLength);
        builder.Ignore(user => user.IsDemo);

        // The cleanup finds expired demo visitors; members are never in this partial index.
        builder.HasIndex(user => user.ExpiresAt).HasFilter("expires_at IS NOT NULL");
    }
}
