// File: Data/Configurations/AuditLogConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Action)
            .HasConversion<string>();

        builder.Property(x => x.ChangedByUserId)
            .IsRequired(false);

        builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OnBehalfOfUser)
            .WithMany()
            .HasForeignKey(x => x.OnBehalfOfUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Delegation)
            .WithMany()
            .HasForeignKey(x => x.DelegationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
