// File: Models/ViewModels/Assignment/AssignmentApproveEndViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentApproveEndViewModel
{
    [Required]
    public int AssignmentId { get; set; }

    [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
    [Display(Name = "Onaylanan Başlangıç Saati")]
    public DateTime ConfirmedStartTime { get; set; }

    [Required(ErrorMessage = "Dönüş saati zorunludur.")]
    [Display(Name = "Onaylanan Bitiş Saati")]
    public DateTime ConfirmedEndTime { get; set; }

    [Display(Name = "Saatlik Ücret (TL) (Opsiyonel)")]
    public decimal? HourlyRate { get; set; }

    [Display(Name = "Hesaplanan Görev Süresi (Saat)")]
    public decimal? CalculatedDutyHours { get; set; }

    [Display(Name = "Toplam Şoför Ücreti (TL)")]
    public decimal? TotalDriverFee { get; set; }

    [Display(Name = "Konaklanan Gece Sayısı")]
    public int? AccommodationNights { get; set; }

    [Display(Name = "Konaklama Masrafı (TL)")]
    public decimal? AccommodationCost { get; set; }

    [Display(Name = "Ücret Hesaplama Türü")]
    public string? PerDiemType { get; set; } = "Hourly";

    [Display(Name = "Görev Süresi (Saat)")]
    public decimal? PerDiemHours { get; set; }

    [Display(Name = "Saat Başına / Gün Başına Ücret (TL)")]
    public decimal? PerDiemRate { get; set; }

    [Display(Name = "Ücretli Gün Sayısı")]
    public int? PerDiemDays { get; set; }

    [Display(Name = "Toplam Ücret Tutarı (TL)")]
    public decimal? PerDiemAmount { get; set; }

    [Display(Name = "Genel Toplam Masraf (TL)")]
    public decimal? TotalTripExpense { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Şef / Yetkili Onay Notu")]
    public string? ExpenseNotes { get; set; }
}
