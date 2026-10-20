// File: Models/ViewModels/Driver/DriverEditViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Driver;

public class DriverEditViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Ehliyet belge numarası zorunludur.")]
    [StringLength(20, ErrorMessage = "Ehliyet numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "Ehliyet Numarası")]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ehliyet sınıfı zorunludur.")]
    [StringLength(10, ErrorMessage = "Ehliyet sınıfı en fazla 10 karakter olabilir.")]
    [Display(Name = "Ehliyet Sınıfı")]
    public string LicenseClass { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ehliyet son geçerlilik tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ehliyet Geçerlilik Tarihi")]
    public DateTime LicenseExpiryDate { get; set; }

    [Required(ErrorMessage = "Bağlı olunan birim / departman zorunludur.")]
    [StringLength(100, ErrorMessage = "Birim adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Birim / Departman")]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Aktif Durumda")]
    public bool IsActive { get; set; } = true;
}
