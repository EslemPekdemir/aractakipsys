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
}
