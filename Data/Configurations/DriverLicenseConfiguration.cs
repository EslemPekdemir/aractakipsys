// File: Data/Configurations/DriverLicenseConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class DriverLicenseConfiguration : IEntityTypeConfiguration<DriverLicense>
{
    public void Configure(EntityTypeBuilder<DriverLicense> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LicenseClass)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.LicenseExpiryDate)
            .IsRequired();

        builder.HasOne(x => x.Driver)
            .WithMany(d => d.DriverLicenses)
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
