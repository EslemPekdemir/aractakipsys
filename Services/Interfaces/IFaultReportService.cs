// File: Services/Interfaces/IFaultReportService.cs
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels;
using aractakipsys.Models.ViewModels.FaultReport;

namespace aractakipsys.Services.Interfaces;

public interface IFaultReportService
{
    Task<List<FaultReportItemViewModel>> GetFaultReportsAsync(FaultReportStatus? status = null, int? vehicleId = null);
    Task<FaultReportItemViewModel?> GetFaultReportByIdAsync(int id);
    Task<AjaxResultViewModel> CreateFaultReportAsync(FaultReportCreateRequest request, string userId);
    Task<AjaxResultViewModel> ApproveFaultReportAsync(FaultReportApproveRequest request, string reviewerUserId);
    Task<AjaxResultViewModel> UpdateStatusAsync(int id, FaultReportStatus status, string? notes, string reviewerUserId);
    Task<FaultReportPdfModel?> GetFaultReportPdfModelAsync(int id);
    Task<FaultReportPdfModel> GetBlankFaultReportPdfModelAsync(string reviewerFullName = "Ulaştırma Amiri");
    Task<int> GetPendingFaultCountAsync();
    Task<List<FaultReportItemViewModel>> GetLatestPendingFaultsAsync(int take = 5);
}
