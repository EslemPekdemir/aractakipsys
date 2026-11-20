// File: Models/ViewModels/Report/MaintenanceReportFilterViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Report;

public class MaintenanceReportFilterViewModel
{
    [Display(Name = "Araç")]
    public int? VehicleId { get; set; }

    [Display(Name = "Bakım / İşlem Türü")]
    public string? MaintenanceType { get; set; }

    [Display(Name = "Durum")]
    public bool? IsOngoing { get; set; } // null: Tümü, true: Devam Eden, false: Tamamlanan

    [Display(Name = "Dönem / Hazır Aralık")]
    public string? DateRangePreset { get; set; } // "ThisMonth", "LastMonth", "ThisYear", "LastYear", "Custom"

    [Display(Name = "Başlangıç Tarihi ve Saati")]
    [DataType(DataType.DateTime)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Bitiş Tarihi ve Saati")]
    [DataType(DataType.DateTime)]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Kullanıcının forma tıklayıp arama yapıp yapmadığını takip eder
    /// </summary>
    public bool HasSearched { get; set; } = false;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Dropdown Seçenekleri (Select2)
    public List<SelectListItem> VehicleList { get; set; } = new();
    public List<SelectListItem> MaintenanceTypeList { get; set; } = new();
}
