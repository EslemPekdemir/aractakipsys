// File: Models/ViewModels/Report/UsageReportItemViewModel.cs
namespace aractakipsys.Models.ViewModels.Report;

public class UsageReportItemViewModel
{
    public int AssignmentId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string DriverFullName { get; set; } = string.Empty;
    public string DriverDepartment { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? RequestedByDepartment { get; set; }
    
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public int? StartKm { get; set; }
    public int? EndKm { get; set; }

    public int? DriverId { get; set; }
    public bool IsPrimaryDriver { get; set; } = true;

    /// <summary>
    /// Kat edilen fiili kilometre (EndKm - StartKm)
    /// </summary>
    public int DistanceKm => (EndKm.HasValue && StartKm.HasValue && EndKm.Value >= StartKm.Value)
        ? (EndKm.Value - StartKm.Value)
        : 0;

    /// <summary>
    /// Seferin gerçekleştiği tarih (Fiili başlangıç veya planlanan başlangıç)
    /// </summary>
    public DateTime TripDate => ActualStartTime ?? PlannedStartTime;

    /// <summary>
    /// Sefer süresi (Fiili bitiş-başlangıç, yoksa planlanan bitiş-başlangıç)
    /// </summary>
    public TimeSpan DurationTimeSpan
    {
        get
        {
            var start = ActualStartTime ?? PlannedStartTime;
            var end = ActualEndTime ?? PlannedEndTime;
            if (end > start)
            {
                return end - start;
            }
            return TimeSpan.Zero;
        }
    }

    public int DurationMinutes => (int)Math.Round(DurationTimeSpan.TotalMinutes);

    public string DurationFormatted => aractakipsys.Common.Helpers.DurationHelper.FormatDuration(DurationTimeSpan);
}
