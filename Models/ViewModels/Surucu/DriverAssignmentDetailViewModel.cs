// File: Models/ViewModels/Surucu/DriverAssignmentDetailViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Surucu;

public class DriverAssignmentDetailViewModel
{
    public int AssignmentId { get; set; }
    public int Id { get => AssignmentId; set => AssignmentId = value; }

    // Araç Bilgileri
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleBrandModel { get => $"{VehicleBrand} {VehicleModel}".Trim(); set { var parts = value.Split(' ', 2); VehicleBrand = parts[0]; VehicleModel = parts.Length > 1 ? parts[1] : ""; } }
    public string VehicleColor { get; set; } = string.Empty;
    public int VehicleYear { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public string VehicleFuelType { get; set; } = string.Empty;
    public string FuelType { get => VehicleFuelType; set => VehicleFuelType = value; }
    public int CurrentVehicleKm { get; set; }
    public int VehicleCurrentKm { get => CurrentVehicleKm; set => CurrentVehicleKm = value; }
    public VehicleStatus VehicleStatus { get; set; }
    public string VehicleStatusStr { get => VehicleStatus.ToTurkishString(); set { } }

    // Görev Bilgileri
    public string Purpose { get; set; } = string.Empty;
    public string? RequestedByDepartment { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int? StartKm { get; set; }
    public int? EndKm { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? CancellationReason { get; set; }
    public string? DriverReturnNotes { get; set; }

    // Şoför Bildirim ve Onay Durumu
    public bool StartReportedBySelf { get; set; }
    public string? StartApprovedByUserId { get; set; }
    public bool EndReportedBySelf { get; set; }
    public string? EndApprovedByUserId { get; set; }

    public bool IsStartApprovalPending => Status != AssignmentStatus.Cancelled && Status != AssignmentStatus.Completed && StartReportedBySelf && string.IsNullOrEmpty(StartApprovedByUserId);
    public bool IsEndApprovalPending => Status != AssignmentStatus.Cancelled && Status != AssignmentStatus.Completed && EndReportedBySelf && string.IsNullOrEmpty(EndApprovedByUserId);

    public string StatusBadgeClass
    {
        get
        {
            if (IsEndApprovalPending || IsStartApprovalPending) return "badge-warning text-white";
            return Status switch
            {
                AssignmentStatus.Planned => "bg-secondary",
                AssignmentStatus.InProgress => "bg-primary",
                AssignmentStatus.Completed => "bg-success",
                AssignmentStatus.Cancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }
    }

    public string StatusDisplayName
    {
        get
        {
            if (IsEndApprovalPending) return "Teslim Edildi (Onay Bekliyor)";
            if (IsStartApprovalPending) return "Başlangıç Bildirildi (Onay Bekliyor)";
            return Status switch
            {
                AssignmentStatus.Planned => "Planlandı",
                AssignmentStatus.InProgress => "Devam Ediyor",
                AssignmentStatus.Completed => "Tamamlandı",
                AssignmentStatus.Cancelled => "İptal Edildi",
                _ => Status.ToString()
            };
        }
    }
}
