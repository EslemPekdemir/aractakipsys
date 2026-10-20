// File: Models/ViewModels/Account/ForgotPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Lütfen kullanıcı adınızı veya e-posta adresinizi giriniz.")]
    [Display(Name = "Kullanıcı Adı veya E-Posta")]
    public string EmailOrUsername { get; set; } = string.Empty;
}
