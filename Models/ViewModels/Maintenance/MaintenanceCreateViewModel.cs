// File: Models/ViewModels/Maintenance/MaintenanceCreateViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Maintenance;

public class MaintenanceCreateViewModel
{
    [Required(ErrorMessage = "Araç seçimi zorunludur.")]
    [Display(Name = "Bakıma Alınacak Araç")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Bakım türü zorunludur.")]
    [StringLength(100, ErrorMessage = "Bakım türü en fazla 100 karakter olabilir.")]
    [Display(Name = "Bakım Türü")]
    public string MaintenanceType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Bakım / İşlem Açıklaması")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Bakım Başlangıç Tarihi")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Bakım Bitiş Tarihi (Opsiyonel)")]
    public DateTime? EndDate { get; set; }

    [StringLength(150, ErrorMessage = "Servis/Yapan firma adı en fazla 150 karakter olabilir.")]
    [Display(Name = "İşlemi Yapan Servis / Firma")]
    public string? PerformedBy { get; set; }

    // Dropdown listesi
    public List<SelectListItem> AvailableVehicles { get; set; } = new();
}
