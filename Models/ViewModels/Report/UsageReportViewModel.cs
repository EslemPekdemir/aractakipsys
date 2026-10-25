// File: Models/ViewModels/Report/UsageReportViewModel.cs
using aractakipsys.Common.Helpers;

namespace aractakipsys.Models.ViewModels.Report;

public class UsageReportViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public List<UsageReportItemViewModel> Items { get; set; } = new();

    /// <summary>
    /// Şoför bazında gruplanmış kayıtlar ve alt toplamlar
    /// </summary>
    public List<UsageReportDriverGroupViewModel> DriverGroups { get; set; } = new();

    // Rapor Sonu Genel Toplamlar:
    // İş Kuralı (Faz 7-A): Çoklu şoförlü görevde aynı görev birden fazla şoförün altında yer alabilir (şoför bazlı alt toplamlarda),
    // ANCAK RAPORUN SONUNDAKİ GENEL TOPLAM her görevi YALNIZCA BİR KEZ saymalıdır (çift sayım engellenir).
    public IEnumerable<UsageReportItemViewModel> DistinctAssignments => Items
        .GroupBy(x => x.AssignmentId)
        .Select(g => g.First());

    public int TotalTrips => DistinctAssignments.Count();
    public int TotalDistanceKm => DistinctAssignments.Sum(x => x.DistanceKm);
    public int TotalDurationMinutes => DistinctAssignments.Sum(x => x.DurationMinutes);
    public string TotalDurationFormatted => DurationHelper.FormatDuration(TimeSpan.FromMinutes(TotalDurationMinutes));

    public double AverageDistanceKm => TotalTrips > 0 ? Math.Round((double)TotalDistanceKm / TotalTrips, 1) : 0;

    // Rapor Başlık & Filtre Özet Bilgileri
    public string? SelectedVehicleName { get; set; }
    public string? SelectedDriverName { get; set; }
    public string DateRangeText { get; set; } = string.Empty;
}
