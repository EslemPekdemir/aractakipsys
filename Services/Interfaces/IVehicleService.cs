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
    Task<(bool Success, string Message)> CorrectVehicleKmAsync(int vehicleId, int correctKm, string reason, string userId);

    // Araç Belgeleri (Faz 5-A)
    Task<VehicleDocumentsPageViewModel> GetVehicleDocumentsPageAsync(int? vehicleId, string? activeTab);
    Task<(bool Success, string Message)> AddVehicleDocumentAsync(VehicleDocumentCreateViewModel model, string userId);
    Task<(bool Success, string Message)> UpdateVehicleDocumentAsync(int id, VehicleDocumentCreateViewModel model, string userId);
    Task<(bool Success, string Message)> DeleteVehicleDocumentAsync(int id, string userId);

    // Araç Lastik Yönetimi (Faz 5-B)
    Task<(bool Success, string Message)> AddVehicleTireAsync(VehicleTireCreateViewModel model, string userId);
    Task<(bool Success, string Message)> UpdateVehicleTireAsync(int id, VehicleTireCreateViewModel model, string userId);
    Task<(bool Success, string Message)> DeleteVehicleTireAsync(int id, string userId);
    Task<List<VehicleTireItemViewModel>> GetAllCurrentTiresReportAsync();
}
