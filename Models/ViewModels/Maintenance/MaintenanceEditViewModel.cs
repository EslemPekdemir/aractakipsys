// File: Models/ViewModels/Maintenance/MaintenanceEditViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Maintenance;

public class MaintenanceEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Araç seçimi zorunludur.")]
    [Display(Name = "Araç")]
    public int VehicleId { get; set; }

    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bakım türü zorunludur.")]
    [StringLength(100, ErrorMessage = "Bakım türü en fazla 100 karakter olabilir.")]
    [Display(Name = "Bakım Türü")]
    public string MaintenanceType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Bakım / İşlem Açıklaması")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Bakım Başlangıç Tarihi")]
    public DateTime StartDate { get; set; }

    [Display(Name = "Bakım Bitiş Tarihi")]
    public DateTime? EndDate { get; set; }

    [StringLength(150, ErrorMessage = "Servis/Yapan firma adı en fazla 150 karakter olabilir.")]
    [Display(Name = "İşlemi Yapan Servis / Firma")]
    public string? PerformedBy { get; set; }

    [Display(Name = "Maliyet (₺)")]
    [Range(0, 99999999.99, ErrorMessage = "Maliyet 0 ile 99.999.999,99 ₺ arasında olmalıdır.")]
    public decimal? Cost { get; set; }

    public List<SelectListItem> AvailableVehicles { get; set; } = new();
    public List<SelectListItem> AvailableMaintenanceTypes { get; set; } = new();
}
