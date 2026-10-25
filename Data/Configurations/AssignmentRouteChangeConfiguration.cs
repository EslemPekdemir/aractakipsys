// File: Data/Configurations/AssignmentRouteChangeConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class AssignmentRouteChangeConfiguration : IEntityTypeConfiguration<AssignmentRouteChange>
{
    public void Configure(EntityTypeBuilder<AssignmentRouteChange> builder)
    {
        builder.ToTable("AssignmentRouteChanges");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NewRoute)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.OldRoute)
            .HasMaxLength(250);

        builder.Property(x => x.OldDestination)
            .HasMaxLength(200);

        builder.Property(x => x.NewDestination)
            .HasMaxLength(200);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ChangedByFullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(x => x.Assignment)
            .WithMany(a => a.RouteChanges)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AssignmentId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
