// File: Models/ViewModels/Maintenance/MaintenanceDetailsViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Maintenance;

public class MaintenanceDetailsViewModel
{
    public int Id { get; set; }
    
    // Araç Bilgileri
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string VehicleColor { get; set; } = string.Empty;
    public int VehicleCurrentKm { get; set; }
    public VehicleStatus VehicleStatus { get; set; }

    // Bakım Bilgileri
    public string MaintenanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PerformedBy { get; set; }
    public decimal? Cost { get; set; }

    public bool IsOngoing => !EndDate.HasValue;

    public int DurationDays => EndDate.HasValue
        ? Math.Max(1, (int)(EndDate.Value.Date - StartDate.Date).TotalDays)
        : Math.Max(1, (int)(DateTime.Today - StartDate.Date).TotalDays);

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }

    public string StatusBadgeClass => IsOngoing ? "bg-warning text-dark" : "bg-success text-white";
    public string StatusDisplayName => IsOngoing ? "Devam Ediyor" : "Tamamlandı";
}
