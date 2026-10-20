// File: Data/Configurations/AssignmentConfiguration.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        // Composite Indexes
        builder.HasIndex(x => new { x.VehicleId, x.PlannedStartTime, x.PlannedEndTime });
        builder.HasIndex(x => new { x.DriverId, x.PlannedStartTime, x.PlannedEndTime });

        builder.Property(x => x.RequestedByDepartment)
            .HasMaxLength(150);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(300);

        builder.Property(x => x.DriverReturnNotes)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasDefaultValue(AssignmentStatus.Planned);

        builder.Property(x => x.StartReportedBySelf)
            .HasDefaultValue(false);

        builder.Property(x => x.StartApprovedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.EndReportedBySelf)
            .HasDefaultValue(false);

        builder.Property(x => x.EndApprovedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.AccommodationCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.PerDiemType)
            .HasMaxLength(20)
            .HasDefaultValue("Hourly");

        builder.Property(x => x.PerDiemHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.PerDiemRate)
            .HasPrecision(18, 2);

        builder.Property(x => x.PerDiemAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.ExpenseNotes)
            .HasMaxLength(500);

        builder.Property(x => x.HourlyRate)
            .HasPrecision(18, 2);

        builder.Property(x => x.CalculatedDutyHours)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalDriverFee)
            .HasPrecision(18, 2);

        // Delete Behavior: Restrict
        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StartApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.StartApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EndApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.EndApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // İmzalı Belge Konfigürasyonu (Aşama H)
        builder.Property(x => x.SignedDocumentPath)
            .HasMaxLength(500);

        builder.Property(x => x.SignedDocumentOriginalFileName)
            .HasMaxLength(255);

        builder.Property(x => x.SignedDocumentUploadedByUserId)
            .HasMaxLength(450);

        builder.HasOne(x => x.SignedDocumentUploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.SignedDocumentUploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
