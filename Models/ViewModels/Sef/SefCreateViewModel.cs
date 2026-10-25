// File: Models/ViewModels/Sef/SefCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Sef;

public class SefCreateViewModel
{
    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(150, ErrorMessage = "Ad Soyad en fazla 150 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Kullanıcı adı sadece harf, rakam, nokta, alt çizgi ve tire içerebilir.")]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@duzce\.edu\.tr$", ErrorMessage = "E-posta adresi @duzce.edu.tr uzantılı olmalıdır.")]
    [Display(Name = "Kurumsal E-Posta (@duzce.edu.tr)")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "İlk giriş şifresi zorunludur.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Şifre en az 10 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "İlk Giriş Şifresi")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Şifre Tekrar")]
    [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Hesap Durumu")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Rol seçimi zorunludur.")]
    [Display(Name = "Kullanıcı Rolü")]
    public string Role { get; set; } = "Kullanici";
}
