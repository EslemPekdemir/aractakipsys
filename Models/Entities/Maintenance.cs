// File: Models/Entities/Maintenance.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

public class Maintenance : BaseAuditableEntity
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    [StringLength(100)]
    public string MaintenanceType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(150)]
    public string? PerformedBy { get; set; }

    public decimal? Cost { get; set; }

    // Navigation Property
    public Vehicle Vehicle { get; set; } = null!;
}
