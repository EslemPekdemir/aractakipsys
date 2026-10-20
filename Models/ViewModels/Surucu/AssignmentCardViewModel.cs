// File: Models/ViewModels/Surucu/AssignmentCardViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Surucu;

public class AssignmentCardViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehiclePlate { get => PlateNumber; set => PlateNumber = value; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string VehicleBrandModel { get => $"{Brand} {Model}".Trim(); set { var parts = value.Split(' ', 2); Brand = parts[0]; Model = parts.Length > 1 ? parts[1] : ""; } }
    public string VehicleType { get; set; } = string.Empty;
    public int CurrentVehicleKm { get; set; }
    public int VehicleCurrentKm { get => CurrentVehicleKm; set => CurrentVehicleKm = value; }
    public string? RequestedByDepartment { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int? StartKm { get; set; }
    public int? EndKm { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? DriverReturnNotes { get; set; }

    // Şoför Bildirimi ve Onay Alanları
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
