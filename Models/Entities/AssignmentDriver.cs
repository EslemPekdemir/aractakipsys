// File: Models/Entities/AssignmentDriver.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

public class AssignmentDriver : BaseAuditableEntity
{
    [Required]
    public int AssignmentId { get; set; }

    [Required]
    public int DriverId { get; set; }

    public bool IsPrimary { get; set; } = false;

    // Navigation Properties
    public Assignment Assignment { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}
