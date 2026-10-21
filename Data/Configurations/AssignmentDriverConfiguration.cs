// File: Data/Configurations/AssignmentDriverConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class AssignmentDriverConfiguration : IEntityTypeConfiguration<AssignmentDriver>
{
    public void Configure(EntityTypeBuilder<AssignmentDriver> builder)
    {
        builder.HasIndex(ad => new { ad.AssignmentId, ad.DriverId })
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        builder.Property(ad => ad.IsPrimary)
            .HasDefaultValue(false);

        builder.HasOne(ad => ad.Assignment)
            .WithMany(a => a.AssignmentDrivers)
            .HasForeignKey(ad => ad.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ad => ad.Driver)
            .WithMany(d => d.AssignmentDrivers)
            .HasForeignKey(ad => ad.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
