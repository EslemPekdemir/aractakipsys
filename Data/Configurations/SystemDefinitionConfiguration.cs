// File: Data/Configurations/SystemDefinitionConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class SystemDefinitionConfiguration : IEntityTypeConfiguration<SystemDefinition>
{
    public void Configure(EntityTypeBuilder<SystemDefinition> builder)
    {
        builder.ToTable("SystemDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.IsSystemDefault)
            .HasDefaultValue(false);

        // Filtreli Unique Index: Aynı kategoride aynı normalleştirilmiş ad (IsDeleted = 0) birden fazla olamaz
        builder.HasIndex(x => new { x.CategoryKey, x.NormalizedName })
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        builder.HasIndex(x => new { x.CategoryKey, x.IsActive });
    }
}
