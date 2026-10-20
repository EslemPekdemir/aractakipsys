// File: Models/ViewModels/Assignment/AssignmentCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentCreateViewModel
{
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
    public DateTime PlannedStartTime { get; set; } = DateTime.Today.AddHours(9);

    [Required(ErrorMessage = "Planlanan bitiş zamanı zorunludur.")]
    [Display(Name = "Planlanan Bitiş")]
    public DateTime PlannedEndTime { get; set; } = DateTime.Today.AddHours(17);

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

    // Dropdown listeleri (Müsaitlik ve ehliyet durumları işaretli)
    public List<SelectableVehicleViewModel> AvailableVehicles { get; set; } = new();
    public List<SelectableDriverViewModel> AvailableDrivers { get; set; } = new();
}

public class SelectableVehicleViewModel
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public int CurrentKm { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? UnavailableReason { get; set; }

    // Gecikmiş Seyirde (Planlanan dönüş tarihi geçmiş ama teslim edilmemiş / hala yolda olan araç)
    public bool IsOverdueOnDuty { get; set; } = false;
    public DateTime? OverdueSince { get; set; }
    public string? OverdueWarningText { get; set; }

    public string DisplayText
    {
        get
        {
            if (IsOverdueOnDuty)
            {
                var dateStr = OverdueSince?.ToString("dd.MM.yyyy HH:mm") ?? "";
                return $"⚠️ [GECİKMİŞ SEYİRDE] {PlateNumber} - {BrandModel} ({CurrentKm:N0} KM - {dateStr}'den beri seyirde)";
            }

            if (IsAvailable)
                return $"{PlateNumber} - {BrandModel} ({CurrentKm:N0} KM)";

            var reason = UnavailableReason?
                .Replace(", görev atanamaz veya başlatılamaz.", "")
                .Replace(", görev atanamaz veya başlatılamaz", "")
                .Trim();

            return $"[UYARI] {PlateNumber} - {BrandModel} ({reason})";
        }
    }
}

public class SelectableDriverViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string? UnavailableReason { get; set; }
    public bool IsCurrentlyOnDuty { get; set; }
    public string? ActiveDutyPlate { get; set; }

    public string DisplayText
    {
        get
        {
            if (IsAvailable)
            {
                if (IsCurrentlyOnDuty)
                {
                    var plateInfo = !string.IsNullOrWhiteSpace(ActiveDutyPlate) ? $" - {ActiveDutyPlate}" : "";
                    return $"[GÖREVDE] {FullName}{plateInfo} (Sonraki tarihler için görev verilebilir) (Ehliyet: {LicenseClass})";
                }
                return $"{FullName} (Ehliyet: {LicenseClass}, {Department})";
            }

            var reason = UnavailableReason?
                .Replace(", görev atanamaz veya başlatılamaz.", "")
                .Replace(", görev atanamaz veya başlatılamaz", "")
                .Trim();

            return $"[UYARI] {FullName} ({reason})";
        }
    }
}

