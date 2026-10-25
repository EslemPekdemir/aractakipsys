// File: Models/Entities/VehicleDocument.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.Entities;

public class VehicleDocument : BaseAuditableEntity
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public VehicleDocumentType DocumentType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; } = 0;

    [StringLength(100)]
    public string? PolicyOrStationNumber { get; set; }

    [StringLength(100)]
    public string? CompanyName { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation Property
    public Vehicle Vehicle { get; set; } = null!;
}
