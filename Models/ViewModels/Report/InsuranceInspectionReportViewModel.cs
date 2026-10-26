// File: Models/ViewModels/Report/InsuranceInspectionReportViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public class InsuranceInspectionReportViewModel
{
    public InsuranceInspectionReportFilterViewModel Filter { get; set; } = new();
    public List<InsuranceInspectionReportItemViewModel> Items { get; set; } = new();

    public int TotalVehiclesCount { get; set; }
    public int ExpiredCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ValidCount { get; set; }

    // Maliyet Toplamları (Faz 7-A)
    public decimal TotalInsuranceCost => Items.Where(x => x.InsuranceCost.HasValue).Sum(x => x.InsuranceCost!.Value);
    public decimal TotalInspectionCost => Items.Where(x => x.InspectionCost.HasValue).Sum(x => x.InspectionCost!.Value);
    public decimal TotalKaskoCost => Items.Where(x => x.KaskoCost.HasValue).Sum(x => x.KaskoCost!.Value);
    public decimal TotalCost => TotalInsuranceCost + TotalInspectionCost + TotalKaskoCost;

    // Sayfalama (Pagination) Bilgileri
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public string FilterOptionDisplayName => Filter.FilterOption switch
    {
        DocumentExpiryFilterOption.Expired => "Sadece Süresi Geçenler",
        DocumentExpiryFilterOption.ExpiringSoon => "Sadece 30 Gün İçinde Dolacaklar",
        DocumentExpiryFilterOption.CustomDate => GetCustomDateDisplayName(),
        _ => "Tümü (Tüm Filo)"
    };

    public string ScopeDisplayName => Filter.Scope switch
    {
        ReportScopeType.Insurance => "Sadece Trafik Sigortası",
        ReportScopeType.Inspection => "Sadece Fenni Muayene",
        ReportScopeType.Kasko => "Sadece Kasko",
        _ => "Tümü (Muayene, Sigorta, Kasko)"
    };

    private string GetCustomDateDisplayName()
    {
        var targetName = Filter.DateTarget == "StartDate" ? "Başlangıç" : "Bitiş (Geçerlilik)";
        if (Filter.StartDate.HasValue && Filter.EndDate.HasValue)
        {
            return $"Özel Tarih ({targetName}: {Filter.StartDate.Value:dd.MM.yyyy} - {Filter.EndDate.Value:dd.MM.yyyy})";
        }
        if (Filter.StartDate.HasValue)
        {
            return $"Özel Tarih ({targetName} >= {Filter.StartDate.Value:dd.MM.yyyy})";
        }
        if (Filter.EndDate.HasValue)
        {
            return $"Özel Tarih ({targetName} <= {Filter.EndDate.Value:dd.MM.yyyy})";
        }
        return "Özel Tarih Aralığı";
    }
}
