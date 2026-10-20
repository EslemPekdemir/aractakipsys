// File: Models/ViewModels/Account/ResetPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Account;

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-Posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifrenizi girmelisiniz.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Şifreniz en az 10 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifrenizi tekrar girmelisiniz.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre Tekrar")]
    [Compare("Password", ErrorMessage = "Girdiğiniz şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
