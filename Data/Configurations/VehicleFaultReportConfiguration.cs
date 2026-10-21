// File: Data/Configurations/VehicleFaultReportConfiguration.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class VehicleFaultReportConfiguration : IEntityTypeConfiguration<VehicleFaultReport>
{
    public void Configure(EntityTypeBuilder<VehicleFaultReport> builder)
    {
        builder.HasIndex(x => new { x.VehicleId, x.Status });
        builder.HasIndex(x => x.ReportDate);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasDefaultValue(FaultReportStatus.Pending);

        builder.Property(x => x.ReviewerUserId)
            .HasMaxLength(450);

        builder.Property(x => x.ReviewerNotes)
            .HasMaxLength(500);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Assignment)
            .WithMany()
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewerUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
