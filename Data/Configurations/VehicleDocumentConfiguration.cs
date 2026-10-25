// File: Data/Configurations/VehicleDocumentConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class VehicleDocumentConfiguration : IEntityTypeConfiguration<VehicleDocument>
{
    public void Configure(EntityTypeBuilder<VehicleDocument> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.Cost)
            .HasPrecision(18, 2);

        builder.Property(x => x.PolicyOrStationNumber)
            .HasMaxLength(100);

        builder.Property(x => x.CompanyName)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasOne(x => x.Vehicle)
            .WithMany(v => v.Documents)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
