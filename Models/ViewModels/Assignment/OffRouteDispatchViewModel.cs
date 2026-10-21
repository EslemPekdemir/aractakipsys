// File: Models/ViewModels/Assignment/OffRouteDispatchViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Assignment;

public class OffRouteDispatchViewModel
{
    [Required]
    public int AssignmentId { get; set; }

    [Required(ErrorMessage = "Lütfen aracı güzergah dışına sevk eden yetkilinin adını ve soyadını giriniz.")]
    [StringLength(150, ErrorMessage = "Yetkili adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Sevk Eden Yetkili Adı Soyadı")]
    public string DispatcherName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Unvan en fazla 100 karakter olabilir.")]
    [Display(Name = "Yetkili Unvanı")]
    public string? DispatcherTitle { get; set; }

    [Required(ErrorMessage = "Lütfen aracı güzergah dışına sevk etme gerekçesini belirtiniz.")]
    [StringLength(500, ErrorMessage = "Sevk nedeni en fazla 500 karakter olabilir.")]
    [Display(Name = "Acil Sevk Gerekçesi / Nedeni")]
    public string Reason { get; set; } = string.Empty;
}
