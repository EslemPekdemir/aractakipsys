// File: Services/Interfaces/IDashboardService.cs
using aractakipsys.Models.ViewModels.Dashboard;

namespace aractakipsys.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardMetricsAsync();
    Task<DashboardPlateSearchResultViewModel?> SearchPlateAsync(string plate);
}
