// File: Models/ViewModels/Vehicle/VehicleFilterViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleFilterViewModel
{
    public VehicleStatus? Status { get; set; }
    public string? VehicleType { get; set; }
    public string? SearchTerm { get; set; }

    public List<string> AvailableVehicleTypes { get; set; } = new();

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<VehicleListItemViewModel> Vehicles { get; set; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
