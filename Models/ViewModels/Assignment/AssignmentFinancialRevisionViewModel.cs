// File: Models/ViewModels/Assignment/AssignmentFinancialRevisionViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Assignment;

/// <summary>
/// Tamamlanmış bir görev için sonradan getirilen fatura veya harcırah belgelerine istinaden
/// yalnızca maliyet ve harcırah bilgilerini güncellemek için kullanılan model.
/// </summary>
public class AssignmentFinancialRevisionViewModel
{
    [Required]
    public int AssignmentId { get; set; }

    [Display(Name = "Şoför Saatlik Ücreti (TL) (Opsiyonel)")]
    [Range(0, 100000, ErrorMessage = "Saatlik ücret 0 veya daha büyük olmalıdır.")]
    public decimal? HourlyRate { get; set; }

    [Display(Name = "Konaklanan Gece Sayısı")]
    [Range(0, 365, ErrorMessage = "Gece sayısı 0 veya daha büyük olmalıdır.")]
    public int? AccommodationNights { get; set; }

    [Display(Name = "Konaklama / Fatura Tutarı (TL)")]
    [Range(0, 1000000, ErrorMessage = "Konaklama tutarı 0 veya daha büyük olmalıdır.")]
    public decimal? AccommodationCost { get; set; }

    [Display(Name = "Ücret Hesaplama Türü")]
    public string? PerDiemType { get; set; } = "Hourly";

    [Display(Name = "Görev Süresi (Saat)")]
    public decimal? PerDiemHours { get; set; }

    [Display(Name = "Saat Başına / Gün Başına Ücret (TL)")]
    public decimal? PerDiemRate { get; set; }

    [Display(Name = "Ücretli Gün Sayısı")]
    [Range(0, 365, ErrorMessage = "Gün sayısı 0 veya daha büyük olmalıdır.")]
    public int? PerDiemDays { get; set; }

    [Display(Name = "Toplam Ücret Tutarı (TL)")]
    [Range(0, 1000000, ErrorMessage = "Ücret tutarı 0 veya daha büyük olmalıdır.")]
    public decimal? PerDiemAmount { get; set; }

    [Required(ErrorMessage = "Lütfen yapılan revizyonun gerekçesini giriniz (Örn: Otel faturası ibraz edildi).")]
    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Revizyon Gerekçesi / Fatura Notu")]
    public string RevisionReason { get; set; } = string.Empty;
}
