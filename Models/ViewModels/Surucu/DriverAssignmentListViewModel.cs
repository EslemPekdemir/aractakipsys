// File: Models/ViewModels/Surucu/DriverAssignmentListViewModel.cs
using aractakipsys.Models.ViewModels.Assignment;

namespace aractakipsys.Models.ViewModels.Surucu;

public class DriverAssignmentListViewModel
{
    public string DriverFullName { get; set; } = string.Empty;
    public string DriverDepartment { get; set; } = string.Empty;

    // Şoförün ehliyetleri ve uyarıları (Faz 4)
    public List<aractakipsys.Models.ViewModels.Driver.DriverLicenseItemViewModel> Licenses { get; set; } = new();
    public List<aractakipsys.Models.ViewModels.Driver.DriverLicenseItemViewModel> ExpiringOrExpiredLicenses =>
        Licenses.Where(l => (l.LicenseExpiryDate.Date - DateTime.UtcNow.Date).Days <= 30).ToList();
    public bool HasLicenseAlert => ExpiringOrExpiredLicenses.Any();

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

    // KULLANIMDAN KALDIRILDI: Gece vardiyası / acil görev için otoparktaki müsait araçlar listesi kaldırılmıştır.
    [Obsolete("Gece Vardiyası / Acil Görev Başlat özelliği kaldırılmıştır.")]
    public List<SelectableVehicleViewModel> AvailableVehicles { get; set; } = new();

    public bool HasAnyAssignments =>
        ActiveAssignment != null ||
        PendingApprovalAssignments.Count > 0 ||
        TodayPlannedAssignments.Count > 0 ||
        UpcomingAssignments.Count > 0 ||
        CompletedAssignments.Count > 0;
}
