// File: Models/Entities/DriverLicense.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

public class DriverLicense : BaseAuditableEntity
{
    [Required]
    public int DriverId { get; set; }

    [Required]
    [StringLength(10)]
    public string LicenseClass { get; set; } = string.Empty;

    [Required]
    public DateTime LicenseExpiryDate { get; set; }

    // Navigation Property
    public Driver Driver { get; set; } = null!;
}
