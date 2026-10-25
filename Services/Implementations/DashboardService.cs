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

        // 8. Ehliyet Süresi Yaklaşan / Geçen Şoförler (Faz 4 - 30 gün içinde dolacaklar + süresi dolmuşlar)
        var expiringDriverLicenses = await _context.DriverLicenses
            .Include(l => l.Driver)
            .ThenInclude(d => d.User)
            .Where(l => !l.IsDeleted && !l.Driver.IsDeleted && l.Driver.IsActive && l.LicenseExpiryDate.Date <= threshold)
            .OrderBy(l => l.LicenseExpiryDate)
            .Select(l => new DriverLicenseExpiryAlertViewModel
            {
                DriverId = l.DriverId,
                DriverFullName = l.Driver.User.FullName,
                Department = l.Driver.Department,
                LicenseClass = l.LicenseClass,
                LicenseExpiryDate = l.LicenseExpiryDate,
                DaysRemaining = (int)(l.LicenseExpiryDate.Date - today).TotalDays
            })
            .ToListAsync();

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
            DocumentAlertVehicles = docAlertVehicles,
            ExpiringDriverLicenses = expiringDriverLicenses
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

        // Güncel Lastik Bilgisi (Faz 5-B)
        var latestTire = await _context.VehicleTires
            .Where(t => t.VehicleId == vehicle.Id && !t.IsDeleted)
            .OrderByDescending(t => t.InstalledDate ?? t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        aractakipsys.Models.ViewModels.Vehicle.VehicleTireItemViewModel? currentTireVm = null;
        if (latestTire != null)
        {
            currentTireVm = new aractakipsys.Models.ViewModels.Vehicle.VehicleTireItemViewModel
            {
                Id = latestTire.Id,
                VehicleId = latestTire.VehicleId,
                PlateNumber = vehicle.PlateNumber,
                Brand = latestTire.Brand,
                Size = latestTire.Size,
                DotDate = latestTire.DotDate,
                ProductionYear = latestTire.ProductionYear,
                Season = latestTire.Season,
                InstalledDate = latestTire.InstalledDate,
                InstalledKm = latestTire.InstalledKm,
                RecommendedReplaceKm = latestTire.RecommendedReplaceKm,
                RecommendedReplaceDate = latestTire.RecommendedReplaceDate,
                Cost = latestTire.Cost,
                Notes = latestTire.Notes,
                CreatedAt = latestTire.CreatedAt,
                VehicleCurrentKm = vehicle.CurrentKm,
                IsCurrentTire = true
            };
        }

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
            CurrentTire = currentTireVm,
            DriverHistory = completedAssignments
        };
    }

    public async Task<DashboardCalendarMonthResponseDto> GetCalendarMonthDataAsync(int year, int month)
    {
        if (year < 2000 || year > 2100) year = DateTime.Today.Year;
        if (month < 1 || month > 12) month = DateTime.Today.Month;

        var culture = new System.Globalization.CultureInfo("tr-TR");
        var firstDayOfMonth = new DateTime(year, month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Takvim ızgarası: Pazartesi ile başlar
        int offsetDays = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var gridStartDate = firstDayOfMonth.AddDays(-offsetDays);
        var gridEndDate = gridStartDate.AddDays(41); // 6 satır x 7 gün = 42 gün

        var today = DateTime.Today;

        // TEK SORGULAR (N+1 yapma kuralı)
        // 1) Assignment'lar (Planlanan veya Seyirde olanlar)
        var assignments = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Include(a => a.Driver).ThenInclude(d => d!.User)
            .Include(a => a.AssignmentDrivers).ThenInclude(ad => ad.Driver).ThenInclude(d => d.User)
            .Where(a => !a.IsDeleted &&
                        (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress) &&
                        a.PlannedStartTime.Date <= gridEndDate.Date &&
                        a.PlannedEndTime.Date >= gridStartDate.Date)
            .ToListAsync();

        // 2) Maintenances (Bakımda olanlar)
        var maintenances = await _context.Maintenances
            .AsNoTracking()
            .Include(m => m.Vehicle)
            .Where(m => !m.IsDeleted &&
                        m.StartDate.Date <= gridEndDate.Date &&
                        (!m.EndDate.HasValue || m.EndDate.Value.Date >= gridStartDate.Date))
            .ToListAsync();

        // 3) Vehicle Documents & Tires
        var vehiclesWithDocs = await _context.Vehicles
            .AsNoTracking()
            .Include(v => v.Documents)
            .Include(v => v.Tires)
            .Where(v => !v.IsDeleted)
            .ToListAsync();

        // 4) Driver Licenses
        var driverLicenses = await _context.DriverLicenses
            .AsNoTracking()
            .Include(dl => dl.Driver).ThenInclude(d => d.User)
            .Where(dl => !dl.IsDeleted && dl.LicenseExpiryDate.Date >= gridStartDate.Date && dl.LicenseExpiryDate.Date <= gridEndDate.Date)
            .ToListAsync();

        var daysList = new List<DashboardCalendarDaySummaryDto>();
        var detailsDict = new Dictionary<string, DashboardCalendarDayDetailDto>();

        for (var date = gridStartDate; date <= gridEndDate; date = date.AddDays(1))
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var dayDate = date.Date;

            // O güne denk gelen görevler
            var dayAssignments = assignments
                .Where(a => a.PlannedStartTime.Date <= dayDate && a.PlannedEndTime.Date >= dayDate)
                .Select(a =>
                {
                    var driverList = new List<string>();
                    if (a.Driver != null && a.Driver.User != null)
                    {
                        driverList.Add(a.Driver.User.FullName);
                    }
                    if (a.AssignmentDrivers != null && a.AssignmentDrivers.Any())
                    {
                        foreach (var ad in a.AssignmentDrivers)
                        {
                            if (ad.Driver?.User != null && !driverList.Contains(ad.Driver.User.FullName))
                            {
                                driverList.Add(ad.Driver.User.FullName);
                            }
                        }
                    }
                    if (a.IsDriverless) driverList.Add("Şoförsüz Araç");
                    if (a.IsExternalDriver && !string.IsNullOrWhiteSpace(a.ExternalDriverFullName))
                    {
                        driverList.Add($"Dış Şoför: {a.ExternalDriverFullName}");
                    }

                    var statusBadge = a.Status == AssignmentStatus.InProgress ? "badge-primary bg-primary text-white" : "badge-secondary bg-secondary text-white";
                    var statusName = a.Status == AssignmentStatus.InProgress ? "Seyirde" : "Planlandı";

                    return new DashboardCalendarAssignmentItemDto
                    {
                        Id = a.Id,
                        VehicleId = a.VehicleId,
                        VehiclePlate = a.Vehicle?.PlateNumber ?? "-",
                        VehicleBrandModel = $"{a.Vehicle?.Brand} {a.Vehicle?.Model}".Trim(),
                        DriverNames = driverList.Any() ? string.Join(", ", driverList) : "Belirtilmemiş",
                        Route = a.Route ?? "-",
                        Destination = a.Destination ?? "-",
                        Purpose = a.Purpose,
                        Department = a.RequestedByDepartment ?? "-",
                        Status = a.Status.ToString(),
                        StatusDisplayName = statusName,
                        StatusBadgeClass = statusBadge,
                        StartTime = a.ActualStartTime?.ToString("HH:mm") ?? a.PlannedStartTime.ToString("HH:mm"),
                        EndTime = a.PlannedEndTime.ToString("HH:mm")
                    };
                })
                .ToList();

            // O gün görevde / planlı olan BENZERSİZ araç sayısı
            var activeVehiclesCount = dayAssignments.Select(x => x.VehicleId).Distinct().Count();

            // O gün bakımda olan araçlar
            var dayMaintenances = maintenances
                .Where(m => m.StartDate.Date <= dayDate && (!m.EndDate.HasValue || m.EndDate.Value.Date >= dayDate))
                .Select(m => new DashboardCalendarMaintenanceItemDto
                {
                    Id = m.Id,
                    VehicleId = m.VehicleId,
                    VehiclePlate = m.Vehicle?.PlateNumber ?? "-",
                    VehicleBrandModel = $"{m.Vehicle?.Brand} {m.Vehicle?.Model}".Trim(),
                    MaintenanceType = m.MaintenanceType,
                    Description = m.Description ?? "-",
                    StartDate = m.StartDate.ToString("dd.MM.yyyy"),
                    EndDate = m.EndDate?.ToString("dd.MM.yyyy"),
                    PerformedBy = m.PerformedBy ?? "-"
                })
                .ToList();

            var maintenanceVehiclesCount = dayMaintenances.Select(x => x.VehicleId).Distinct().Count();

            // O gün süresi dolan belgeler (Muayene, Sigorta, Kasko, Lastik)
            var dayDocs = new List<DashboardCalendarDocumentItemDto>();
            foreach (var v in vehiclesWithDocs)
            {
                if (v.InspectionExpiryDate.Date == dayDate)
                {
                    dayDocs.Add(new DashboardCalendarDocumentItemDto
                    {
                        VehicleId = v.Id,
                        VehiclePlate = v.PlateNumber,
                        VehicleBrandModel = $"{v.Brand} {v.Model}".Trim(),
                        DocumentType = "Periyodik Muayene",
                        ExpiryDate = v.InspectionExpiryDate.ToString("dd.MM.yyyy"),
                        BadgeClass = "badge-danger bg-danger text-white",
                        Details = "Araç muayene son geçerlilik tarihi"
                    });
                }
                if (v.InsuranceExpiryDate.Date == dayDate)
                {
                    dayDocs.Add(new DashboardCalendarDocumentItemDto
                    {
                        VehicleId = v.Id,
                        VehiclePlate = v.PlateNumber,
                        VehicleBrandModel = $"{v.Brand} {v.Model}".Trim(),
                        DocumentType = "Trafik Sigortası",
                        ExpiryDate = v.InsuranceExpiryDate.ToString("dd.MM.yyyy"),
                        BadgeClass = "badge-warning bg-warning text-dark",
                        Details = "Zorunlu mali sorumluluk sigortası"
                    });
                }

                // Alt belgeler
                if (v.Documents != null)
                {
                    foreach (var doc in v.Documents.Where(d => !d.IsDeleted && d.EndDate.Date == dayDate))
                    {
                        var docName = doc.DocumentType == Models.Enums.VehicleDocumentType.Insurance ? "Trafik Sigortası"
                                    : (doc.DocumentType == Models.Enums.VehicleDocumentType.Inspection ? "Muayene"
                                    : (doc.DocumentType == Models.Enums.VehicleDocumentType.Kasko ? "Kasko Poliçesi" : "Diğer Belge"));
                        if (!dayDocs.Any(x => x.VehicleId == v.Id && x.DocumentType == docName))
                        {
                            dayDocs.Add(new DashboardCalendarDocumentItemDto
                            {
                                VehicleId = v.Id,
                                VehiclePlate = v.PlateNumber,
                                VehicleBrandModel = $"{v.Brand} {v.Model}".Trim(),
                                DocumentType = docName,
                                ExpiryDate = doc.EndDate.ToString("dd.MM.yyyy"),
                                BadgeClass = "badge-info bg-info text-white",
                                Details = doc.PolicyOrStationNumber ?? "-"
                            });
                        }
                    }
                }

                // Lastik tavsiye değişim tarihi
                if (v.Tires != null)
                {
                    foreach (var tire in v.Tires.Where(t => !t.IsDeleted && t.RecommendedReplaceDate.HasValue && t.RecommendedReplaceDate.Value.Date == dayDate))
                    {
                        dayDocs.Add(new DashboardCalendarDocumentItemDto
                        {
                            VehicleId = v.Id,
                            VehiclePlate = v.PlateNumber,
                            VehicleBrandModel = $"{v.Brand} {v.Model}".Trim(),
                            DocumentType = "Lastik Değişimi",
                            ExpiryDate = tire.RecommendedReplaceDate?.ToString("dd.MM.yyyy") ?? "-",
                            BadgeClass = "badge-secondary bg-secondary text-white",
                            Details = $"{tire.Brand} {tire.Season}"
                        });
                    }
                }
            }

            // O gün süresi dolan ehliyetler
            var dayLicenses = driverLicenses
                .Where(dl => dl.LicenseExpiryDate.Date == dayDate)
                .Select(dl => new DashboardCalendarLicenseItemDto
                {
                    DriverId = dl.DriverId,
                    DriverFullName = dl.Driver?.User?.FullName ?? "-",
                    LicenseClass = dl.LicenseClass,
                    ExpiryDate = dl.LicenseExpiryDate.ToString("dd.MM.yyyy"),
                    Department = dl.Driver?.Department ?? "-"
                })
                .ToList();

            daysList.Add(new DashboardCalendarDaySummaryDto
            {
                Date = dateStr,
                Day = date.Day,
                IsCurrentMonth = date.Month == month,
                IsToday = dayDate == today,
                ActiveVehicleCount = activeVehiclesCount,
                MaintenanceVehicleCount = maintenanceVehiclesCount,
                ExpiringDocumentCount = dayDocs.Count,
                ExpiringLicenseCount = dayLicenses.Count
            });

            detailsDict[dateStr] = new DashboardCalendarDayDetailDto
            {
                Date = dateStr,
                FormattedDate = date.ToString("dd MMMM yyyy, dddd", culture),
                Assignments = dayAssignments,
                Maintenances = dayMaintenances,
                ExpiringDocuments = dayDocs,
                ExpiringLicenses = dayLicenses
            };
        }

        return new DashboardCalendarMonthResponseDto
        {
            Year = year,
            Month = month,
            MonthName = firstDayOfMonth.ToString("MMMM yyyy", culture),
            Days = daysList,
            DetailsByDate = detailsDict
        };
    }
}
