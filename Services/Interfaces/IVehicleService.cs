// File: Services/Interfaces/IVehicleService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.Vehicle;

namespace aractakipsys.Services.Interfaces;

public interface IVehicleService
{
    Task<VehicleFilterViewModel> GetVehiclesPagedAsync(VehicleFilterViewModel filter);
    Task<Vehicle?> GetVehicleByIdAsync(int id);
    Task<VehicleEditViewModel?> GetVehicleEditModelAsync(int id);
    Task<VehicleDetailsViewModel?> GetVehicleDetailsAsync(int id);
    Task<(bool Success, string Message, int VehicleId)> CreateVehicleAsync(VehicleCreateViewModel model, string userId);
    Task<(bool Success, string Message)> UpdateVehicleAsync(VehicleEditViewModel model, string userId);
    Task<(bool Success, string Message)> DeleteVehicleAsync(int id, string userId);
    Task<DocumentExpiryNotificationSummaryViewModel> GetExpiringDocumentsSummaryAsync();
}
