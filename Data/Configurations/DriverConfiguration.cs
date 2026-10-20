// File: Data/Configurations/DriverConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.LicenseNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.LicenseClass)
            .HasMaxLength(10);

        builder.Property(x => x.Department)
            .HasMaxLength(150);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Driver>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
