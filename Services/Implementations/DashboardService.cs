// File: Services/Implementations/DashboardService.cs
using aractakipsys.Data;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Dashboard;
using aractakipsys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardMetricsAsync()
    {
        var today = DateTime.Today;

        // 1. Araç KPI Dağılımları (5 Durum Eksiksiz Sayımı)
        var statusCounts = await _context.Vehicles
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var idleVehicles = statusCounts.FirstOrDefault(x => x.Status == VehicleStatus.Idle)?.Count ?? 0;
        var onDutyVehicles = statusCounts.FirstOrDefault(x => x.Status == VehicleStatus.OnDuty)?.Count ?? 0;
        var maintenanceVehicles = statusCounts.FirstOrDefault(x => x.Status == VehicleStatus.Maintenance)?.Count ?? 0;
        var outOfServiceVehicles = statusCounts.FirstOrDefault(x => x.Status == VehicleStatus.OutOfService)?.Count ?? 0;
        var documentExpiredVehicles = statusCounts.FirstOrDefault(x => x.Status == VehicleStatus.DocumentExpired)?.Count ?? 0;
        var totalVehicles = idleVehicles + onDutyVehicles + maintenanceVehicles + outOfServiceVehicles + documentExpiredVehicles;

        // 2. Görev ve Bakım KPI'ları
        var activeAssignmentsCount = await _context.Assignments
            .CountAsync(a => a.Status == AssignmentStatus.InProgress);

        var todayPlannedAssignmentsCount = await _context.Assignments
            .CountAsync(a => a.Status == AssignmentStatus.Planned);

        var ongoingMaintenancesCount = await _context.Maintenances
            .CountAsync(m => m.EndDate == null);

        // 3. Şoför Metrikleri
        var totalDrivers = await _context.Drivers.CountAsync();
        var activeDrivers = await _context.Drivers.CountAsync(d => d.IsActive);

        // 4. Devam Eden ve Planlanan Görevler (En son 10 kayıt)
        // İş Kuralı: InProgress olanlar en üstte, sonra Planned (tarihe göre en yakın olan üstte)
        var activeAssignments = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .Where(a => a.Status == AssignmentStatus.InProgress ||
                       a.Status == AssignmentStatus.Planned)
            .OrderBy(a => a.Status == AssignmentStatus.InProgress ? 0 : 1)
            .ThenBy(a => a.PlannedStartTime)
            .Take(20)
            .Select(a => new DashboardAssignmentItemViewModel
            {
                Id = a.Id,
                VehiclePlate = a.Vehicle.PlateNumber,
                VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                DriverFullName = a.Driver.User != null ? a.Driver.User.FullName : "Atanmamış",
                DriverPhone = a.Driver.User != null ? a.Driver.User.PhoneNumber : null,
                PlannedStartTime = a.PlannedStartTime,
                ActualStartTime = a.ActualStartTime,
                Purpose = a.Purpose,
                RequestedByDepartment = a.RequestedByDepartment,
                Status = a.Status
            })
            .ToListAsync();

        // 5. Devam Eden / Açık Bakımlar (En eski tarihliden başa doğru)
        var ongoingMaintenances = await _context.Maintenances
            .Include(m => m.Vehicle)
            .Where(m => m.EndDate == null)
            .OrderBy(m => m.StartDate)
            .Take(20)
            .Select(m => new DashboardMaintenanceItemViewModel
            {
                Id = m.Id,
                VehiclePlate = m.Vehicle.PlateNumber,
                VehicleBrandModel = $"{m.Vehicle.Brand} {m.Vehicle.Model}",
                MaintenanceType = m.MaintenanceType,
                StartDate = m.StartDate,
                PerformedBy = m.PerformedBy
            })
            .ToListAsync();

        // 6. Onay Bekleyen Araç Talepleri (Bildirim Sistemi)
        var pendingRequestsQuery = _context.VehicleRequests
            .Where(r => !r.IsDeleted && r.Status == VehicleRequestStatus.Pending);

        var pendingVehicleRequestsCount = await pendingRequestsQuery.CountAsync();

        var pendingVehicleRequestsSummary = await pendingRequestsQuery
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new aractakipsys.Models.ViewModels.VehicleRequest.PendingNotificationItemViewModel
            {
                Id = r.Id,
                RequesterFullName = r.RequesterFullName,
                Purpose = r.Purpose,
                FormattedDate = r.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            })
            .ToListAsync();

        // 7. Belge Süresi Yaklaşan / Geçen Araçlar (30 gün içinde dolacaklar + süresi dolmuşlar)
        var threshold = today.AddDays(30);
        var docAlertVehicles = await _context.Vehicles
            .Where(v => !v.IsDeleted &&
                        (v.Status == VehicleStatus.DocumentExpired ||
                         v.InspectionExpiryDate.Date < today ||
                         v.InsuranceExpiryDate.Date < today ||
                         (v.InspectionExpiryDate.Date >= today && v.InspectionExpiryDate.Date <= threshold) ||
                         (v.InsuranceExpiryDate.Date >= today && v.InsuranceExpiryDate.Date <= threshold)))
            .OrderBy(v => (v.Status == VehicleStatus.DocumentExpired || v.InspectionExpiryDate.Date < today || v.InsuranceExpiryDate.Date < today) ? 0 : 1)
            .ThenBy(v => v.InspectionExpiryDate < v.InsuranceExpiryDate ? v.InspectionExpiryDate : v.InsuranceExpiryDate)
            .Select(v => new DocumentExpiryTableItemViewModel
            {
                VehicleId = v.Id,
                PlateNumber = v.PlateNumber,
                BrandModel = $"{v.Brand} {v.Model}",
                Status = v.Status,
                StatusDisplayName = v.Status == VehicleStatus.Idle ? "Boşta" :
                                    v.Status == VehicleStatus.OnDuty ? "Görevde" :
                                    v.Status == VehicleStatus.Maintenance ? "Bakımda" :
                                    v.Status == VehicleStatus.OutOfService ? "Hizmet Dışı" :
                                    v.Status == VehicleStatus.DocumentExpired ? "Belgesi Geçersiz" : v.Status.ToString(),
                StatusBadgeClass = v.Status == VehicleStatus.Idle ? "bg-success" :
                                   v.Status == VehicleStatus.OnDuty ? "bg-primary" :
                                   v.Status == VehicleStatus.Maintenance ? "bg-warning" :
                                   v.Status == VehicleStatus.OutOfService ? "bg-danger" :
                                   v.Status == VehicleStatus.DocumentExpired ? "bg-danger" : "bg-secondary",
                InspectionExpiryDate = v.InspectionExpiryDate,
                InspectionDaysRemaining = (int)(v.InspectionExpiryDate.Date - today).TotalDays,
                InsuranceExpiryDate = v.InsuranceExpiryDate,
                InsuranceDaysRemaining = (int)(v.InsuranceExpiryDate.Date - today).TotalDays
            })
            .ToListAsync();

        // İş Kuralı: Belge Uyarıları - En acil olan (en çok gecikmiş / en az gün kalmış) en üstte
        docAlertVehicles = docAlertVehicles
            .OrderBy(d => d.MostUrgentDaysRemaining)
            .ToList();

        return new DashboardViewModel
        {
            TotalVehicles = totalVehicles,
            IdleVehicles = idleVehicles,
            OnDutyVehicles = onDutyVehicles,
            MaintenanceVehicles = maintenanceVehicles,
            OutOfServiceVehicles = outOfServiceVehicles,
            DocumentExpiredVehicles = documentExpiredVehicles,
            ActiveAssignmentsCount = activeAssignmentsCount,
            TodayPlannedAssignmentsCount = todayPlannedAssignmentsCount,
            OngoingMaintenancesCount = ongoingMaintenancesCount,
            TotalDrivers = totalDrivers,
            ActiveDrivers = activeDrivers,
            ActiveAssignments = activeAssignments,
            OngoingMaintenances = ongoingMaintenances,
            PendingVehicleRequestsCount = pendingVehicleRequestsCount,
            PendingVehicleRequestsSummary = pendingVehicleRequestsSummary,
            DocumentAlertVehicles = docAlertVehicles
        };
    }

    public async Task<DashboardPlateSearchResultViewModel?> SearchPlateAsync(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return null;

        var normalizedPlate = plate.Trim().Replace(" ", "").ToUpperInvariant();

        // Plaka eşleşmesi (boşluklu veya boşluksuz arama desteği)
        var vehicle = await _context.Vehicles
            .Where(v => !v.IsDeleted && (
                v.PlateNumber == plate.Trim() ||
                v.PlateNumber.Replace(" ", "").ToUpper() == normalizedPlate))
            .FirstOrDefaultAsync();

        if (vehicle == null)
            return null;

        var today = DateTime.Today;

        // Tamamlanmış Assignment kayıtları (Status == Completed), en son kullanan en üstte
        var completedAssignments = await _context.Assignments
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .Where(a => a.VehicleId == vehicle.Id && 
                        a.Status == AssignmentStatus.Completed && 
                        !a.IsDeleted)
            .OrderByDescending(a => a.ActualEndTime ?? a.PlannedEndTime)
            .Select(a => new DashboardVehicleDriverHistoryViewModel
            {
                AssignmentId = a.Id,
                DriverId = a.DriverId,
                DriverFullName = a.Driver.User != null ? a.Driver.User.FullName : "Bilinmiyor",
                DriverPhone = a.Driver.User != null ? a.Driver.User.PhoneNumber : null,
                CompletedDate = a.ActualEndTime ?? a.PlannedEndTime,
                PlannedStartTime = a.PlannedStartTime,
                PlannedEndTime = a.PlannedEndTime,
                StartKm = a.StartKm,
                EndKm = a.EndKm,
                Purpose = a.Purpose,
                RequestedByDepartment = a.RequestedByDepartment
            })
            .ToListAsync();

        var statusName = vehicle.Status switch
        {
            VehicleStatus.Idle => "Boşta / Müsait",
            VehicleStatus.OnDuty => "Görevde / Seyirde",
            VehicleStatus.Maintenance => "Bakımda / Serviste",
            VehicleStatus.OutOfService => "Hizmet Dışı",
            VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
            _ => vehicle.Status.ToString()
        };

        var badgeClass = vehicle.Status switch
        {
            VehicleStatus.Idle => "badge-success bg-success text-white",
            VehicleStatus.OnDuty => "badge-primary bg-primary text-white",
            VehicleStatus.Maintenance => "badge-warning bg-warning text-dark",
            VehicleStatus.OutOfService => "badge-danger bg-danger text-white",
            VehicleStatus.DocumentExpired => "badge-danger bg-danger text-white",
            _ => "badge-secondary"
        };

        return new DashboardPlateSearchResultViewModel
        {
            VehicleId = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Color = vehicle.Color,
            CurrentKm = vehicle.CurrentKm,
            Status = vehicle.Status,
            StatusDisplayName = statusName,
            StatusBadgeClass = badgeClass,
            InsuranceExpiryDate = vehicle.InsuranceExpiryDate,
            InsuranceDaysRemaining = (int)(vehicle.InsuranceExpiryDate.Date - today).TotalDays,
            InspectionExpiryDate = vehicle.InspectionExpiryDate,
            InspectionDaysRemaining = (int)(vehicle.InspectionExpiryDate.Date - today).TotalDays,
            FineNotes = vehicle.FineNotes,
            DriverHistory = completedAssignments
        };
    }
}
