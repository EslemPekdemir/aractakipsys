// File: Models/ViewModels/Assignment/AssignmentDetailsViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentDetailsViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string VehicleColor { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public int VehicleCurrentKm { get; set; }

    public int? DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string DriverPhone { get; set; } = string.Empty;
    public string DriverEmail { get; set; } = string.Empty;
    public string DriverDepartment { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }

    public string DriverLicenseNumber { get => LicenseNumber; set => LicenseNumber = value; }
    public string DriverLicenseClass { get => LicenseClass; set => LicenseClass = value; }
    public DateTime DriverLicenseExpiryDate { get => LicenseExpiryDate; set => LicenseExpiryDate = value; }

    public string RequestedByDepartment { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? Route { get; set; }
    public string Purpose { get; set; } = string.Empty;

    // Şoför Seçim Modları ve Çoklu Şoför
    public bool IsDriverless { get; set; }
    public bool IsExternalDriver { get; set; }
    public string? ExternalDriverFullName { get; set; }
    public string? ExternalDriverDepartment { get; set; }

    public List<AssignedDriverDetailItemViewModel> AssignedDrivers { get; set; } = new();

    // İşlemi Yapan Şoför Bilgileri (Ayrı alanlar - PDF'e basılmaz)
    public string? StartReportedByDriverName { get; set; }
    public string? EndReportedByDriverName { get; set; }
    public string? ChecksCompletedByDriverName { get; set; }

    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public int? StartKm { get; set; }
    public int? EndKm { get; set; }
    public int? TotalDistanceKm => (StartKm.HasValue && EndKm.HasValue) ? (EndKm.Value - StartKm.Value) : null;

    public AssignmentStatus Status { get; set; }
    public string? CancellationReason { get; set; }
    public string? DriverReturnNotes { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? CreatedByUserName { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Şoför Bildirim ve Onay Alanları
    public bool StartReportedBySelf { get; set; }
    public string? StartApprovedByUserId { get; set; }
    public string? StartApprovedByUserName { get; set; }
    public bool EndReportedBySelf { get; set; }
    public string? EndApprovedByUserId { get; set; }
    public string? EndApprovedByUserName { get; set; }

    public bool IsStartApprovalPending => Status != AssignmentStatus.Cancelled && Status != AssignmentStatus.Completed && StartReportedBySelf && string.IsNullOrEmpty(StartApprovedByUserId);
    public bool IsEndApprovalPending => Status != AssignmentStatus.Cancelled && Status != AssignmentStatus.Completed && EndReportedBySelf && string.IsNullOrEmpty(EndApprovedByUserId);

    // Konaklama ve Yevmiye Alanları (Aşama F)
    public int? AccommodationNights { get; set; }
    public decimal? AccommodationCost { get; set; }
    public string? PerDiemType { get; set; }
    public decimal? PerDiemHours { get; set; }
    public decimal? PerDiemRate { get; set; }
    public int? PerDiemDays { get; set; }
    public decimal? PerDiemAmount { get; set; }
    public string? ExpenseNotes { get; set; }

    // Şoför Görev Ücreti ve Çalışma Saati (Şef / Admin Hesaplama)
    public decimal? HourlyRate { get; set; }
    public decimal? CalculatedDutyHours { get; set; }
    public decimal? TotalDriverFee { get; set; }
    public bool HasDriverFeeCalculation => TotalDriverFee.HasValue || HourlyRate.HasValue;
    public decimal EffectiveDriverFee => PerDiemAmount ?? TotalDriverFee ?? 0;
    public decimal TotalTripExpense => EffectiveDriverFee + (AccommodationCost ?? 0);
    public bool HasFinancialCalculation => HasDriverFeeCalculation || HasAccommodationOrPerDiem;

    public bool IsMultiDay => (PlannedEndTime - PlannedStartTime).TotalHours > 24;
    public bool HasAccommodationOrPerDiem => AccommodationNights.HasValue || AccommodationCost.HasValue || PerDiemDays.HasValue || PerDiemHours.HasValue || PerDiemAmount.HasValue || !string.IsNullOrWhiteSpace(ExpenseNotes);

    // İmzalı Belge Alanları (Aşama H)
    public string? SignedDocumentPath { get; set; }
    public string? SignedDocumentOriginalFileName { get; set; }
    public DateTime? SignedDocumentUploadedAt { get; set; }
    public string? SignedDocumentUploadedByUserId { get; set; }
    public string? SignedDocumentUploadedByUserName { get; set; }
    public bool HasSignedDocument => !string.IsNullOrWhiteSpace(SignedDocumentPath);

    // Güzergah Dışı Sevk Alanları (Faz 3)
    public string? OffRouteDispatcherName { get; set; }
    public string? OffRouteDispatcherTitle { get; set; }
    public string? OffRouteReason { get; set; }
    public DateTime? OffRouteReportedAt { get; set; }
    public string? OffRouteReportedByUserName { get; set; }
    public bool HasOffRouteDispatch => !string.IsNullOrWhiteSpace(OffRouteDispatcherName) || !string.IsNullOrWhiteSpace(OffRouteReason);

    // Araç Kontrol Tikleri (Faz 3)
    public bool CheckOil { get; set; }
    public bool CheckWater { get; set; }
    public bool CheckFuel { get; set; }
    public bool CheckHydraulic { get; set; }
    public bool CheckTires { get; set; }
    public DateTime? ChecksCompletedAt { get; set; }
    // Güzergah Değişikliği Tarihçesi (Faz 2 - B)
    public List<AssignmentRouteChangeItemViewModel> RouteChanges { get; set; } = new();
    public bool HasRouteChanges => RouteChanges.Any();

    public string StatusBadgeClass => Status switch
    {
        AssignmentStatus.Planned => "bg-secondary",
        AssignmentStatus.InProgress => "bg-primary",
        AssignmentStatus.Completed => "bg-success",
        AssignmentStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusDisplayName => Status switch
    {
        AssignmentStatus.Planned => "Planlandı",
        AssignmentStatus.InProgress => "Devam Ediyor",
        AssignmentStatus.Completed => "Tamamlandı",
        AssignmentStatus.Cancelled => "İptal Edildi",
        _ => Status.ToString()
    };
}

public class AssignmentCancelViewModel
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string DriverFullName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
}

public class AssignedDriverDetailItemViewModel
{
    public int AssignmentDriverId { get; set; }
    public int DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DriverFullName { get => FullName; set => FullName = value; }
    public string? Phone { get; set; }
    public string? DriverPhone { get => Phone; set => Phone = value; }
    public string? Email { get; set; }
    public string? DriverEmail { get => Email; set => Email = value; }
    public string? Department { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseClass { get; set; }
    public DateTime LicenseExpiryDate { get; set; }
    public bool IsPrimary { get; set; }
}

