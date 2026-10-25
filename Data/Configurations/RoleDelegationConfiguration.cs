// File: Data/Configurations/RoleDelegationConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class RoleDelegationConfiguration : IEntityTypeConfiguration<RoleDelegation>
{
    public void Configure(EntityTypeBuilder<RoleDelegation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DelegatorUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.DelegateeUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.RevokeReason)
            .HasMaxLength(500);

        builder.Property(x => x.OriginalDelegateeRole)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DelegatedRole)
            .IsRequired()
            .HasMaxLength(50);

        // Foreign keys with DeleteBehavior.Restrict
        builder.HasOne(x => x.DelegatorUser)
            .WithMany()
            .HasForeignKey(x => x.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DelegateeUser)
            .WithMany()
            .HasForeignKey(x => x.DelegateeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RevokedByUser)
            .WithMany()
            .HasForeignKey(x => x.RevokedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // DÜZELTME 8: IX_RoleDelegations_ActiveCheck indeksi Status olmadan
        builder.HasIndex(x => new { x.DelegateeUserId, x.StartDate, x.EndDate, x.ActualRevokedAt, x.IsDeleted })
            .HasDatabaseName("IX_RoleDelegations_ActiveCheck");
    }
}
