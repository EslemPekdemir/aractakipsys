// File: Models/ViewModels/Vehicle/VehicleDocumentCreateViewModel.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleDocumentCreateViewModel
{
    [Required(ErrorMessage = "Araç seçimi zorunludur.")]
    public int VehicleId { get; set; }

    [Required(ErrorMessage = "Belge türü zorunludur.")]
    public VehicleDocumentType DocumentType { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

    [Range(0, 10000000, ErrorMessage = "Tutar 0 veya daha büyük bir değer olmalıdır.")]
    public decimal Cost { get; set; } = 0;

    [StringLength(100, ErrorMessage = "Poliçe / İstasyon no en fazla 100 karakter olabilir.")]
    public string? PolicyOrStationNumber { get; set; }

    [StringLength(100, ErrorMessage = "Firma / Kuruluş adı en fazla 100 karakter olabilir.")]
    public string? CompanyName { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama / Not en fazla 500 karakter olabilir.")]
    public string? Notes { get; set; }
}
