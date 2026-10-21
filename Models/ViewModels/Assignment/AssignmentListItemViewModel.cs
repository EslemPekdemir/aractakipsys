// File: Models/ViewModels/Assignment/AssignmentListItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentListItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public int VehicleCurrentKm { get; set; }
    public int? DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public List<string> AssignedDriverNames { get; set; } = new();
    public int AdditionalDriverCount => AssignedDriverNames.Count > 1 ? AssignedDriverNames.Count - 1 : 0;

    public bool IsDriverless { get; set; }
    public bool IsExternalDriver { get; set; }
    public string? ExternalDriverFullName { get; set; }
    public string? ExternalDriverDepartment { get; set; }

    public string DriverDisplayName
    {
        get
        {
            if (IsDriverless) return "Şoförsüz (Personel)";
            if (IsExternalDriver) return !string.IsNullOrWhiteSpace(ExternalDriverFullName) ? $"{ExternalDriverFullName} ({ExternalDriverDepartment})" : "Dış Şoför";
            if (AssignedDriverNames.Count > 1) return string.Join(", ", AssignedDriverNames);
            if (!string.IsNullOrWhiteSpace(DriverFullName)) return DriverFullName;
            return "Atanmadı";
        }
    }

    public string RequestedByDepartment { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? Route { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int? StartKm { get; set; }
    public int? EndKm { get; set; }
    public AssignmentStatus Status { get; set; }

    // Şoför Bildirim ve Onay Durumu (ARAYUZ-SPESIFIKASYONU & Onay Akışı)
    public bool StartReportedBySelf { get; set; }
    public string? StartApprovedByUserId { get; set; }
    public bool EndReportedBySelf { get; set; }
    public string? EndApprovedByUserId { get; set; }

    public bool IsStartApprovalPending => Status != AssignmentStatus.Cancelled && Status != AssignmentStatus.Completed && StartReportedBySelf && string.IsNullOrEmpty(StartApprovedByUserId);
    public bool IsEndApprovalPending => Status != AssignmentStatus.Cancelled && Status != AssignmentStatus.Completed && EndReportedBySelf && string.IsNullOrEmpty(EndApprovedByUserId);

    // Şoför Görev Ücreti ve Çalışma Saati (Şef / Admin Görünümü)
    public decimal? HourlyRate { get; set; }
    public decimal? CalculatedDutyHours { get; set; }
    public decimal? TotalDriverFee { get; set; }

    // Konaklama ve Yevmiye Alanları (Aşama F)
    public int? AccommodationNights { get; set; }
    public decimal? AccommodationCost { get; set; }
    public string? PerDiemType { get; set; }
    public decimal? PerDiemHours { get; set; }
    public decimal? PerDiemRate { get; set; }
    public int? PerDiemDays { get; set; }
    public decimal? PerDiemAmount { get; set; }
    public string? ExpenseNotes { get; set; }

    public decimal EffectiveDriverFee => PerDiemAmount ?? TotalDriverFee ?? 0;
    public decimal TotalTripExpense => EffectiveDriverFee + (AccommodationCost ?? 0);
    public bool HasFinancialCalculation => TotalDriverFee.HasValue || HourlyRate.HasValue || AccommodationCost.HasValue || PerDiemAmount.HasValue;

    public int? TotalDistanceKm => (StartKm.HasValue && EndKm.HasValue) ? (EndKm.Value - StartKm.Value) : null;

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
