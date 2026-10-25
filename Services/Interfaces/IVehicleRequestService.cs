// File: Services/Interfaces/IVehicleRequestService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.VehicleRequest;

namespace aractakipsys.Services.Interfaces;

public interface IVehicleRequestService
{
    // Public işlemler
    Task<(bool Success, string Message, int RequestId)> CreateRequestAsync(VehicleRequestCreateViewModel model);
    Task<VehicleRequest?> GetRequestByIdAsync(int id);

    // Admin / Şef yönetim işlemleri
    Task<VehicleRequestFilterViewModel> GetRequestsPagedAsync(VehicleRequestFilterViewModel filter);
    Task<VehicleRequestFulfillViewModel?> GetFulfillViewModelAsync(int requestId, DateTime? startTime = null, DateTime? endTime = null);
    Task<(bool Success, string Message, int AssignmentId)> FulfillRequestAsync(VehicleRequestFulfillViewModel model, string adminUserId);
    Task<(bool Success, string Message)> RejectRequestAsync(int requestId, string rejectionReason, string adminUserId);
    Task<PendingNotificationSummaryViewModel> GetPendingNotificationSummaryAsync();

    // Talep Seçenekleri (Admin Yönetimi)
    Task<RequestOptionsPageViewModel> GetRequestOptionsAsync();
    Task<(bool Success, string Message)> SaveDepartmentAsync(RequestDepartmentCreateOrEditViewModel model, string adminUserId);
    Task<(bool Success, string Message)> ToggleDepartmentActiveAsync(int id, string adminUserId);
    Task<(bool Success, string Message)> SaveVehicleTypeAsync(RequestVehicleTypeCreateOrEditViewModel model, string adminUserId);
    Task<(bool Success, string Message)> ToggleVehicleTypeActiveAsync(int id, string adminUserId);
    Task<List<string>> GetActiveDepartmentsAsync();
    Task<List<string>> GetActiveVehicleTypesAsync();
}
