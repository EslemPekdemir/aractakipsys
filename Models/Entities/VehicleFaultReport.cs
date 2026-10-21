// File: Models/Entities/VehicleFaultReport.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.Entities;

public class VehicleFaultReport : BaseAuditableEntity
{
    [Required]
    public int VehicleId { get; set; }

    public int? DriverId { get; set; }

    public int? AssignmentId { get; set; }

    [Required]
    public int CurrentKm { get; set; }

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime ReportDate { get; set; }

    [Required]
    public FaultReportStatus Status { get; set; } = FaultReportStatus.Pending;

    [StringLength(450)]
    public string? ReviewerUserId { get; set; }

    [StringLength(500)]
    public string? ReviewerNotes { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // Navigation Properties
    public Vehicle Vehicle { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Assignment? Assignment { get; set; }
    public ApplicationUser? ReviewerUser { get; set; }
}
