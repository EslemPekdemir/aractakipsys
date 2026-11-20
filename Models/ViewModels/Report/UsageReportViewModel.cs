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

    private int? _totalTrips;
    private int? _totalDistanceKm;
    private int? _totalDurationMinutes;
    private int? _distinctVehicleCount;
    private int? _totalCount;

    public int TotalTrips
    {
        get => _totalTrips ?? DistinctAssignments.Count();
        set => _totalTrips = value;
    }

    public int TotalDistanceKm
    {
        get => _totalDistanceKm ?? DistinctAssignments.Sum(x => x.DistanceKm);
        set => _totalDistanceKm = value;
    }

    public int TotalDurationMinutes
    {
        get => _totalDurationMinutes ?? DistinctAssignments.Sum(x => x.DurationMinutes);
        set => _totalDurationMinutes = value;
    }

    public int DistinctVehicleCount
    {
        get => _distinctVehicleCount ?? Items.Select(x => x.PlateNumber).Distinct().Count();
        set => _distinctVehicleCount = value;
    }

    public string TotalDurationFormatted => DurationHelper.FormatDuration(TimeSpan.FromMinutes(TotalDurationMinutes));

    public double AverageDistanceKm => TotalTrips > 0 ? Math.Round((double)TotalDistanceKm / TotalTrips, 1) : 0;

    // Sayfalama Özellikleri
    public int TotalCount
    {
        get => _totalCount ?? Items.Count;
        set => _totalCount = value;
    }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    // Rapor Başlık & Filtre Özet Bilgileri
    public string? SelectedVehicleName { get; set; }
    public string? SelectedDriverName { get; set; }
    public string DateRangeText { get; set; } = string.Empty;
}
