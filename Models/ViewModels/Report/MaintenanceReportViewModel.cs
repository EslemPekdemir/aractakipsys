// File: Models/ViewModels/Report/MaintenanceReportViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public class MaintenanceReportViewModel
{
    public MaintenanceReportFilterViewModel Filter { get; set; } = new();
    public List<MaintenanceReportItemViewModel> Items { get; set; } = new();

    private int? _totalMaintenances;
    private int? _totalDaysInService;
    private int? _ongoingCount;
    private int? _completedCount;
    private decimal? _totalCost;
    private int? _totalCount;

    // Özet KPI Metrikleri (Sayfalamadan bağımsız tüm filtrelenmiş kayıtların toplamları)
    public int TotalMaintenances
    {
        get => _totalMaintenances ?? Items.Count;
        set => _totalMaintenances = value;
    }

    public int TotalDaysInService
    {
        get => _totalDaysInService ?? Items.Sum(x => x.DurationDays);
        set => _totalDaysInService = value;
    }

    public int OngoingCount
    {
        get => _ongoingCount ?? Items.Count(x => x.IsOngoing);
        set => _ongoingCount = value;
    }

    public int CompletedCount
    {
        get => _completedCount ?? Items.Count(x => !x.IsOngoing);
        set => _completedCount = value;
    }

    // Maliyet Metrikleri (Faz 7-A: Maliyeti boş olan satırlar toplamda sayılmaz)
    public decimal TotalCost
    {
        get => _totalCost ?? Items.Where(x => x.Cost.HasValue).Sum(x => x.Cost!.Value);
        set => _totalCost = value;
    }
    public string TotalCostFormatted => $"{TotalCost:N2} ₺";

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
    public string DateRangeText { get; set; } = string.Empty;
}
