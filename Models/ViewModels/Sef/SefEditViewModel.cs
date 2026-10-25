// File: Models/ViewModels/Sef/SefEditViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Sef;

public class SefEditViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(150, ErrorMessage = "Ad Soyad en fazla 150 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Kurumsal E-Posta")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Hesap Durumu (Aktif)")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Mevcut Rol")]
    public string RoleName { get; set; } = "Kullanici";

    [Display(Name = "Kullanıcı Rolü")]
    public string Role { get => RoleName; set => RoleName = value; }

    [Display(Name = "Rol")]
    public string RoleDisplayName { get; set; } = "Ulaşım Şefi";
}
