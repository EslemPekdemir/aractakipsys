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
    All = 0,            // Tümü (Sigorta ve Muayene)
    Insurance = 1,      // Sadece Trafik Sigortası
    Inspection = 2      // Sadece Fenni Muayene
}

public class InsuranceInspectionReportFilterViewModel
{
    public DocumentExpiryFilterOption FilterOption { get; set; } = DocumentExpiryFilterOption.All;
    public ReportScopeType Scope { get; set; } = ReportScopeType.All;
    public string? SearchTerm { get; set; }

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
