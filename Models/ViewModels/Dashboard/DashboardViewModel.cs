// File: Models/ViewModels/Dashboard/DashboardViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Dashboard;

public class DashboardViewModel
{
    // 1. KPI Metrikleri
    public int TotalVehicles { get; set; }
    public int IdleVehicles { get; set; }
    public int OnDutyVehicles { get; set; }
    public int MaintenanceVehicles { get; set; }
    public int OutOfServiceVehicles { get; set; }
    public int DocumentExpiredVehicles { get; set; }

    public int ActiveAssignmentsCount { get; set; }
    public int TodayPlannedAssignmentsCount { get; set; }
    public int OngoingMaintenancesCount { get; set; }
    public int TotalDrivers { get; set; }
    public int ActiveDrivers { get; set; }

    // 2. Bugün Devam Eden ve Planlanan Görevler (En son 10 kayıt)
    public List<DashboardAssignmentItemViewModel> ActiveAssignments { get; set; } = new();

    // 3. Devam Eden / Açık Bakımlar
    public List<DashboardMaintenanceItemViewModel> OngoingMaintenances { get; set; } = new();

    // 4. Grafik Verileri
    public int ChartIdleCount => IdleVehicles;
    public int ChartOnDutyCount => OnDutyVehicles;
    public int ChartMaintenanceCount => MaintenanceVehicles;
    public int ChartOutOfServiceCount => OutOfServiceVehicles;
    public int ChartDocumentExpiredCount => DocumentExpiredVehicles;

    // 5. Onay Bekleyen Araç Talepleri (Bildirim Sistemi)
    public int PendingVehicleRequestsCount { get; set; }
    public List<aractakipsys.Models.ViewModels.VehicleRequest.PendingNotificationItemViewModel> PendingVehicleRequestsSummary { get; set; } = new();

    // 7. Belge Süresi Yaklaşan / Geçen Araçlar (Tablo & Kart)
    public List<DocumentExpiryTableItemViewModel> DocumentAlertVehicles { get; set; } = new();
    public int DocumentAlertVehiclesCount => DocumentAlertVehicles.Count;

    // 8. Ehliyet Süresi Yaklaşan / Geçen Şoförler (Faz 4 - 30 gün içinde dolacaklar + süresi dolmuşlar)
    public List<DriverLicenseExpiryAlertViewModel> ExpiringDriverLicenses { get; set; } = new();
    public int ExpiringDriverLicensesCount => ExpiringDriverLicenses.Count;

    // 9. Toast Bildirimi Kullanıcı & Uyarı İmzası (Oturum Hafızası)
    public string CurrentUserId { get; set; } = string.Empty;

    public string ToastAlertSignature
    {
        get
        {
            var pendingIds = string.Join(",", PendingVehicleRequestsSummary.Select(p => p.Id).OrderBy(id => id));
            var docIds = string.Join(",", DocumentAlertVehicles.Select(d => d.VehicleId).OrderBy(id => id));
            var licIds = string.Join(",", ExpiringDriverLicenses.Select(l => $"{l.DriverId}_{l.LicenseClass}").OrderBy(id => id));
            return $"user:{CurrentUserId}|pending:{PendingVehicleRequestsCount}[{pendingIds}]|docs:{DocumentAlertVehiclesCount}[{docIds}]|lics:{ExpiringDriverLicensesCount}[{licIds}]";
        }
    }
}

public class DriverLicenseExpiryAlertViewModel
{
    public int DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string LicenseClass { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired => DaysRemaining < 0;

    public string RowClass => IsExpired ? "table-danger" : "table-warning";
    public string BadgeClass => IsExpired ? "badge-danger bg-danger text-white" : "badge-warning bg-warning text-dark";
    public string StatusText => IsExpired ? $"{Math.Abs(DaysRemaining)} gün geçti" : (DaysRemaining == 0 ? "Bugün doluyor!" : $"{DaysRemaining} gün kaldı");
}

public class DocumentExpiryTableItemViewModel
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;

    public DateTime InspectionExpiryDate { get; set; }
    public int InspectionDaysRemaining { get; set; }
    public bool IsInspectionExpired => InspectionDaysRemaining < 0;

    public DateTime InsuranceExpiryDate { get; set; }
    public int InsuranceDaysRemaining { get; set; }
    public bool IsInsuranceExpired => InsuranceDaysRemaining < 0;

    public bool HasAnyExpired => IsInspectionExpired || IsInsuranceExpired || Status == VehicleStatus.DocumentExpired;

    /// <summary>
    /// İş Kuralı: En kritik gün farkı (negatif = gecikmiş, pozitif = kalan gün).
    /// Sıralamada en küçük (en çok gecikmiş / en az gün kalmış) en başa gelir.
    /// </summary>
    public int MostUrgentDaysRemaining => Math.Min(InspectionDaysRemaining, InsuranceDaysRemaining);

    public string RowClass => HasAnyExpired ? "table-danger" : "table-warning";
    public string AlertBadgeClass => HasAnyExpired ? "badge-danger bg-danger text-white" : "badge-warning bg-warning text-dark";
    public string AlertStatusText => HasAnyExpired ? "Süresi Doldu" : "Süresi Yaklaşıyor";
}

public class DashboardAssignmentItemViewModel
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string DriverFullName { get; set; } = string.Empty;
    public string? DriverPhone { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? RequestedByDepartment { get; set; }
    public AssignmentStatus Status { get; set; }

    public string StatusDisplayName => Status switch
    {
        AssignmentStatus.Planned => "Planlandı",
        AssignmentStatus.InProgress => "Seyirde / Görevde",
        AssignmentStatus.Completed => "Tamamlandı",
        AssignmentStatus.Cancelled => "İptal Edildi",
        _ => Status.ToString()
    };

    public string StatusBadgeClass => Status switch
    {
        AssignmentStatus.Planned => "badge-info bg-info text-white",
        AssignmentStatus.InProgress => "badge-primary bg-primary text-white",
        AssignmentStatus.Completed => "badge-success bg-success text-white",
        AssignmentStatus.Cancelled => "badge-danger bg-danger text-white",
        _ => "badge-secondary"
    };
}

public class DashboardMaintenanceItemViewModel
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int DurationDays => (int)(DateTime.Today - StartDate.Date).TotalDays;
    public string? PerformedBy { get; set; }
}
