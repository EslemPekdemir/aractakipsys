// File: Models/ViewModels/Vehicle/VehicleEditViewModel.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleEditViewModel : IValidatableObject
{
    public int Id { get; set; }

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
    public int Year { get; set; }

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

    // Salt Okunur Alanlar (ARAYUZ-SPESIFIKASYONU Bölüm 5)
    [Display(Name = "Güncel KM")]
    public int CurrentKm { get; set; }

    [Display(Name = "Araç Durumu")]
    public VehicleStatus Status { get; set; }

    [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir.")]
    [Display(Name = "Notlar / Açıklama")]
    public string? Notes { get; set; }

    // Muayene & Sigorta Bilgileri (Zorunlu)
    [Required(ErrorMessage = "Muayene başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Muayene Başlangıç Tarihi")]
    public DateTime InspectionStartDate { get; set; }

    [Required(ErrorMessage = "Muayene bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Muayene Bitiş Tarihi")]
    public DateTime InspectionExpiryDate { get; set; }

    [Required(ErrorMessage = "Sigorta başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Sigorta Başlangıç Tarihi")]
    public DateTime InsuranceStartDate { get; set; }

    [Required(ErrorMessage = "Sigorta bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Sigorta Bitiş Tarihi")]
    public DateTime InsuranceExpiryDate { get; set; }

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

    public string StatusBadgeClass => Status switch
    {
        VehicleStatus.Idle => "bg-success",
        VehicleStatus.OnDuty => "bg-primary",
        VehicleStatus.Maintenance => "bg-warning",
        VehicleStatus.OutOfService => "bg-danger",
        VehicleStatus.DocumentExpired => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusDisplayName => Status switch
    {
        VehicleStatus.Idle => "Boşta",
        VehicleStatus.OnDuty => "Görevde",
        VehicleStatus.Maintenance => "Bakımda",
        VehicleStatus.OutOfService => "Hizmet Dışı",
        VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
        _ => Status.ToString()
    };

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
