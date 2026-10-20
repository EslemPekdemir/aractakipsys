// File: Data/Configurations/VehicleConfiguration.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasIndex(x => x.PlateNumber)
            .IsUnique();

        builder.Property(x => x.PlateNumber)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(x => x.Brand)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Color)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.CurrentKm)
            .HasDefaultValue(0);

        builder.Property(x => x.VehicleType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.TireSeason)
            .HasMaxLength(50);

        builder.Property(x => x.FuelType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasDefaultValue(VehicleStatus.Idle);
    }
}
