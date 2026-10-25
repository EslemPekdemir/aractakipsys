// File: Models/ViewModels/Report/UsageReportDriverGroupViewModel.cs
using aractakipsys.Common.Helpers;

namespace aractakipsys.Models.ViewModels.Report;

public class UsageReportDriverGroupViewModel
{
    public int? DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string DriverDepartment { get; set; } = string.Empty;

    public List<UsageReportItemViewModel> Items { get; set; } = new();

    // Şoför Bazlı Alt Toplamlar
    public int SubtotalTrips => Items.Count;
    public int SubtotalDistanceKm => Items.Sum(x => x.DistanceKm);
    public int SubtotalDurationMinutes => Items.Sum(x => x.DurationMinutes);
    public string SubtotalDurationFormatted => DurationHelper.FormatDuration(TimeSpan.FromMinutes(SubtotalDurationMinutes));
}
