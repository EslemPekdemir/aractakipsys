// File: Models/ViewModels/VehicleRequest/VehicleRequestCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.VehicleRequest;

public class VehicleRequestCreateViewModel
{
    [Required(ErrorMessage = "Talep türü seçimi zorunludur.")]
    [Display(Name = "Talep Türü")]
    public aractakipsys.Models.Enums.VehicleRequestType RequestType { get; set; } = aractakipsys.Models.Enums.VehicleRequestType.OfficialTrip;

    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(150, ErrorMessage = "Ad Soyad en fazla 150 karakter olabilir.")]
    [Display(Name = "Talep Eden Adı Soyadı")]
    public string RequesterFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kurumsal e-posta adresi zorunludur.")]
    [StringLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@duzce\.edu\.tr$", ErrorMessage = "E-posta adresi @duzce.edu.tr uzantılı olmalıdır.")]
    [Display(Name = "Kurumsal E-Posta (@duzce.edu.tr)")]
    public string RequesterEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "İletişim telefonu zorunludur.")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Telefon numarası en az 10, en fazla 11 hane olmalıdır.")]
    [RegularExpression(@"^(\+90|0)?\s*\(?5\d{2}\)?[-. ]*\d{3}[-. ]*(\d{4}|\d{2}[-. ]*\d{2})$", 
        ErrorMessage = "Lütfen geçerli ve eksiksiz bir cep telefonu numarası giriniz (Örn: 05xx xxx xx xx - 10 veya 11 hane).")]
    [Display(Name = "İletişim Telefonu")]
    public string RequesterPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fakülte / Daire Başkanlığı / Birim seçimi zorunludur.")]
    [StringLength(150, ErrorMessage = "Birim adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Fakülte / Daire Başkanlığı / Birim")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "İstenen araç türü seçimi zorunludur.")]
    [StringLength(100, ErrorMessage = "İstenen araç türü en fazla 100 karakter olabilir.")]
    [Display(Name = "İstenen Araç Türü")]
    public string RequestedVehicleType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kaç kişi seyahat edileceği (kişi sayısı) zorunludur.")]
    [Range(1, 100, ErrorMessage = "Kişi sayısı en az 1 olmalıdır.")]
    [Display(Name = "Kaç Kişi (Yolcu Sayısı)")]
    public int? PassengerCount { get; set; } = 1;

    [Required(ErrorMessage = "Talep edilen başlangıç tarih ve saati zorunludur.")]
    [Display(Name = "Görev Başlangıç Tarih ve Saati")]
    public DateTime? RequestedStartTime { get; set; }

    [Required(ErrorMessage = "Talep edilen bitiş tarih ve saati zorunludur.")]
    [Display(Name = "Görev Bitiş Tarih ve Saati")]
    public DateTime? RequestedEndTime { get; set; }

    [Required(ErrorMessage = "Araç talep gerekçesi / görev detayı zorunludur.")]
    [StringLength(500, ErrorMessage = "Talep gerekçesi en fazla 500 karakter olabilir.")]
    [Display(Name = "Talep Gerekçesi ve Görev Detayı")]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gidilecek güzergah / rota alanı zorunludur.")]
    [StringLength(500, ErrorMessage = "Güzergah en fazla 500 karakter olabilir.")]
    [Display(Name = "Gidilecek Güzergah / Rota")]
    public string Route { get; set; } = string.Empty;

    // Dropdown seçenek listeleri (Controller tarafından doldurulur)
    public List<string> AvailableDepartments { get; set; } = new();
    public List<string> AvailableVehicleTypes { get; set; } = new();
}
