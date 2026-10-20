// File: Models/ViewModels/Assignment/AssignmentFilterViewModel.cs
using aractakipsys.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentFilterViewModel
{
    public AssignmentStatus? Status { get; set; }
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<AssignmentListItemViewModel> Assignments { get; set; } = new();

    public List<SelectListItem> VehicleList { get; set; } = new();
    public List<SelectListItem> DriverList { get; set; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
