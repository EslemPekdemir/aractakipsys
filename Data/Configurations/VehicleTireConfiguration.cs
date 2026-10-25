// File: Data/Configurations/VehicleTireConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class VehicleTireConfiguration : IEntityTypeConfiguration<VehicleTire>
{
    public void Configure(EntityTypeBuilder<VehicleTire> builder)
    {
        builder.ToTable("VehicleTires");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Brand)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Size)
            .HasMaxLength(30);

        builder.Property(t => t.DotDate)
            .HasMaxLength(10);

        builder.Property(t => t.Season)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Cost)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.Notes)
            .HasMaxLength(500);

        builder.HasOne(t => t.Vehicle)
            .WithMany(v => v.Tires)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => new { t.VehicleId, t.IsDeleted });
    }
}
