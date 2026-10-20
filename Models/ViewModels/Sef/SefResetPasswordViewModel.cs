// File: Models/ViewModels/Sef/SefResetPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Sef;

public class SefResetPasswordViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre alanı zorunludur.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Şifre en az 10 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre Tekrar")]
    [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
