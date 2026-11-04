// File: Models/ViewModels/Surucu/EmergencySelfAssignmentRequest.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Surucu;

/// <summary>
/// KULLANIMDAN KALDIRILDI: Gece vardiyası / acil kendi kendine görev başlatma özelliği kaldırılmıştır.
/// </summary>
[Obsolete("Gece Vardiyası / Acil Görev Başlat özelliği kullanımdan kaldırıldı.")]
public class EmergencySelfAssignmentRequest
{
    [Required(ErrorMessage = "Lütfen kullanılacak aracı seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir araç seçiniz.")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Lütfen görevi talep eden birimi belirtiniz.")]
    [StringLength(150, ErrorMessage = "Birim adı en fazla 150 karakter olabilir.")]
    public string RequestedByDepartment { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen görev amacını ve güzergahı belirtiniz.")]
    [StringLength(300, ErrorMessage = "Görev amacı en fazla 300 karakter olabilir.")]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen başlangıç kilometresini giriniz.")]
    [Range(0, 2000000, ErrorMessage = "Geçerli bir kilometre değeri giriniz.")]
    public int StartKm { get; set; }

    [Range(1, 72, ErrorMessage = "Tahmini süre 1 ile 72 saat arasında olmalıdır.")]
    public int EstimatedHours { get; set; } = 4;
}
