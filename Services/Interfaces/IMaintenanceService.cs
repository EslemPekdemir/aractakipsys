// File: Services/Interfaces/IMaintenanceService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.Maintenance;

namespace aractakipsys.Services.Interfaces;

public interface IMaintenanceService
{
    Task<MaintenanceFilterViewModel> GetMaintenancesPagedAsync(MaintenanceFilterViewModel filter);
    Task<Maintenance?> GetMaintenanceByIdAsync(int id);
    Task<MaintenanceDetailsViewModel?> GetMaintenanceDetailsAsync(int id);
    Task<MaintenanceCreateViewModel> GetCreateViewModelAsync();
    Task<MaintenanceEditViewModel?> GetEditViewModelAsync(int id);
    Task<(bool Success, string Message, int MaintenanceId)> CreateMaintenanceAsync(MaintenanceCreateViewModel model, string adminUserId);
    Task<(bool Success, string Message)> UpdateMaintenanceAsync(MaintenanceEditViewModel model, string adminUserId);
    Task<(bool Success, string Message)> CompleteMaintenanceAsync(int id, DateTime endDate, string? performedBy, string adminUserId);
    Task<(bool Success, string Message)> DeleteMaintenanceAsync(int id, string adminUserId);
}
