// File: Services/Interfaces/IAssignmentService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.Assignment;

namespace aractakipsys.Services.Interfaces;

public interface IAssignmentService
{
    Task<AssignmentFilterViewModel> GetAssignmentsPagedAsync(AssignmentFilterViewModel filter);
    Task<Assignment?> GetAssignmentByIdAsync(int id);
    Task<AssignmentDetailsViewModel?> GetAssignmentDetailsAsync(int id);
    Task<AssignmentEditViewModel?> GetAssignmentEditModelAsync(int id);
    Task<AssignmentCreateViewModel> GetCreateViewModelAsync(DateTime? startTime = null, DateTime? endTime = null);
    Task<AssignmentEditViewModel> PopulateEditDropdownsAsync(AssignmentEditViewModel model);

    // Müsaitlik ve Çakışma Kontrolleri (AJAX & Validation)
    Task<(bool IsAvailable, string? Reason, int? ConflictingAssignmentId)> CheckVehicleAvailabilityAsync(
        int vehicleId, DateTime start, DateTime end, int? excludeAssignmentId = null);

    Task<(bool IsAvailable, string? Reason, int? ConflictingAssignmentId)> CheckDriverAvailabilityAsync(
        int driverId, DateTime start, DateTime end, int? excludeAssignmentId = null);

    // CRUD Operasyonları
    Task<(bool Success, string Message, int AssignmentId)> CreateAssignmentAsync(AssignmentCreateViewModel model, string adminUserId);
    Task<(bool Success, string Message)> UpdateAssignmentAsync(AssignmentEditViewModel model, string adminUserId);
    Task SyncAssignmentDriversAsync(Assignment assignment, List<int>? driverIds, bool isDriverless, bool isExternalDriver, string userId);
    Task<(bool Success, string Message)> ExtendAssignmentDurationAsync(int id, DateTime newPlannedEndTime, string? reason, string adminUserId);
    Task<(bool Success, string Message)> CancelAssignmentAsync(int id, string cancellationReason, string adminUserId);

    // Şef / Admin Onay İşlemleri (Şoför Bildirimi Onayı)
    Task<(bool Success, string Message)> ApproveStartAsync(int assignmentId, string approverUserId);
    Task<(bool Success, string Message)> ApproveEndAsync(int assignmentId, string approverUserId);
    Task<(bool Success, string Message)> ApproveEndAsync(AssignmentApproveEndViewModel model, string approverUserId);

    // Yetkili Tarafından Doğrudan Başlatma / Bitirme (Şoför Bildirimi Olmaksızın)
    Task<(bool Success, string Message)> StartAssignmentDirectAsync(int assignmentId, string userId, int? startKm = null, bool confirmKmDifference = false);
    Task<(bool Success, string Message)> EndAssignmentDirectAsync(int assignmentId, string userId, int endKm, string? driverReturnNotes = null, bool confirmKmDifference = false);

    // Harcırah & Maliyet Bilgilerini Sonradan Revize Etme (Fatura / Muhasebe Düzeltmesi)
    Task<(bool Success, string Message)> UpdateFinancialDetailsAsync(AssignmentFinancialRevisionViewModel model, string userId);

    // İmzalı Belge İşlemleri (Aşama H)
    Task<(bool Success, string? ErrorMessage)> UploadSignedDocumentAsync(int assignmentId, Microsoft.AspNetCore.Http.IFormFile file, string userId);
    Task<(byte[]? FileContents, string? ContentType, string? OriginalFileName)?> GetSignedDocumentFileAsync(int assignmentId);
    Task<SignedDocumentArchiveViewModel> GetSignedDocumentArchiveAsync(SignedDocumentArchiveFilterViewModel filter);

    // Acil Güzergah Dışı Sevk Kaydı (Faz 3)
    Task<(bool Success, string Message)> SaveOffRouteDispatchAsync(OffRouteDispatchViewModel model, string userId);
}
