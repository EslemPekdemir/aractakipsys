// File: Models/ViewModels/FaultReport/FaultReportItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.FaultReport;

public class FaultReportItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public int? DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public int? AssignmentId { get; set; }
    public int CurrentKm { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public FaultReportStatus Status { get; set; }
    public string StatusDisplayName => Status.ToTurkishString();
    public string? ReviewerUserId { get; set; }
    public string? ReviewerFullName { get; set; }
    public string? ReviewerNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public string StatusBadgeClass => Status switch
    {
        FaultReportStatus.Pending => "badge-warning",
        FaultReportStatus.Approved => "badge-primary",
        FaultReportStatus.Resolved => "badge-success",
        FaultReportStatus.Rejected => "badge-danger",
        _ => "badge-secondary"
    };
}
