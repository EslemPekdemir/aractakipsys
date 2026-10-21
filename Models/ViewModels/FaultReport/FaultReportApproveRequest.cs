// File: Models/ViewModels/FaultReport/FaultReportApproveRequest.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.FaultReport;

public class FaultReportApproveRequest
{
    public int Id { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [StringLength(100)]
    public string? DriverFullName { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int CurrentKm { get; set; }

    [Required]
    public DateTime ReportDate { get; set; }

    [Required(ErrorMessage = "Arıza açıklaması boş bırakılamaz.")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ReviewerNotes { get; set; }
}
