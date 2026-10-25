// File: Models/Entities/Vehicle.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.Entities;

public class Vehicle : BaseAuditableEntity
{
    [Required]
    [StringLength(15)]
    public string PlateNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [Range(1990, 2100)]
    public int Year { get; set; }

    [Required]
    [StringLength(30)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string VehicleType { get; set; } = "Sedan";

    [Required]
    [StringLength(50)]
    public string FuelType { get; set; } = "Dizel";

    [Required]
    [Range(0, int.MaxValue)]
    public int CurrentKm { get; set; } = 0;

    [Required]
    public VehicleStatus Status { get; set; } = VehicleStatus.Idle;

    [Required]
    public DateTime InspectionStartDate { get; set; }

    [Required]
    public DateTime InspectionExpiryDate { get; set; }

    [Required]
    public DateTime InsuranceStartDate { get; set; }

    [Required]
    public DateTime InsuranceExpiryDate { get; set; }

    [StringLength(50)]
    public string? TireBrand { get; set; }

    public int? TireYear { get; set; }

    [StringLength(50)]
    public string? TireSeason { get; set; }

    [StringLength(1000)]
    public string? FineNotes { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation Properties
    public ICollection<VehicleDocument> Documents { get; set; } = new List<VehicleDocument>();
    public ICollection<VehicleTire> Tires { get; set; } = new List<VehicleTire>();

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string StatusDisplayName => Status switch
    {
        VehicleStatus.Idle => "Boşta",
        VehicleStatus.OnDuty => "Görevde",
        VehicleStatus.Maintenance => "Bakımda",
        VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
        VehicleStatus.OutOfService => "Hizmet Dışı",
        _ => Status.ToString()
    };

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string StatusBadgeClass => Status switch
    {
        VehicleStatus.Idle => "badge-success bg-success text-white",
        VehicleStatus.OnDuty => "badge-primary bg-primary text-white",
        VehicleStatus.Maintenance => "badge-warning bg-warning text-dark",
        VehicleStatus.DocumentExpired => "badge-danger bg-danger text-white",
        VehicleStatus.OutOfService => "badge-secondary bg-secondary text-white",
        _ => "badge-secondary"
    };
}

