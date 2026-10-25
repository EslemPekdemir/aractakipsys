// File: Models/ViewModels/Driver/DriverLicenseItemViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Driver;

public class DriverLicenseItemViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ehliyet sınıfı zorunludur.")]
    [StringLength(10, ErrorMessage = "Ehliyet sınıfı en fazla 10 karakter olabilir.")]
    [Display(Name = "Ehliyet Sınıfı")]
    public string LicenseClass { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ehliyet son geçerlilik tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Geçerlilik Tarihi")]
    public DateTime LicenseExpiryDate { get; set; } = DateTime.Today.AddYears(5);

    public bool HasExpired => LicenseExpiryDate.Date < DateTime.Today;
    public bool IsExpired => HasExpired;
    public int DaysUntilExpiry => (LicenseExpiryDate.Date - DateTime.Today).Days;
    public int RemainingDays => DaysUntilExpiry;

    public string StatusBadgeText => IsExpired ? "Süresi Doldu" : $"{RemainingDays} Gün Kaldı";
    public string StatusBadgeClass => IsExpired ? "badge-danger" : RemainingDays <= 30 ? "badge-warning" : "badge-info";
}
