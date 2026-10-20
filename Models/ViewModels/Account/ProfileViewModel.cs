// File: Models/ViewModels/Account/ProfileViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Account;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-Posta")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Kullanıcı Rolleri")]
    public List<string> Roles { get; set; } = new();

    [Display(Name = "Kayıt Tarihi")]
    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    [Display(Name = "Mevcut Profil Fotoğrafı")]
    public string? ProfilePictureUrl { get; set; }

    [Display(Name = "Yeni Profil Fotoğrafı")]
    public IFormFile? ProfilePicture { get; set; }

    // Parola Değiştirme Alanları (Opsiyonel - Doğrulaması Controller seviyesinde yönetilir)
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Parola")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola Tekrar")]
    public string? ConfirmPassword { get; set; }
}
