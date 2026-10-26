// File: Models/ViewModels/Report/InsuranceInspectionReportFilterViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public enum DocumentExpiryFilterOption
{
    All = 0,            // Tümü
    Expired = 1,        // Sadece Süresi Geçenler
    ExpiringSoon = 2,   // Sadece 30 Gün İçinde Dolacaklar
    CustomDate = 3      // Özel Tarih Aralığı
}

public enum ReportScopeType
{
    All = 0,            // Tümü (Muayene, Sigorta, Kasko)
    Insurance = 1,      // Sadece Trafik Sigortası
    Inspection = 2,     // Sadece Fenni Muayene
    Kasko = 3           // Sadece Kasko
}

public class InsuranceInspectionReportFilterViewModel
{
    public DocumentExpiryFilterOption FilterOption { get; set; } = DocumentExpiryFilterOption.All;
    public ReportScopeType Scope { get; set; } = ReportScopeType.All;
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Tek araç filtresi (null ise tüm araçlar)
    /// </summary>
    public int? VehicleId { get; set; }

    /// <summary>
    /// Filtre ekranında araç seçimi için dropdown listesi
    /// </summary>
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> VehicleList { get; set; } = new();

    /// <summary>
    /// Özel tarih aralığı başlangıç
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Özel tarih aralığı bitiş
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Hangi tarihe göre filtreleneceği ("ExpiryDate" = Bitiş Tarihi, "StartDate" = Başlangıç Tarihi)
    /// </summary>
    public string DateTarget { get; set; } = "ExpiryDate";

    /// <summary>
    /// Sayfalama: Mevcut sayfa numarası (varsayılan 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Sayfalama: Sayfa başına kayıt sayısı (varsayılan 10)
    /// </summary>
    public int PageSize { get; set; } = 10;
}
