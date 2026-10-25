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
    public decimal? Cost { get; set; }

    /// <summary>
    /// Maliyet gösterim metni (boş ise -, dolu ise Türk Lirası)
    /// </summary>
    public string CostDisplay => Cost.HasValue ? $"{Cost.Value:N2} ₺" : "-";

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
