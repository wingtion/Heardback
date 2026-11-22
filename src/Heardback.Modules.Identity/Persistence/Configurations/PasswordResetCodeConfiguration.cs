using Heardback.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heardback.Modules.Identity.Persistence.Configurations;

internal sealed class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
    {
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).ValueGeneratedNever();
        builder.Property(code => code.CodeHash).HasMaxLength(PasswordResetCode.HashLength).IsFixedLength();
        builder.HasIndex(code => code.CodeHash).IsUnique();
        builder.HasIndex(code => code.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(code => code.IssuedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
