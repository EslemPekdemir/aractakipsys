// File: Models/ViewModels/Surucu/DriverChangePasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Surucu;

public class DriverChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifrenizi girmelisiniz.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifrenizi girmelisiniz.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Yeni şifre en az 10 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifrenizi tekrar girmelisiniz.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre Tekrar")]
    [Compare("NewPassword", ErrorMessage = "Girdiğiniz yeni şifreler birbiriyle eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
