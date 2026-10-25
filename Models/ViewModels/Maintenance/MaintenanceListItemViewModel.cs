// File: Models/ViewModels/Maintenance/MaintenanceListItemViewModel.cs
namespace aractakipsys.Models.ViewModels.Maintenance;

public class MaintenanceListItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PerformedBy { get; set; }
    public decimal? Cost { get; set; }

    public bool IsOngoing => !EndDate.HasValue;

    public int? DurationDays => EndDate.HasValue 
        ? Math.Max(1, (int)(EndDate.Value.Date - StartDate.Date).TotalDays) 
        : (int)(DateTime.Today - StartDate.Date).TotalDays;

    public string StatusBadgeClass => IsOngoing ? "bg-warning text-dark" : "bg-success text-white";
    public string StatusDisplayName => IsOngoing ? "Devam Ediyor" : "Tamamlandı";
}
