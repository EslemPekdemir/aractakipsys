// File: Models/Entities/Driver.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

public class Driver : BaseAuditableEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string LicenseNumber { get; set; } = string.Empty;

    [StringLength(10)]
    public string? LicenseClass { get; set; }

    [Required]
    public DateTime LicenseExpiryDate { get; set; }

    [StringLength(150)]
    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<AssignmentDriver> AssignmentDrivers { get; set; } = new List<AssignmentDriver>();
}
