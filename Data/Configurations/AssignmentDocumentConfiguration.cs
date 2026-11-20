// File: Data/Configurations/AssignmentDocumentConfiguration.cs
using aractakipsys.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aractakipsys.Data.Configurations;

public class AssignmentDocumentConfiguration : IEntityTypeConfiguration<AssignmentDocument>
{
    public void Configure(EntityTypeBuilder<AssignmentDocument> builder)
    {
        builder.HasIndex(x => x.AssignmentId);
        builder.HasIndex(x => x.DriverId);
        builder.HasIndex(x => x.UploadedAt);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Description)
            .HasMaxLength(250);

        builder.Property(x => x.UploadedByUserId)
            .HasMaxLength(450);

        builder.HasOne(x => x.Assignment)
            .WithMany(a => a.SignedDocuments)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
