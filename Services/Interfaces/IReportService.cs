// File: Services/Interfaces/IReportService.cs
using aractakipsys.Models.ViewModels.Report;

namespace aractakipsys.Services.Interfaces;

public interface IReportService
{
    /// <summary>
    /// Filtre formundaki Select2 dropdown seçeneklerini (Araçlar ve Şoförler) hazırlar.
    /// </summary>
    Task<ReportFilterViewModel> PrepareFilterOptionsAsync(ReportFilterViewModel? filter = null);

    /// <summary>
    /// Belirtilen filtrelere göre sadece tamamlanmış (Completed) seferleri getirir ve kullanım raporunu üretir.
    /// </summary>
    Task<UsageReportViewModel> GetUsageReportAsync(ReportFilterViewModel filter, bool paged = true);

    /// <summary>
    /// Bakım raporu filtre seçeneklerini hazırlar.
    /// </summary>
    Task<MaintenanceReportFilterViewModel> PrepareMaintenanceFilterOptionsAsync(MaintenanceReportFilterViewModel? filter = null);

    /// <summary>
    /// Belirtilen filtrelere göre araçların bakım ve tamir geçmişi raporunu üretir.
    /// </summary>
    Task<MaintenanceReportViewModel> GetMaintenanceReportAsync(MaintenanceReportFilterViewModel filter, bool paged = true);

    /// <summary>
    /// Filodaki araçların sigorta ve muayene bitiş tarihlerine göre durum raporunu üretir.
    /// </summary>
    Task<InsuranceInspectionReportViewModel> GetInsuranceInspectionReportAsync(InsuranceInspectionReportFilterViewModel filter, bool paged = true);

    /// <summary>
    /// Talep eden birimler bazında araç talep istatistiği raporunu üretir (Faz 7-B).
    /// </summary>
    Task<VehicleRequestReportViewModel> GetVehicleRequestReportAsync(VehicleRequestReportFilterViewModel filter, bool paged = true);
}
