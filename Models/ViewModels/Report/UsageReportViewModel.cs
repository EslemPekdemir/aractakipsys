// File: Models/ViewModels/Report/UsageReportViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public class UsageReportViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public List<UsageReportItemViewModel> Items { get; set; } = new();

    // Özet Metrikler
    public int TotalTrips => Items.Count;
    public int TotalDistanceKm => Items.Sum(x => x.DistanceKm);
    public double AverageDistanceKm => TotalTrips > 0 ? Math.Round((double)TotalDistanceKm / TotalTrips, 1) : 0;

    // Rapor Başlık & Filtre Özet Bilgileri
    public string? SelectedVehicleName { get; set; }
    public string? SelectedDriverName { get; set; }
    public string DateRangeText { get; set; } = string.Empty;
}
