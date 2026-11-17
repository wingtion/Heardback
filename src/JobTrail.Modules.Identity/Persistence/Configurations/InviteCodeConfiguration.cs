using JobTrail.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobTrail.Modules.Identity.Persistence.Configurations;

internal sealed class InviteCodeConfiguration : IEntityTypeConfiguration<InviteCode>
{
    public void Configure(EntityTypeBuilder<InviteCode> builder)
    {
        builder.HasKey(invite => invite.Id);
        builder.Property(invite => invite.Id).ValueGeneratedNever();
        builder.Property(invite => invite.Code).HasMaxLength(InviteCode.CodeLength).IsFixedLength();
        builder.HasIndex(invite => invite.Code).IsUnique();
        builder.HasIndex(invite => invite.CreatedAt);
        builder.Ignore(invite => invite.DisplayCode);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invite => invite.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invite => invite.UsedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
