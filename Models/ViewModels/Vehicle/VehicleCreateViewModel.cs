// File: Models/ViewModels/Vehicle/VehicleCreateViewModel.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Plaka alanı zorunludur.")]
    [StringLength(15, ErrorMessage = "Plaka en fazla 15 karakter olabilir.")]
    [Display(Name = "Plaka")]
    public string PlateNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Marka alanı zorunludur.")]
    [StringLength(50, ErrorMessage = "Marka en fazla 50 karakter olabilir.")]
    [Display(Name = "Marka")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model alanı zorunludur.")]
    [StringLength(50, ErrorMessage = "Model en fazla 50 karakter olabilir.")]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model yılı zorunludur.")]
    [Range(1990, 2100, ErrorMessage = "Geçerli bir model yılı giriniz (1990 - 2100).")]
    [Display(Name = "Model Yılı")]
    public int Year { get; set; } = DateTime.Now.Year;

    [Required(ErrorMessage = "Renk alanı zorunludur.")]
    [StringLength(30, ErrorMessage = "Renk en fazla 30 karakter olabilir.")]
    [Display(Name = "Renk")]
    public string Color { get; set; } = string.Empty;

    [Required(ErrorMessage = "Araç tipi alanı zorunludur.")]
    [StringLength(50, ErrorMessage = "Araç tipi en fazla 50 karakter olabilir.")]
    [Display(Name = "Araç Tipi")]
    public string VehicleType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yakıt tipi alanı zorunludur.")]
    [StringLength(50, ErrorMessage = "Yakıt tipi en fazla 50 karakter olabilir.")]
    [Display(Name = "Yakıt Tipi")]
    public string FuelType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlangıç kilometresi zorunludur.")]
    [Range(0, int.MaxValue, ErrorMessage = "Kilometre negatif olamaz.")]
    [Display(Name = "Başlangıç KM")]
    public int CurrentKm { get; set; } = 0;

    // Muayene & Sigorta Bilgileri (Zorunlu)
    [Required(ErrorMessage = "Muayene başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Muayene Başlangıç Tarihi")]
    public DateTime InspectionStartDate { get; set; } = DateTime.Today.AddYears(-1);

    [Required(ErrorMessage = "Muayene bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Muayene Bitiş Tarihi")]
    public DateTime InspectionExpiryDate { get; set; } = DateTime.Today.AddYears(1);

    [Required(ErrorMessage = "Sigorta başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Sigorta Başlangıç Tarihi")]
    public DateTime InsuranceStartDate { get; set; } = DateTime.Today.AddYears(-1);

    [Required(ErrorMessage = "Sigorta bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Sigorta Bitiş Tarihi")]
    public DateTime InsuranceExpiryDate { get; set; } = DateTime.Today.AddYears(1);

    // Lastik Bilgileri (Opsiyonel)
    [StringLength(50, ErrorMessage = "Lastik markası en fazla 50 karakter olabilir.")]
    [Display(Name = "Lastik Marka / Modeli")]
    public string? TireBrand { get; set; }

    [Range(2000, 2100, ErrorMessage = "Geçerli bir lastik üretim yılı giriniz.")]
    [Display(Name = "Lastik Üretim Yılı")]
    public int? TireYear { get; set; }

    [StringLength(50, ErrorMessage = "Lastik mevsimi en fazla 50 karakter olabilir.")]
    [Display(Name = "Lastik Mevsimi")]
    public string? TireSeason { get; set; }

    // Ceza Notları (Opsiyonel Manuel Not)
    [StringLength(1000, ErrorMessage = "Ceza bilgisi / notları en fazla 1000 karakter olabilir.")]
    [Display(Name = "Trafik Cezası / Ceza Notları")]
    public string? FineNotes { get; set; }

    [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir.")]
    [Display(Name = "Notlar / Açıklama")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InspectionExpiryDate < InspectionStartDate)
        {
            yield return new ValidationResult(
                "Muayene bitiş tarihi, başlangıç tarihinden önce olamaz.",
                new[] { nameof(InspectionExpiryDate) });
        }

        if (InsuranceExpiryDate < InsuranceStartDate)
        {
            yield return new ValidationResult(
                "Sigorta bitiş tarihi, başlangıç tarihinden önce olamaz.",
                new[] { nameof(InsuranceExpiryDate) });
        }
    }
}
