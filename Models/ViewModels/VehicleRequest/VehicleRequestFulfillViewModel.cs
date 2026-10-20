// File: Models/ViewModels/VehicleRequest/VehicleRequestFulfillViewModel.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.ViewModels.Assignment;

namespace aractakipsys.Models.ViewModels.VehicleRequest;

public class VehicleRequestFulfillViewModel
{
    public int RequestId { get; set; }
    public string RequesterFullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string RequesterEmail { get; set; } = string.Empty;
    public string RequesterPhone { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    [Required(ErrorMessage = "Lütfen görevlendirilecek bir araç seçiniz.")]
    [Display(Name = "Görevlendirilecek Araç")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Lütfen görevlendirilecek bir şoför seçiniz.")]
    [Display(Name = "Görevlendirilecek Şoför")]
    public int DriverId { get; set; }

    [Required(ErrorMessage = "Planlanan başlangıç tarihi/saati zorunludur.")]
    [Display(Name = "Planlanan Başlangıç Zamanı")]
    public DateTime PlannedStartTime { get; set; } = DateTime.Today.AddDays(1).AddHours(9);

    [Required(ErrorMessage = "Planlanan bitiş tarihi/saati zorunludur.")]
    [Display(Name = "Planlanan Bitiş Zamanı")]
    public DateTime PlannedEndTime { get; set; } = DateTime.Today.AddDays(1).AddHours(17);

    [Display(Name = "Şoförün Adı ve Telefonu Gönderilen E-Postada Yer Alsın")]
    public bool IncludeDriverInfoInEmail { get; set; } = true;

    [Display(Name = "Gönderen Büro / Şeflik Adı")]
    public string? DepartmentTitle { get; set; }

    [Display(Name = "Şef Notu / Özel Talimat")]
    [StringLength(500, ErrorMessage = "Şef notu en fazla 500 karakter olabilir.")]
    public string? ChiefNote { get; set; }

    // Müsaitlik ve ehliyet durumları işaretli listeler
    public List<SelectableVehicleViewModel> AvailableVehicles { get; set; } = new();
    public List<SelectableDriverViewModel> AvailableDrivers { get; set; } = new();
}
