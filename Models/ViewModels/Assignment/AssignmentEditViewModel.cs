// File: Models/ViewModels/Assignment/AssignmentEditViewModel.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Araç seçimi zorunludur.")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen listeden görevlendirilecek bir araç seçiniz.")]
    [Display(Name = "Görevlendirilecek Araç")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Şoför seçimi zorunludur.")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen listeden görevlendirilecek bir şoför seçiniz.")]
    [Display(Name = "Görevlendirilecek Şoför")]
    public int DriverId { get; set; }

    [Required(ErrorMessage = "Talep eden birim zorunludur.")]
    [StringLength(150, ErrorMessage = "Birim adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Talep Eden Birim")]
    public string RequestedByDepartment { get; set; } = string.Empty;

    [Required(ErrorMessage = "Görev amacı / açıklaması zorunludur.")]
    [StringLength(300, ErrorMessage = "Görev amacı en fazla 300 karakter olabilir.")]
    [Display(Name = "Görev Amacı ve Güzergah")]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "Planlanan başlangıç zamanı zorunludur.")]
    [Display(Name = "Planlanan Başlangıç")]
    public DateTime PlannedStartTime { get; set; }

    [Required(ErrorMessage = "Planlanan bitiş zamanı zorunludur.")]
    [Display(Name = "Planlanan Bitiş")]
    public DateTime PlannedEndTime { get; set; }

    public AssignmentStatus Status { get; set; }

    // Konaklama ve Yevmiye Bilgileri (Saatlik / Günlük Hibrit Model - Aşama F)
    [Display(Name = "Konaklanan Gece Sayısı")]
    public int? AccommodationNights { get; set; }

    [Display(Name = "Konaklama Toplam Maliyeti (₺)")]
    public decimal? AccommodationCost { get; set; }

    [Display(Name = "Ücret Hesaplama Türü")]
    public string PerDiemType { get; set; } = "Hourly";

    [Display(Name = "Görev Süresi (Saat)")]
    public decimal? PerDiemHours { get; set; }

    [Display(Name = "Saat Başına / Gün Başına Ücret (₺)")]
    public decimal? PerDiemRate { get; set; }

    [Display(Name = "Ücretli Gün Sayısı")]
    public int? PerDiemDays { get; set; }

    [Display(Name = "Toplam Görev Ücreti (₺)")]
    public decimal? PerDiemAmount { get; set; }

    [StringLength(500, ErrorMessage = "Masraf notu en fazla 500 karakter olabilir.")]
    [Display(Name = "Masraf / Harcırah Notları")]
    public string? ExpenseNotes { get; set; }

    public List<SelectableVehicleViewModel> AvailableVehicles { get; set; } = new();
    public List<SelectableDriverViewModel> AvailableDrivers { get; set; } = new();
}
