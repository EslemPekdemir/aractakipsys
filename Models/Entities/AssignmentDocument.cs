// File: Models/Entities/AssignmentDocument.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

public class AssignmentDocument : BaseAuditableEntity
{
    [Required]
    public int AssignmentId { get; set; }

    /// <summary>
    /// Opsiyonel: Belgenin ait olduğu şoför (çok şoförlü görevlerde her şoförün kendi imzalı belgesini ayırmak için).
    /// Null ise genel görev belgesi olarak kabul edilir.
    /// </summary>
    public int? DriverId { get; set; }

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? UploadedByUserId { get; set; }

    // Navigation Properties
    public Assignment Assignment { get; set; } = null!;
    public Driver? Driver { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
}
