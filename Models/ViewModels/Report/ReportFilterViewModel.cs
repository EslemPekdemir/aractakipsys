// File: Models/ViewModels/Report/ReportFilterViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Report;

public class ReportFilterViewModel
{
    [Display(Name = "Araç")]
    public int? VehicleId { get; set; }

    [Display(Name = "Şoför")]
    public int? DriverId { get; set; }

    [Display(Name = "Dönem / Hazır Aralık")]
    public string? DateRangePreset { get; set; } // "ThisMonth", "LastMonth", "ThisYear", "LastYear", "Custom"

    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Kullanıcının forma tıklayıp arama yapıp yapmadığını takip eder (ilk sayfa açılışı ile ayrım için)
    /// </summary>
    public bool HasSearched { get; set; } = false;

    // Dropdown Seçenekleri (Select2)
    public List<SelectListItem> VehicleList { get; set; } = new();
    public List<SelectListItem> DriverList { get; set; } = new();
}
