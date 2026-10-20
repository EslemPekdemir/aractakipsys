// File: Models/ViewModels/Account/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı veya e-posta adresi zorunludur.")]
    [Display(Name = "Kullanıcı Adı veya E-Posta")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    [Required(ErrorMessage = "Güvenlik kodunu giriniz.")]
    [Display(Name = "Güvenlik Kodu")]
    public string CaptchaCode { get; set; } = string.Empty;

    public string CaptchaToken { get; set; } = string.Empty;

    public string? CaptchaSvgImage { get; set; }
}
