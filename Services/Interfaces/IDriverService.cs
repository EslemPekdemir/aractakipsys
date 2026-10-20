// File: Services/Interfaces/IDriverService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels;
using aractakipsys.Models.ViewModels.Driver;
using aractakipsys.Models.ViewModels.Surucu;

namespace aractakipsys.Services.Interfaces;

public interface IDriverService
{
    // ==========================================
    // 1. ŞOFÖR MOBİL PANELİ METOTLARI
    // ==========================================
    Task<DriverAssignmentListViewModel> GetDriverAssignmentsAsync(string userId);
    Task<DriverAssignmentDetailViewModel?> GetAssignmentDetailAsync(int assignmentId, string userId);
    Task<AjaxResultViewModel> StartAssignmentAsync(int assignmentId, string userId, StartAssignmentRequest request);
    Task<AjaxResultViewModel> EndAssignmentAsync(int assignmentId, string userId, EndAssignmentRequest request);
    Task<AjaxResultViewModel> CreateSelfAssignmentAsync(string userId, EmergencySelfAssignmentRequest request);

    // ==========================================
    // 2. ADMIN ŞOFÖR YÖNETİMİ (CRUD) METOTLARI
    // ==========================================
    Task<DriverFilterViewModel> GetDriversPagedAsync(DriverFilterViewModel filter);
    Task<Driver?> GetDriverByIdAsync(int id);
    Task<DriverEditViewModel?> GetDriverEditModelAsync(int id);
    Task<DriverDetailsViewModel?> GetDriverDetailsAsync(int id);
    Task<(bool Success, string Message, int DriverId)> CreateDriverAsync(DriverCreateViewModel model, string adminUserId);
    Task<(bool Success, string Message)> UpdateDriverAsync(DriverEditViewModel model, string adminUserId);
    Task<(bool Success, string Message)> ToggleDriverStatusAsync(int id, string adminUserId);
    Task<(bool Success, string Message)> DeleteDriverAsync(int id, string adminUserId);
    Task<(bool Success, string Message)> ResetDriverPasswordAsync(int driverId, string newPassword, string adminUserId);
}
