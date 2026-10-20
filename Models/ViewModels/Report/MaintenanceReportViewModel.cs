// File: Models/ViewModels/Report/MaintenanceReportViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public class MaintenanceReportViewModel
{
    public MaintenanceReportFilterViewModel Filter { get; set; } = new();
    public List<MaintenanceReportItemViewModel> Items { get; set; } = new();

    // Özet KPI Metrikleri
    public int TotalMaintenances => Items.Count;
    public int TotalDaysInService => Items.Sum(x => x.DurationDays);
    public int OngoingCount => Items.Count(x => x.IsOngoing);
    public int CompletedCount => Items.Count(x => !x.IsOngoing);

    // Rapor Başlık & Filtre Özet Bilgileri
    public string? SelectedVehicleName { get; set; }
    public string DateRangeText { get; set; } = string.Empty;
}
