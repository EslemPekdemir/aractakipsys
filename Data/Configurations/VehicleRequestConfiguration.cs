// File: Data/Configurations/VehicleRequestConfiguration.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class VehicleRequestConfiguration : IEntityTypeConfiguration<VehicleRequest>
{
    public void Configure(EntityTypeBuilder<VehicleRequest> builder)
    {
        // Indexes
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequesterEmail);
        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.RequesterFullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.RequesterEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.RequesterPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.RequestType)
            .HasConversion<string>()
            .HasDefaultValue(VehicleRequestType.OfficialTrip);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasDefaultValue(VehicleRequestStatus.Pending);

        builder.Property(x => x.FulfilledByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        // Delete Behaviors (Restrict to avoid cascade cycles and maintain audit integrity)
        builder.HasOne(x => x.FulfilledAssignment)
            .WithMany()
            .HasForeignKey(x => x.FulfilledAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FulfilledByUser)
            .WithMany()
            .HasForeignKey(x => x.FulfilledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
