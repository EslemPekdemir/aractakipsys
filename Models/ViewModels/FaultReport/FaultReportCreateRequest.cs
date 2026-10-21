// File: Models/ViewModels/FaultReport/FaultReportCreateRequest.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.FaultReport;

public class FaultReportCreateRequest
{
    [Required(ErrorMessage = "Lütfen aracı seçiniz.")]
    public int VehicleId { get; set; }

    public int? AssignmentId { get; set; }

    [Required(ErrorMessage = "Lütfen güncel kilometreyi giriniz.")]
    [Range(0, int.MaxValue, ErrorMessage = "Geçerli bir kilometre değeri giriniz.")]
    public int CurrentKm { get; set; }

    [Required(ErrorMessage = "Lütfen arıza açıklamasını detaylıca belirtiniz.")]
    [StringLength(1000, ErrorMessage = "Arıza açıklaması en fazla 1000 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;
}
