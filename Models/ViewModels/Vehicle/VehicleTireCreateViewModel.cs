// File: Models/ViewModels/Vehicle/VehicleTireCreateViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleTireCreateViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Lütfen araç seçiniz.")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Marka / Model alanı zorunludur.")]
    [StringLength(50, ErrorMessage = "Marka en fazla 50 karakter olabilir.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ebat alanı zorunludur.")]
    [StringLength(30, ErrorMessage = "Ebat en fazla 30 karakter olabilir.")]
    public string Size { get; set; } = string.Empty; // Örn: 205/55 R16

    [Required(ErrorMessage = "DOT üretim tarihi zorunludur.")]
    [RegularExpression(@"^(0[1-9]|[1-4][0-9]|5[0-3])\/[0-9]{2}$", ErrorMessage = "Üretim tarihi 'Hafta/Yıl' biçiminde olmalıdır (Örn: 52/23, hafta 01-53 ve yıl 2 hane).")]
    public string DotDate { get; set; } = string.Empty; // Hafta/Yıl (01-53/00-99)

    [Required(ErrorMessage = "Mevsim seçimi zorunludur.")]
    [StringLength(50)]
    public string Season { get; set; } = "Yazlık"; // Yazlık / Kışlık / Dört Mevsim

    [Required(ErrorMessage = "Takılma / Değişim tarihi zorunludur.")]
    [DataType(DataType.Date)]
    public DateTime? InstalledDate { get; set; }

    [Required(ErrorMessage = "Takıldığı andaki KM zorunludur.")]
    [Range(0, 2000000, ErrorMessage = "Takılma KM'si 0 ile 2.000.000 arasında olmalıdır.")]
    public int? InstalledKm { get; set; }

    [Range(0, 2000000, ErrorMessage = "Değişim KM'si 0 ile 2.000.000 arasında olmalıdır.")]
    public int? RecommendedReplaceKm { get; set; }

    [DataType(DataType.Date)]
    public DateTime? RecommendedReplaceDate { get; set; }

    [Range(0, 500000, ErrorMessage = "Maliyet 0'dan küçük olamaz.")]
    public decimal? Cost { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama 500 karakteri geçemez.")]
    public string? Notes { get; set; }
}
