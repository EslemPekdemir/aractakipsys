// File: Models/ViewModels/Maintenance/MaintenanceFilterViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Maintenance;

public class MaintenanceFilterViewModel
{
    public int? VehicleId { get; set; }
    public string? MaintenanceType { get; set; }
    public bool? IsOngoing { get; set; }
    public string? SearchTerm { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public List<MaintenanceListItemViewModel> Maintenances { get; set; } = new();

    // Dropdown filtre seçenekleri
    public List<SelectListItem> VehicleList { get; set; } = new();
    public List<string> MaintenanceTypeList { get; set; } = new();
}
