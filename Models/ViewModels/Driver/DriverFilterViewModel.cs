// File: Models/ViewModels/Driver/DriverFilterViewModel.cs
namespace aractakipsys.Models.ViewModels.Driver;

public class DriverFilterViewModel
{
    public bool? IsActive { get; set; }
    public string? Department { get; set; }
    public string? SearchTerm { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<DriverListItemViewModel> Drivers { get; set; } = new();
    public List<string> DepartmentList { get; set; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
