// File: Models/ViewModels/Report/MaintenanceReportItemViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public class MaintenanceReportItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PerformedBy { get; set; }

    /// <summary>
    /// Bakımın devam edip etmediği
    /// </summary>
    public bool IsOngoing => !EndDate.HasValue;

    /// <summary>
    /// Serviste geçirilen gün sayısı
    /// </summary>
    public int DurationDays
    {
        get
        {
            var end = EndDate ?? DateTime.Today;
            var days = (int)(end.Date - StartDate.Date).TotalDays;
            return days < 1 ? 1 : days;
        }
    }
}
