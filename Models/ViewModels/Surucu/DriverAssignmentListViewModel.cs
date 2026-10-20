// File: Models/ViewModels/Surucu/DriverAssignmentListViewModel.cs
using aractakipsys.Models.ViewModels.Assignment;

namespace aractakipsys.Models.ViewModels.Surucu;

public class DriverAssignmentListViewModel
{
    public string DriverFullName { get; set; } = string.Empty;
    public string DriverDepartment { get; set; } = string.Empty;

    // Aktif devam eden görev (Varsa en üstte gösterilir)
    public AssignmentCardViewModel? ActiveAssignment { get; set; }

    // Bugünün planlanan görevleri
    public List<AssignmentCardViewModel> TodayPlannedAssignments { get; set; } = new();

    // Gelecekteki planlanan görevler
    public List<AssignmentCardViewModel> UpcomingAssignments { get; set; } = new();

    // Şoförün bitiş bildirdiği ancak Şef onayı bekleyen görevler
    public List<AssignmentCardViewModel> PendingApprovalAssignments { get; set; } = new();

    // Geçmiş tamamlanan/iptal edilen görevler
    public List<AssignmentCardViewModel> CompletedAssignments { get; set; } = new();
    public List<AssignmentCardViewModel> PastAssignments { get => CompletedAssignments; set => CompletedAssignments = value; }

    // Gece vardiyası / acil görev için otoparktaki müsait araçlar
    public List<SelectableVehicleViewModel> AvailableVehicles { get; set; } = new();

    public bool HasAnyAssignments =>
        ActiveAssignment != null ||
        PendingApprovalAssignments.Count > 0 ||
        TodayPlannedAssignments.Count > 0 ||
        UpcomingAssignments.Count > 0 ||
        CompletedAssignments.Count > 0;
}
