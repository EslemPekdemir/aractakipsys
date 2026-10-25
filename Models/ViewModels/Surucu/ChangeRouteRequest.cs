// File: Models/ViewModels/Surucu/ChangeRouteRequest.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Surucu;

public class ChangeRouteRequest
{
    [Required]
    public int AssignmentId { get; set; }

    [Required(ErrorMessage = "Yeni güzergah bilgisi zorunludur.")]
    [StringLength(250, ErrorMessage = "Güzergah bilgisi en fazla 250 karakter olabilir.")]
    [Display(Name = "Yeni Güzergah / Rota")]
    public string NewRoute { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Hedef / Varış noktası en fazla 200 karakter olabilir.")]
    [Display(Name = "Yeni Varış / Görev Yeri (Opsiyonel)")]
    public string? NewDestination { get; set; }

    [Required(ErrorMessage = "Güzergah değişikliği gerekçesi / açıklaması zorunludur.")]
    [StringLength(500, ErrorMessage = "Gerekçe açıklaması en fazla 500 karakter olabilir.")]
    [Display(Name = "Değişiklik Gerekçesi")]
    public string Reason { get; set; } = string.Empty;
}
