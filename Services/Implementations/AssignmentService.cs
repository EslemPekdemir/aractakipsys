// File: Services/Implementations/AssignmentService.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Assignment;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AssignmentService> _logger;
    private readonly IKmValidationService _kmValidationService;

    public AssignmentService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IWebHostEnvironment environment,
        ILogger<AssignmentService> logger,
        IKmValidationService kmValidationService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _environment = environment;
        _logger = logger;
        _kmValidationService = kmValidationService;
    }

    public async Task<AssignmentFilterViewModel> GetAssignmentsPagedAsync(AssignmentFilterViewModel filter)
    {
        var query = _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .Include(a => a.AssignmentDrivers)
                .ThenInclude(ad => ad.Driver)
                    .ThenInclude(d => d.User)
            .AsNoTracking()
            .AsQueryable();

        // 1. Dropdown Filtre Verileri
        filter.VehicleList = await _context.Vehicles
            .OrderBy(v => v.PlateNumber)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.PlateNumber} - {v.Brand} {v.Model}"
            })
            .ToListAsync();

        filter.DriverList = await _context.Drivers
            .Include(d => d.User)
            .Where(d => d.IsActive)
            .OrderBy(d => d.User.FullName)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.User.FullName
            })
            .ToListAsync();

        // 2. Filtreler
        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (filter.VehicleId.HasValue)
        {
            query = query.Where(a => a.VehicleId == filter.VehicleId.Value);
        }

        if (filter.DriverId.HasValue)
        {
            query = query.Where(a => a.DriverId == filter.DriverId.Value ||
                                     a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == filter.DriverId.Value));
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.PlannedStartTime >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(a => a.PlannedEndTime <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Vehicle.PlateNumber.ToLower().Contains(search) ||
                (a.Driver != null && a.Driver.User.FullName.ToLower().Contains(search)) ||
                (a.ExternalDriverFullName != null && a.ExternalDriverFullName.ToLower().Contains(search)) ||
                a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.Driver.User.FullName.ToLower().Contains(search)) ||
                a.RequestedByDepartment.ToLower().Contains(search) ||
                (a.Destination != null && a.Destination.ToLower().Contains(search)) ||
                (a.Route != null && a.Route.ToLower().Contains(search)) ||
                a.Purpose.ToLower().Contains(search));
        }

        // 3. Toplam Kayıt ve Sayfalama
        filter.TotalCount = await query.CountAsync();
        filter.PageNumber = Math.Max(1, filter.PageNumber);
        filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var items = await query
            .OrderByDescending(a => a.PlannedStartTime)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AssignmentListItemViewModel
            {
                Id = a.Id,
                VehicleId = a.VehicleId,
                VehiclePlate = a.Vehicle.PlateNumber,
                VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                VehicleCurrentKm = a.Vehicle.CurrentKm,
                DriverId = a.DriverId,
                DriverFullName = a.Driver != null ? a.Driver.User.FullName : (a.IsDriverless ? "Şoförsüz" : (a.IsExternalDriver ? a.ExternalDriverFullName ?? "Dış Şoför" : "-")),
                RequestedByDepartment = a.RequestedByDepartment,
                Destination = a.Destination,
                Purpose = a.Purpose,
                Route = a.Route,
                IsDriverless = a.IsDriverless,
                IsExternalDriver = a.IsExternalDriver,
                ExternalDriverFullName = a.ExternalDriverFullName,
                ExternalDriverDepartment = a.ExternalDriverDepartment,
                AssignedDriverNames = a.AssignmentDrivers.Where(ad => !ad.IsDeleted).OrderByDescending(ad => ad.IsPrimary).Select(ad => ad.Driver.User.FullName).ToList(),
                PlannedStartTime = a.PlannedStartTime,
                PlannedEndTime = a.PlannedEndTime,
                ActualStartTime = a.ActualStartTime,
                ActualEndTime = a.ActualEndTime,
                StartKm = a.StartKm,
                EndKm = a.EndKm,
                Status = a.Status,
                StartReportedBySelf = a.StartReportedBySelf,
                StartApprovedByUserId = a.StartApprovedByUserId,
                EndReportedBySelf = a.EndReportedBySelf,
                EndApprovedByUserId = a.EndApprovedByUserId,
                HourlyRate = a.HourlyRate,
                CalculatedDutyHours = a.CalculatedDutyHours,
                TotalDriverFee = a.TotalDriverFee,
                AccommodationNights = a.AccommodationNights,
                AccommodationCost = a.AccommodationCost,
                PerDiemType = a.PerDiemType,
                PerDiemHours = a.PerDiemHours,
                PerDiemRate = a.PerDiemRate,
                PerDiemDays = a.PerDiemDays,
                PerDiemAmount = a.PerDiemAmount,
                ExpenseNotes = a.ExpenseNotes
            })
            .ToListAsync();

        filter.Assignments = items;
        return filter;
    }

    public async Task<Assignment?> GetAssignmentByIdAsync(int id)
    {
        return await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AssignmentDetailsViewModel?> GetAssignmentDetailsAsync(int id)
    {
        var a = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .Include(a => a.AssignmentDrivers)
                .ThenInclude(ad => ad.Driver)
                    .ThenInclude(d => d.User)
            .Include(a => a.StartApprovedByUser)
            .Include(a => a.EndApprovedByUser)
            .Include(a => a.SignedDocumentUploadedByUser)
            .Include(a => a.SignedDocuments)
                .ThenInclude(sd => sd.Driver)
                    .ThenInclude(d => d!.User)
            .Include(a => a.SignedDocuments)
                .ThenInclude(sd => sd.UploadedByUser)
            .Include(a => a.OffRouteReportedByUser)
            .Include(a => a.RouteChanges)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a == null)
        {
            return null;
        }

        string? creatorFullName = null;
        if (!string.IsNullOrEmpty(a.CreatedBy))
        {
            var creatorUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == a.CreatedBy);
            var candidate = creatorUser?.FullName ?? creatorUser?.UserName;
            if (!string.IsNullOrWhiteSpace(candidate) && !Guid.TryParse(candidate.Trim(), out _))
            {
                creatorFullName = candidate.Trim();
            }
        }

        var auditLogs = await _context.AuditLogs
            .Include(al => al.ChangedByUser)
            .Where(al => al.EntityName == nameof(Assignment) && al.EntityId == id)
            .OrderByDescending(al => al.ChangedAt)
            .ToListAsync();

        string? startReportedBy = null;
        string? endReportedBy = null;
        string? checksCompletedBy = null;

        var startLog = auditLogs.FirstOrDefault(al => al.NewValues != null && al.NewValues.Contains("StartReportedBySelf\":true"));
        if (startLog != null)
        {
            startReportedBy = startLog.ChangedByUser?.FullName ?? startLog.ChangedByUser?.UserName;
        }

        var endLog = auditLogs.FirstOrDefault(al => al.NewValues != null && al.NewValues.Contains("EndReportedBySelf\":true"));
        if (endLog != null)
        {
            endReportedBy = endLog.ChangedByUser?.FullName ?? endLog.ChangedByUser?.UserName;
        }

        var checksLog = auditLogs.FirstOrDefault(al => al.NewValues != null && (al.NewValues.Contains("CheckOil") || al.NewValues.Contains("ChecksCompletedAt")) && !al.NewValues.Contains("EndReportedBySelf\":true"));
        if (checksLog != null)
        {
            checksCompletedBy = checksLog.ChangedByUser?.FullName ?? checksLog.ChangedByUser?.UserName;
        }

        var reviewerUserIds = a.RouteChanges
            .Where(rc => !rc.IsDeleted && !string.IsNullOrEmpty(rc.ReviewedByUserId))
            .Select(rc => rc.ReviewedByUserId!)
            .Distinct()
            .ToList();

        var reviewerNames = reviewerUserIds.Any()
            ? await _context.Users
                .Where(u => reviewerUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "Yetkili")
            : new Dictionary<string, string>();

        var assignedDrivers = a.AssignmentDrivers
            .Where(ad => !ad.IsDeleted)
            .OrderByDescending(ad => ad.IsPrimary)
            .ThenBy(ad => ad.Driver.User.FullName)
            .Select(ad => new AssignedDriverDetailItemViewModel
            {
                AssignmentDriverId = ad.Id,
                DriverId = ad.DriverId,
                DriverFullName = ad.Driver.User.FullName,
                DriverPhone = ad.Driver.User.PhoneNumber ?? "-",
                DriverEmail = ad.Driver.User.Email ?? "-",
                Department = ad.Driver.Department,
                LicenseNumber = ad.Driver.LicenseNumber,
                LicenseClass = ad.Driver.LicenseClass,
                LicenseExpiryDate = ad.Driver.LicenseExpiryDate,
                IsPrimary = ad.IsPrimary
            })
            .ToList();

        var primaryDriver = assignedDrivers.FirstOrDefault(d => d.IsPrimary) ?? assignedDrivers.FirstOrDefault();

        var signedDocuments = a.SignedDocuments
            .Where(sd => !sd.IsDeleted)
            .OrderByDescending(sd => sd.UploadedAt)
            .Select(sd => new AssignmentSignedDocumentItemViewModel
            {
                Id = sd.Id,
                AssignmentId = sd.AssignmentId,
                DriverId = sd.DriverId,
                DriverFullName = sd.Driver?.User?.FullName,
                OriginalFileName = sd.OriginalFileName,
                FileSizeBytes = sd.FileSizeBytes,
                Description = sd.Description,
                UploadedAt = sd.UploadedAt,
                UploadedByUserName = sd.UploadedByUser?.FullName ?? sd.UploadedByUser?.UserName
            })
            .ToList();

        // Geriye dönük uyumluluk: Yeni tabloda kayıt yoksa ancak eski tekil alanda belge varsa listede göster
        if (signedDocuments.Count == 0 && !string.IsNullOrWhiteSpace(a.SignedDocumentPath))
        {
            signedDocuments.Add(new AssignmentSignedDocumentItemViewModel
            {
                Id = 0,
                AssignmentId = a.Id,
                OriginalFileName = a.SignedDocumentOriginalFileName ?? Path.GetFileName(a.SignedDocumentPath),
                UploadedAt = a.SignedDocumentUploadedAt ?? a.UpdatedAt ?? a.CreatedAt,
                UploadedByUserName = a.SignedDocumentUploadedByUser?.FullName
            });
        }

        return new AssignmentDetailsViewModel
        {
            Id = a.Id,
            VehicleId = a.VehicleId,
            VehiclePlate = a.Vehicle.PlateNumber,
            VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
            VehicleType = a.Vehicle.VehicleType,
            VehicleColor = a.Vehicle.Color,
            FuelType = a.Vehicle.FuelType,
            VehicleCurrentKm = a.Vehicle.CurrentKm,
            DriverId = a.DriverId,
            DriverFullName = primaryDriver?.DriverFullName ?? a.Driver?.User?.FullName ?? (a.IsDriverless ? "Şoförsüz" : (a.IsExternalDriver ? a.ExternalDriverFullName ?? "Dış Şoför" : "-")),
            DriverPhone = primaryDriver?.DriverPhone ?? a.Driver?.User?.PhoneNumber ?? "-",
            DriverEmail = primaryDriver?.DriverEmail ?? a.Driver?.User?.Email ?? "-",
            DriverDepartment = primaryDriver?.Department ?? a.Driver?.Department ?? (a.IsExternalDriver ? a.ExternalDriverDepartment ?? "-" : "-"),
            LicenseNumber = primaryDriver?.LicenseNumber ?? a.Driver?.LicenseNumber ?? "-",
            LicenseClass = primaryDriver?.LicenseClass ?? a.Driver?.LicenseClass ?? "-",
            LicenseExpiryDate = primaryDriver?.LicenseExpiryDate ?? a.Driver?.LicenseExpiryDate ?? DateTime.MinValue,
            AssignedDrivers = assignedDrivers,
            RequestedByDepartment = a.RequestedByDepartment,
            Destination = a.Destination,
            Purpose = a.Purpose,
            Route = a.Route,
            IsDriverless = a.IsDriverless,
            IsExternalDriver = a.IsExternalDriver,
            ExternalDriverFullName = a.ExternalDriverFullName,
            ExternalDriverDepartment = a.ExternalDriverDepartment,
            PlannedStartTime = a.PlannedStartTime,
            PlannedEndTime = a.PlannedEndTime,
            ActualStartTime = a.ActualStartTime,
            ActualEndTime = a.ActualEndTime,
            StartKm = a.StartKm,
            EndKm = a.EndKm,
            Status = a.Status,
            CancellationReason = a.CancellationReason,
            DriverReturnNotes = a.DriverReturnNotes,
            CreatedAt = a.CreatedAt,
            CreatedBy = a.CreatedBy,
            CreatedByUserName = creatorFullName,
            UpdatedAt = a.UpdatedAt,
            StartReportedBySelf = a.StartReportedBySelf,
            StartApprovedByUserId = a.StartApprovedByUserId,
            StartApprovedByUserName = a.StartApprovedByUser?.FullName,
            EndReportedBySelf = a.EndReportedBySelf,
            EndApprovedByUserId = a.EndApprovedByUserId,
            EndApprovedByUserName = a.EndApprovedByUser?.FullName,
            StartReportedByDriverName = startReportedBy,
            EndReportedByDriverName = endReportedBy,
            ChecksCompletedByDriverName = checksCompletedBy,
            AccommodationNights = a.AccommodationNights,
            AccommodationCost = a.AccommodationCost,
            PerDiemType = a.PerDiemType,
            PerDiemHours = a.PerDiemHours,
            PerDiemRate = a.PerDiemRate,
            PerDiemDays = a.PerDiemDays,
            PerDiemAmount = a.PerDiemAmount,
            ExpenseNotes = a.ExpenseNotes,
            HourlyRate = a.HourlyRate,
            CalculatedDutyHours = a.CalculatedDutyHours,
            TotalDriverFee = a.TotalDriverFee,
            SignedDocumentPath = a.SignedDocumentPath,
            SignedDocumentOriginalFileName = a.SignedDocumentOriginalFileName,
            SignedDocumentUploadedAt = a.SignedDocumentUploadedAt,
            SignedDocumentUploadedByUserId = a.SignedDocumentUploadedByUserId,
            SignedDocumentUploadedByUserName = a.SignedDocumentUploadedByUser?.FullName,
            SignedDocuments = signedDocuments,
            OffRouteDispatcherName = a.OffRouteDispatcherName,
            OffRouteDispatcherTitle = a.OffRouteDispatcherTitle,
            OffRouteReason = a.OffRouteReason,
            OffRouteReportedAt = a.OffRouteReportedAt,
            OffRouteReportedByUserName = a.OffRouteReportedByUser?.FullName,
            CheckOil = a.CheckOil,
            CheckWater = a.CheckWater,
            CheckFuel = a.CheckFuel,
            CheckHydraulic = a.CheckHydraulic,
            CheckTires = a.CheckTires,
            ChecksCompletedAt = a.ChecksCompletedAt,
            RouteChanges = a.RouteChanges
                .Where(rc => !rc.IsDeleted)
                .OrderByDescending(rc => rc.CreatedAt)
                .Select(rc => new AssignmentRouteChangeItemViewModel
                {
                    Id = rc.Id,
                    AssignmentId = rc.AssignmentId,
                    OldRoute = rc.OldRoute,
                    NewRoute = rc.NewRoute,
                    OldDestination = rc.OldDestination,
                    NewDestination = rc.NewDestination,
                    Reason = rc.Reason,
                    ChangedByUserId = rc.ChangedByUserId,
                    ChangedByFullName = rc.ChangedByFullName,
                    CreatedAt = rc.CreatedAt,
                    IsReviewedByAdmin = rc.IsReviewedByAdmin,
                    ReviewedAt = rc.ReviewedAt,
                    ReviewedByUserId = rc.ReviewedByUserId,
                    ReviewedByFullName = (!string.IsNullOrEmpty(rc.ReviewedByUserId) && reviewerNames.TryGetValue(rc.ReviewedByUserId, out var revName)) ? revName : null
                })
                .ToList()
        };
    }

    public async Task<(bool Success, string Message)> SaveOffRouteDispatchAsync(OffRouteDispatchViewModel model, string userId)
    {
        if (model == null) return (false, "Geçersiz veri gönderildi.");

        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == model.AssignmentId);
        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        assignment.OffRouteDispatcherName = model.DispatcherName.Trim();
        assignment.OffRouteDispatcherTitle = model.DispatcherTitle?.Trim();
        assignment.OffRouteReason = model.Reason.Trim();
        assignment.OffRouteReportedAt = DateTime.Now;
        assignment.OffRouteReportedByUserId = userId;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = user?.FullName ?? user?.UserName ?? "Yetkili";

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(Assignment),
            assignment.Id,
            AuditAction.Update,
            userId,
            user?.FullName ?? user?.UserName ?? "Yetkili",
            new
            {
                AssignmentId = assignment.Id,
                Action = "OffRouteDispatchRecorded",
                DispatcherName = assignment.OffRouteDispatcherName,
                DispatcherTitle = assignment.OffRouteDispatcherTitle,
                Reason = assignment.OffRouteReason
            });

        return (true, "Acil güzergah dışı sevk bilgisi başarıyla kaydedildi.");
    }

    /// <summary>
    /// Şefin Şoför Güzergah Değişikliğini Onaylaması (Güzergah Dışı Sevk Kaydına Aktarır)
    /// </summary>
    public async Task<(bool Success, string Message, int AssignmentId)> ApproveRouteChangeAsync(int routeChangeId, string reviewerUserId)
    {
        var rc = await _context.AssignmentRouteChanges
            .Include(r => r.Assignment)
            .FirstOrDefaultAsync(r => r.Id == routeChangeId && !r.IsDeleted);

        if (rc == null || rc.Assignment == null)
        {
            return (false, "Güzergah değişikliği kaydı bulunamadı.", 0);
        }

        var assignment = rc.Assignment;
        var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Id == reviewerUserId);
        var reviewerName = reviewer?.FullName ?? reviewer?.UserName ?? "Ulaşım Şefi";

        rc.IsReviewedByAdmin = true;
        rc.ReviewedAt = DateTime.UtcNow;
        rc.ReviewedByUserId = reviewerUserId;

        // Görevin resmi sevk alanlarını güncelle: Onaylayan şef ve ek güzergah bilgileri resmi belgeye işlenir
        assignment.OffRouteDispatcherName = reviewerName;
        assignment.OffRouteDispatcherTitle = "Ulaşım Şefi";

        var destSuffix = !string.IsNullOrWhiteSpace(rc.NewDestination) ? $" (Hedef: {rc.NewDestination})" : "";
        assignment.OffRouteReason = $"Ek Güzergah: {rc.NewRoute}{destSuffix} | Gerekçe: {rc.Reason}";
        assignment.OffRouteReportedAt = DateTime.Now;
        assignment.OffRouteReportedByUserId = reviewerUserId;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = reviewerName;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(Assignment),
            assignment.Id,
            AuditAction.Update,
            reviewerUserId,
            reviewerName,
            new
            {
                AssignmentId = assignment.Id,
                Action = "RouteChangeApprovedByChief",
                RouteChangeId = rc.Id,
                NewRoute = rc.NewRoute,
                NewDestination = rc.NewDestination,
                Reason = rc.Reason,
                ApprovedBy = reviewerName
            });

        return (true, "Şoförün ek güzergah değişikliği onaylandı ve resmi Taşıt Görev Emri sevk alanına işlendi.", assignment.Id);
    }

    public async Task<AssignmentEditViewModel?> GetAssignmentEditModelAsync(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            return null;
        }

        var allDrivers = assignment.AssignmentDrivers
            .Where(ad => !ad.IsDeleted)
            .OrderByDescending(ad => ad.IsPrimary)
            .Select(ad => ad.DriverId)
            .ToList();

        if (allDrivers.Count == 0 && assignment.DriverId.HasValue)
        {
            allDrivers.Add(assignment.DriverId.Value);
        }

        var primaryDriverId = assignment.DriverId ?? allDrivers.FirstOrDefault();
        var additionalDriverIds = allDrivers.Where(id => id != primaryDriverId).ToList();

        var model = new AssignmentEditViewModel
        {
            Id = assignment.Id,
            VehicleId = assignment.VehicleId,
            DriverId = primaryDriverId,
            DriverIds = additionalDriverIds,
            RequestedByDepartment = assignment.RequestedByDepartment,
            Destination = assignment.Destination,
            Purpose = assignment.Purpose,
            Route = assignment.Route,
            IsDriverless = assignment.IsDriverless,
            IsExternalDriver = assignment.IsExternalDriver,
            ExternalDriverFullName = assignment.ExternalDriverFullName,
            ExternalDriverDepartment = assignment.ExternalDriverDepartment,
            PlannedStartTime = assignment.PlannedStartTime,
            PlannedEndTime = assignment.PlannedEndTime,
            Status = assignment.Status,
            AccommodationNights = assignment.AccommodationNights,
            AccommodationCost = assignment.AccommodationCost,
            PerDiemType = assignment.PerDiemType ?? "Hourly",
            PerDiemHours = assignment.PerDiemHours,
            PerDiemRate = assignment.PerDiemRate,
            PerDiemDays = assignment.PerDiemDays,
            PerDiemAmount = assignment.PerDiemAmount,
            ExpenseNotes = assignment.ExpenseNotes
        };

        return await PopulateEditDropdownsAsync(model);
    }

    public async Task<AssignmentCreateViewModel> GetCreateViewModelAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        var start = startTime ?? DateTime.Today.AddHours(9);
        var end = endTime ?? DateTime.Today.AddHours(17);

        var model = new AssignmentCreateViewModel
        {
            PlannedStartTime = start,
            PlannedEndTime = end
        };

        // Devam eden aktif araç görevlerini çek (Hangi araç seyirde?)
        var activeVehicleDuties = await _context.Assignments
            .Where(a => !a.IsDeleted && a.Status == AssignmentStatus.InProgress)
            .ToDictionaryAsync(a => a.VehicleId, a => a);

        // Araçları çek ve müsaitliklerini kontrol et
        var vehicles = await _context.Vehicles.OrderBy(v => v.PlateNumber).ToListAsync();
        foreach (var v in vehicles)
        {
            var (isAvail, reason, _) = await CheckVehicleAvailabilityAsync(v.Id, start, end);
            var isOverdue = false;
            DateTime? overdueSince = null;
            string? overdueWarning = null;

            if (activeVehicleDuties.TryGetValue(v.Id, out var activeDuty))
            {
                if (activeDuty.PlannedEndTime < DateTime.Now)
                {
                    isOverdue = true;
                    overdueSince = activeDuty.PlannedEndTime;
                    overdueWarning = $"Araç {activeDuty.PlannedEndTime:dd.MM.yyyy HH:mm} tarihinden beri önceki görevinde teslim edilmemiş görünüyor.";
                    // Kural: Engelleme yok! Seçilebilir olmalı, onay penceresiyle yönetilir
                    isAvail = true;
                }
            }

            model.AvailableVehicles.Add(new SelectableVehicleViewModel
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                BrandModel = $"{v.Brand} {v.Model}",
                CurrentKm = v.CurrentKm,
                IsAvailable = isAvail,
                UnavailableReason = reason,
                IsOverdueOnDuty = isOverdue,
                OverdueSince = overdueSince,
                OverdueWarningText = overdueWarning
            });
        }

        // Devam eden aktif görevleri çek (Hangi şoför hangi araçla seyirde?)
        var activeDriverDuties = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .Where(a => !a.IsDeleted && a.Status == AssignmentStatus.InProgress)
            .ToListAsync();

        // Şoförleri çek ve müsaitliklerini + ehliyet durumlarını kontrol et
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Include(d => d.DriverLicenses)
            .Where(d => d.IsActive)
            .OrderBy(d => d.User.FullName)
            .ToListAsync();

        foreach (var d in drivers)
        {
            var (isAvail, reason, _) = await CheckDriverAvailabilityAsync(d.Id, start, end);
            var activeDutyForDriver = activeDriverDuties.FirstOrDefault(a => a.DriverId == d.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == d.Id));
            var isOnDuty = activeDutyForDriver != null;
            var lics = d.DriverLicenses.Where(l => !l.IsDeleted).ToList();
            var dClass = lics.Any() ? string.Join(", ", lics.Select(l => l.LicenseClass)) : (d.LicenseClass ?? "");
            var dExpiry = lics.Any() ? lics.Max(l => l.LicenseExpiryDate) : d.LicenseExpiryDate;

            model.AvailableDrivers.Add(new SelectableDriverViewModel
            {
                Id = d.Id,
                FullName = d.User.FullName,
                LicenseClass = dClass,
                LicenseExpiryDate = dExpiry,
                Department = d.Department,
                IsAvailable = isAvail,
                UnavailableReason = reason,
                IsCurrentlyOnDuty = isOnDuty,
                ActiveDutyPlate = activeDutyForDriver?.Vehicle?.PlateNumber
            });
        }

        // Müsait olanları üstte göster (Önce normal müsaitler, sonra gecikmiş seyirdekiler, en altta kullanılamayanlar)
        model.AvailableVehicles = model.AvailableVehicles
            .OrderByDescending(v => v.IsAvailable && !v.IsOverdueOnDuty)
            .ThenByDescending(v => v.IsOverdueOnDuty)
            .ThenBy(v => v.PlateNumber)
            .ToList();
        model.AvailableDrivers = model.AvailableDrivers.OrderByDescending(d => d.IsAvailable).ThenBy(d => d.FullName).ToList();

        return model;
    }

    public async Task<AssignmentEditViewModel> PopulateEditDropdownsAsync(AssignmentEditViewModel model)
    {
        var start = model.PlannedStartTime;
        var end = model.PlannedEndTime;

        model.AvailableVehicles.Clear();
        model.AvailableDrivers.Clear();

        var activeVehicleDutiesEdit = await _context.Assignments
            .Where(a => !a.IsDeleted && a.Status == AssignmentStatus.InProgress && (model.Id == 0 || a.Id != model.Id))
            .ToDictionaryAsync(a => a.VehicleId, a => a);

        var vehicles = await _context.Vehicles.OrderBy(v => v.PlateNumber).ToListAsync();
        foreach (var v in vehicles)
        {
            var (isAvail, reason, _) = await CheckVehicleAvailabilityAsync(v.Id, start, end, model.Id);
            var isOverdue = false;
            DateTime? overdueSince = null;
            string? overdueWarning = null;

            if (activeVehicleDutiesEdit.TryGetValue(v.Id, out var activeDuty))
            {
                if (activeDuty.PlannedEndTime < DateTime.Now)
                {
                    isOverdue = true;
                    overdueSince = activeDuty.PlannedEndTime;
                    overdueWarning = $"Araç {activeDuty.PlannedEndTime:dd.MM.yyyy HH:mm} tarihinden beri önceki görevinde teslim edilmemiş görünüyor.";
                    isAvail = true;
                }
            }

            model.AvailableVehicles.Add(new SelectableVehicleViewModel
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                BrandModel = $"{v.Brand} {v.Model}",
                CurrentKm = v.CurrentKm,
                IsAvailable = isAvail,
                UnavailableReason = reason,
                IsOverdueOnDuty = isOverdue,
                OverdueSince = overdueSince,
                OverdueWarningText = overdueWarning
            });
        }

        model.AvailableVehicles = model.AvailableVehicles
            .OrderByDescending(v => v.IsAvailable && !v.IsOverdueOnDuty)
            .ThenByDescending(v => v.IsOverdueOnDuty)
            .ThenBy(v => v.PlateNumber)
            .ToList();

        var activeDriverDutiesEdit = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .Where(a => !a.IsDeleted && a.Status == AssignmentStatus.InProgress && (model.Id == 0 || a.Id != model.Id))
            .ToListAsync();

        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Include(d => d.DriverLicenses)
            .Where(d => d.IsActive)
            .OrderBy(d => d.User.FullName)
            .ToListAsync();

        foreach (var d in drivers)
        {
            var (isAvail, reason, _) = await CheckDriverAvailabilityAsync(d.Id, start, end, model.Id);
            var activeDutyForDriver = activeDriverDutiesEdit.FirstOrDefault(a => a.DriverId == d.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == d.Id));
            var isOnDuty = activeDutyForDriver != null;
            var lics = d.DriverLicenses.Where(l => !l.IsDeleted).ToList();
            var dClass = lics.Any() ? string.Join(", ", lics.Select(l => l.LicenseClass)) : (d.LicenseClass ?? "");
            var dExpiry = lics.Any() ? lics.Max(l => l.LicenseExpiryDate) : d.LicenseExpiryDate;

            model.AvailableDrivers.Add(new SelectableDriverViewModel
            {
                Id = d.Id,
                FullName = d.User.FullName,
                LicenseClass = dClass,
                LicenseExpiryDate = dExpiry,
                Department = d.Department,
                IsAvailable = isAvail,
                UnavailableReason = reason,
                IsCurrentlyOnDuty = isOnDuty,
                ActiveDutyPlate = activeDutyForDriver?.Vehicle?.PlateNumber
            });
        }

        model.AvailableVehicles = model.AvailableVehicles.OrderByDescending(v => v.IsAvailable).ThenBy(v => v.PlateNumber).ToList();
        model.AvailableDrivers = model.AvailableDrivers.OrderByDescending(d => d.IsAvailable).ThenBy(d => d.FullName).ToList();

        return model;
    }

    public async Task<(bool IsAvailable, string? Reason, int? ConflictingAssignmentId)> CheckVehicleAvailabilityAsync(
        int vehicleId, DateTime start, DateTime end, int? excludeAssignmentId = null)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            return (false, "Araç bulunamadı.", null);
        }

        // 1. Araç operasyonel durum ve muayene/sigorta tarih kontrolleri (Status + Doğrudan Tarih)
        var statusCheck = ValidateVehicleOperationalStatus(vehicle);
        if (!statusCheck.IsValid)
        {
            return (false, statusCheck.ErrorMessage, null);
        }

        // 2. Devam eden bakım kontrolü
        var inMaintenance = await _context.Maintenances
            .AnyAsync(m => m.VehicleId == vehicleId && m.EndDate == null);

        if (inMaintenance)
        {
            return (false, "Aracın devam eden bir servis/bakım kaydı bulunmaktadır.", null);
        }

        // 3. Devam eden aktif görev kontrolü (Araç şu anda fiilen seyirde mi?)
        var activeDuty = await _context.Assignments
            .Where(a => a.VehicleId == vehicleId &&
                        !a.IsDeleted &&
                        (excludeAssignmentId == null || a.Id != excludeAssignmentId.Value) &&
                        a.Status == AssignmentStatus.InProgress)
            .FirstOrDefaultAsync();

        if (activeDuty != null)
        {
            if (activeDuty.PlannedEndTime < DateTime.Now)
            {
                // Planlanan süresi geçmiş ama araç henüz teslim edilmemiş (Gecikmiş Seyirde):
                // İş Kuralı: Şef onay penceresiyle aracı atayabilir (Engelleme YOK!).
            }
            else if (start <= activeDuty.PlannedEndTime || start <= DateTime.Now)
            {
                return (false, $"Araç şu anda aktif görevde seyirdedir (Tahmini Dönüş: {activeDuty.PlannedEndTime:dd.MM HH:mm})", activeDuty.Id);
            }
        }

        // 4. Çifte rezervasyon kontrolü (Planned ve InProgress görevlerle çakışma)
        var conflict = await _context.Assignments
            .Where(a => a.VehicleId == vehicleId &&
                        (excludeAssignmentId == null || a.Id != excludeAssignmentId.Value) &&
                        (a.Status == AssignmentStatus.Planned || (a.Status == AssignmentStatus.InProgress && a.PlannedEndTime >= DateTime.Now)) &&
                        start < a.PlannedEndTime &&
                        end > a.PlannedStartTime)
            .FirstOrDefaultAsync();

        if (conflict != null)
        {
            var statusStr = conflict.Status == AssignmentStatus.InProgress ? "Devam Eden Görevde" : "Planlı Görevde";
            return (false, $"Çakışan Görev ({statusStr}: {conflict.PlannedStartTime:dd.MM HH:mm} - {conflict.PlannedEndTime:dd.MM HH:mm})", conflict.Id);
        }

        return (true, null, null);
    }

    public async Task<(bool IsAvailable, string? Reason, int? ConflictingAssignmentId)> CheckDriverAvailabilityAsync(
        int driverId, DateTime start, DateTime end, int? excludeAssignmentId = null)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .Include(d => d.DriverLicenses)
            .FirstOrDefaultAsync(d => d.Id == driverId);
        if (driver == null)
        {
            return (false, "Şoför bulunamadı.", null);
        }

        // 1. Aktiflik kontrolü
        if (!driver.IsActive)
        {
            return (false, "Şoför hesabı pasif durumdadır.", null);
        }

        // 1.1 Yetki Devri (Vekâlet) Kontrolü (DÜZELTME 7: Şoför bu saat aralığında Şef vekili ise seçilemez)
        var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
        var endUtc = end.Kind == DateTimeKind.Utc ? end : end.ToUniversalTime();
        if (!string.IsNullOrEmpty(driver.UserId))
        {
            var isDelegated = await _context.RoleDelegations
                .AnyAsync(rd => rd.DelegateeUserId == driver.UserId &&
                                rd.ActualRevokedAt == null &&
                                !rd.IsDeleted &&
                                startUtc < (rd.ActualRevokedAt ?? rd.EndDate) &&
                                endUtc > rd.StartDate);

            if (isDelegated)
            {
                return (false, "Şoför bu tarih aralığında Ulaşım Şefliği görevini vekâleten yürütmektedir; görev atanamaz.", null);
            }
        }

        // 2. Ehliyet geçerlilik süresi kontrolü (Kritik İş Kuralı - Çoklu Ehliyet)
        // Şoförün EN AZ BİR ehliyetinin geçerlilik tarihi görev bitişini kapsıyorsa şoför uygundur. Araç türüne göre sınıf eşleştirmesi yapılmaz.
        var activeLicenses = driver.DriverLicenses.Where(l => !l.IsDeleted).ToList();
        bool hasValidLicense;
        DateTime latestExpiry;

        if (activeLicenses.Any())
        {
            hasValidLicense = activeLicenses.Any(l => l.LicenseExpiryDate.Date >= end.Date);
            latestExpiry = activeLicenses.Max(l => l.LicenseExpiryDate);
        }
        else
        {
            hasValidLicense = driver.LicenseExpiryDate.Date >= end.Date;
            latestExpiry = driver.LicenseExpiryDate;
        }

        if (!hasValidLicense)
        {
            return (false, $"Ehliyet süresi yetersiz (Son Geçerlilik: {latestExpiry:dd.MM.yyyy})", null);
        }

        // 3. Devam eden aktif görev kontrolü (Şoför şu anda fiilen seyirde mi?)
        var activeDuty = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .Where(a => !a.IsDeleted &&
                        (a.DriverId == driverId || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driverId)) &&
                        (excludeAssignmentId == null || a.Id != excludeAssignmentId.Value) &&
                        a.Status == AssignmentStatus.InProgress)
            .FirstOrDefaultAsync();

        if (activeDuty != null)
        {
            var plate = activeDuty.Vehicle?.PlateNumber ?? "Araç";
            // Kural: Şoför fiilen yoldayken/görevdeyken; yeni görevin başlangıcı bugünü/şu anı kapsıyorsa veya
            // devam eden görevin açık olduğu dönemi kapsıyorsa yeni görev atanamaz.
            if (start <= DateTime.Now || start <= activeDuty.PlannedEndTime)
            {
                return (false, $"Şu anda aktif görevde seyirdedir ({plate})", activeDuty.Id);
            }
        }

        // 4. Çifte rezervasyon kontrolü (Planned ve InProgress görevlerle tarih çakışması)
        var conflict = await _context.Assignments
            .Include(a => a.AssignmentDrivers)
            .Where(a => !a.IsDeleted &&
                        (a.DriverId == driverId || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driverId)) &&
                        (excludeAssignmentId == null || a.Id != excludeAssignmentId.Value) &&
                        (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress) &&
                        start < a.PlannedEndTime &&
                        end > a.PlannedStartTime)
            .FirstOrDefaultAsync();

        if (conflict != null)
        {
            var statusStr = conflict.Status == AssignmentStatus.InProgress ? "Devam Eden Görevde" : "Planlı Görevde";
            return (false, $"Çakışan Görev ({statusStr}: {conflict.PlannedStartTime:dd.MM HH:mm} - {conflict.PlannedEndTime:dd.MM HH:mm})", conflict.Id);
        }

        return (true, null, null);
    }

    public async Task SyncAssignmentDriversAsync(Assignment assignment, List<int>? driverIds, bool isDriverless, bool isExternalDriver, string userId)
    {
        var existing = await _context.AssignmentDrivers
            .Where(ad => ad.AssignmentId == assignment.Id)
            .ToListAsync();

        if (isDriverless || isExternalDriver)
        {
            assignment.DriverId = null;
            foreach (var ed in existing)
            {
                ed.IsDeleted = true;
                ed.UpdatedAt = DateTime.UtcNow;
                ed.UpdatedBy = userId;
            }
            return;
        }

        var distinctIds = driverIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        if (distinctIds.Count > 0)
        {
            assignment.DriverId = distinctIds[0];
        }

        // Çıkarılanları soft-delete yap
        foreach (var ed in existing)
        {
            if (!distinctIds.Contains(ed.DriverId))
            {
                ed.IsDeleted = true;
                ed.UpdatedAt = DateTime.UtcNow;
                ed.UpdatedBy = userId;
            }
        }

        // Eklenen veya var olanları güncelle
        for (int i = 0; i < distinctIds.Count; i++)
        {
            var dId = distinctIds[i];
            var isPrimary = (i == 0);
            var match = existing.FirstOrDefault(ad => ad.DriverId == dId);
            if (match != null)
            {
                match.IsDeleted = false;
                match.IsPrimary = isPrimary;
                match.UpdatedAt = DateTime.UtcNow;
                match.UpdatedBy = userId;
            }
            else
            {
                var newAd = new AssignmentDriver
                {
                    AssignmentId = assignment.Id,
                    DriverId = dId,
                    IsPrimary = isPrimary,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    IsDeleted = false
                };
                _context.AssignmentDrivers.Add(newAd);
            }
        }
    }

    public async Task<(bool Success, string Message, int AssignmentId)> CreateAssignmentAsync(AssignmentCreateViewModel model, string adminUserId)
    {
        // 1. Tarih validasyonu
        if (model.PlannedEndTime <= model.PlannedStartTime)
        {
            return (false, "Planlanan bitiş zamanı, başlangıç zamanından ileri bir tarih/saat olmalıdır.", 0);
        }

        // 2. Araç müsaitlik ve çifte rezervasyon kontrolü
        var (vehicleAvailable, vehicleReason, _) = await CheckVehicleAvailabilityAsync(
            model.VehicleId, model.PlannedStartTime, model.PlannedEndTime);

        if (!vehicleAvailable)
        {
            return (false, $"Seçilen araç müsait değildir: {vehicleReason}", 0);
        }

        // 3. Şoför Modu ve Kontrolleri
        List<int> validDriverIds = new();
        if (model.IsDriverless)
        {
            model.IsExternalDriver = false;
            model.DriverId = null;
            model.DriverIds = null;
            model.ExternalDriverFullName = null;
            model.ExternalDriverDepartment = null;
        }
        else if (model.IsExternalDriver)
        {
            model.IsDriverless = false;
            model.DriverId = null;
            model.DriverIds = null;
            if (string.IsNullOrWhiteSpace(model.ExternalDriverFullName))
            {
                return (false, "Dışarıdan şoför görevlendirmelerinde şoförün Adı Soyadı girilmelidir.", 0);
            }
        }
        else
        {
            var allProvidedDriverIds = new List<int>();

            // 1. Asıl / Birincil Şoför (model.DriverId) daima 1. sırada (Asıl Şoför) yer alır
            if (model.DriverId.HasValue && model.DriverId.Value > 0)
            {
                allProvidedDriverIds.Add(model.DriverId.Value);
            }

            // 2. Ek Şoförler (model.DriverIds) sırayla eklenir
            if (model.DriverIds != null && model.DriverIds.Count > 0)
            {
                foreach (var extraId in model.DriverIds)
                {
                    if (extraId > 0)
                    {
                        allProvidedDriverIds.Add(extraId);
                    }
                }
            }

            if (allProvidedDriverIds.Count == 0)
            {
                return (false, "Lütfen görevlendirilecek en az bir kurum şoförü seçiniz.", 0);
            }

            // Mükerrer şoför kontrolü (Asıl şoför ile ek şoförün veya ek şoförlerin birbiriyle aynı seçilmesi)
            if (allProvidedDriverIds.Count != allProvidedDriverIds.Distinct().Count())
            {
                return (false, "Aynı şoför bir göreve birden fazla kez eklenemez.", 0);
            }

            validDriverIds = allProvidedDriverIds;

            // Her şoför için müsaitlik ve ehliyet kontrolü
            foreach (var dId in validDriverIds)
            {
                var (driverAvailable, driverReason, _) = await CheckDriverAvailabilityAsync(
                    dId, model.PlannedStartTime, model.PlannedEndTime);

                if (!driverAvailable)
                {
                    var driverObj = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == dId);
                    var dName = driverObj?.User?.FullName ?? $"Şoför (ID: {dId})";
                    return (false, $"{dName} müsait değildir: {driverReason}", 0);
                }
            }
        }

        var assignment = new Assignment
        {
            VehicleId = model.VehicleId,
            DriverId = validDriverIds.Count > 0 ? validDriverIds[0] : null,
            RequestedByDepartment = model.RequestedByDepartment.Trim(),
            Destination = model.Destination?.Trim(),
            Purpose = model.Purpose.Trim(),
            Route = model.Route?.Trim(),
            IsDriverless = model.IsDriverless,
            IsExternalDriver = model.IsExternalDriver,
            ExternalDriverFullName = model.IsExternalDriver ? model.ExternalDriverFullName?.Trim() : null,
            ExternalDriverDepartment = model.IsExternalDriver ? model.ExternalDriverDepartment?.Trim() : null,
            PlannedStartTime = model.PlannedStartTime,
            PlannedEndTime = model.PlannedEndTime,
            AccommodationNights = model.AccommodationNights,
            AccommodationCost = model.AccommodationCost,
            PerDiemType = model.PerDiemType ?? "Hourly",
            PerDiemHours = model.PerDiemHours,
            PerDiemRate = model.PerDiemRate,
            PerDiemDays = model.PerDiemDays,
            PerDiemAmount = model.PerDiemAmount,
            ExpenseNotes = model.ExpenseNotes?.Trim(),
            Status = AssignmentStatus.Planned,
            CreatedBy = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        var overdueDuty = await _context.Assignments
            .Where(a => a.VehicleId == model.VehicleId && !a.IsDeleted && a.Status == AssignmentStatus.InProgress && a.PlannedEndTime < DateTime.Now)
            .FirstOrDefaultAsync();

        if (overdueDuty != null)
        {
            _logger.LogWarning("Araç {VehicleId}, önceki görevi (#{DutyId}) teslim alınmadan Şef onayıyla yeni göreve tahsis edildi.", model.VehicleId, overdueDuty.Id);
        }

        try
        {
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            await SyncAssignmentDriversAsync(assignment, validDriverIds, model.IsDriverless, model.IsExternalDriver, adminUserId);
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Create,
                adminUserId,
                null,
                new
                {
                    assignment.VehicleId,
                    assignment.DriverId,
                    assignment.Destination,
                    assignment.Purpose,
                    assignment.Route,
                    assignment.IsDriverless,
                    assignment.IsExternalDriver,
                    assignment.ExternalDriverFullName,
                    AssignedDriverIds = validDriverIds,
                    assignment.PlannedStartTime,
                    assignment.PlannedEndTime,
                    Status = assignment.Status.ToString(),
                    OverdueVehicleAssigned = overdueDuty != null,
                    OverduePreviousDutyId = overdueDuty?.Id,
                    Note = overdueDuty != null ? "Araç önceki görevinden sistemde teslim alınmadan Şef onayıyla tahsis edildi" : null
                }
            );

            _logger.LogInformation("Yeni görev oluşturuldu. Id: {Id}, VehicleId: {VId}, DriverId: {DId}",
                assignment.Id, assignment.VehicleId, assignment.DriverId);

            return (true, "Görevlendirme kaydı başarıyla oluşturuldu.", assignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görev oluşturulurken veritabanı hatası oluştu.");
            return (false, "Görev kaydedilirken bir veritabanı hatası oluştu.", 0);
        }
    }

    public async Task<(bool Success, string Message)> UpdateAssignmentAsync(AssignmentEditViewModel model, string adminUserId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == model.Id);

        if (assignment == null)
        {
            return (false, "Güncellenmek istenen görev kaydı bulunamadı.");
        }

        // Sadece Planned veya InProgress görevler düzenlenebilir
        if (assignment.Status != AssignmentStatus.Planned && assignment.Status != AssignmentStatus.InProgress)
        {
            return (false, $"Yalnızca 'Planlandı' veya 'Seyirde' durumundaki görevler düzenlenebilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        if (model.PlannedEndTime <= model.PlannedStartTime)
        {
            return (false, "Planlanan bitiş zamanı, başlangıç zamanından ileri bir tarih/saat olmalıdır.");
        }

        // Araç müsaitlik kontrolü (Kendi görev kaydı hariç)
        var (vehicleAvailable, vehicleReason, _) = await CheckVehicleAvailabilityAsync(
            model.VehicleId, model.PlannedStartTime, model.PlannedEndTime, model.Id);

        if (!vehicleAvailable)
        {
            return (false, $"Seçilen araç müsait değildir: {vehicleReason}");
        }

        // Şoför Modu ve Doğrulamaları
        List<int> validDriverIds = new();
        if (model.IsDriverless)
        {
            model.IsExternalDriver = false;
            model.DriverId = null;
            model.DriverIds = null;
            model.ExternalDriverFullName = null;
            model.ExternalDriverDepartment = null;
        }
        else if (model.IsExternalDriver)
        {
            model.IsDriverless = false;
            model.DriverId = null;
            model.DriverIds = null;
            if (string.IsNullOrWhiteSpace(model.ExternalDriverFullName))
            {
                return (false, "Dışarıdan şoför görevlendirmelerinde şoförün Adı Soyadı girilmelidir.");
            }
        }
        else
        {
            var allProvidedDriverIds = new List<int>();

            // 1. Asıl / Birincil Şoför (model.DriverId) daima 1. sırada (Asıl Şoför) yer alır
            if (model.DriverId.HasValue && model.DriverId.Value > 0)
            {
                allProvidedDriverIds.Add(model.DriverId.Value);
            }

            // 2. Ek Şoförler (model.DriverIds) sırayla eklenir
            if (model.DriverIds != null && model.DriverIds.Count > 0)
            {
                foreach (var extraId in model.DriverIds)
                {
                    if (extraId > 0)
                    {
                        allProvidedDriverIds.Add(extraId);
                    }
                }
            }

            if (allProvidedDriverIds.Count == 0)
            {
                return (false, "Lütfen görevlendirilecek en az bir kurum şoförü seçiniz.");
            }

            // Mükerrer şoför kontrolü
            if (allProvidedDriverIds.Count != allProvidedDriverIds.Distinct().Count())
            {
                return (false, "Aynı şoför bir göreve birden fazla kez eklenemez.");
            }

            validDriverIds = allProvidedDriverIds;

            // Seyir halindeki görevlerde araç değiştirilemez
            if (assignment.Status == AssignmentStatus.InProgress)
            {
                if (model.VehicleId != assignment.VehicleId)
                {
                    return (false, "Seyir halindeki görevlerde araç değiştirilemez.");
                }
            }

            // Her şoför için müsaitlik kontrolü (Kendi görevi hariç)
            foreach (var dId in validDriverIds)
            {
                var (driverAvailable, driverReason, _) = await CheckDriverAvailabilityAsync(
                    dId, model.PlannedStartTime, model.PlannedEndTime, model.Id);

                if (!driverAvailable)
                {
                    var driverObj = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == dId);
                    var dName = driverObj?.User?.FullName ?? $"Şoför (ID: {dId})";
                    return (false, $"{dName} müsait değildir: {driverReason}");
                }
            }
        }

        var oldState = new
        {
            assignment.VehicleId,
            assignment.DriverId,
            assignment.RequestedByDepartment,
            assignment.Destination,
            assignment.Purpose,
            assignment.Route,
            assignment.IsDriverless,
            assignment.IsExternalDriver,
            assignment.PlannedStartTime,
            assignment.PlannedEndTime
        };

        assignment.VehicleId = model.VehicleId;
        assignment.DriverId = validDriverIds.Count > 0 ? validDriverIds[0] : null;
        assignment.RequestedByDepartment = model.RequestedByDepartment.Trim();
        assignment.Destination = model.Destination?.Trim();
        assignment.Purpose = model.Purpose.Trim();
        assignment.Route = model.Route?.Trim();
        assignment.IsDriverless = model.IsDriverless;
        assignment.IsExternalDriver = model.IsExternalDriver;
        assignment.ExternalDriverFullName = model.IsExternalDriver ? model.ExternalDriverFullName?.Trim() : null;
        assignment.ExternalDriverDepartment = model.IsExternalDriver ? model.ExternalDriverDepartment?.Trim() : null;
        assignment.PlannedStartTime = model.PlannedStartTime;
        assignment.PlannedEndTime = model.PlannedEndTime;
        assignment.AccommodationNights = model.AccommodationNights;
        assignment.AccommodationCost = model.AccommodationCost;
        assignment.PerDiemType = model.PerDiemType ?? "Hourly";
        assignment.PerDiemHours = model.PerDiemHours;
        assignment.PerDiemRate = model.PerDiemRate;
        assignment.PerDiemDays = model.PerDiemDays;
        assignment.PerDiemAmount = model.PerDiemAmount;
        assignment.ExpenseNotes = model.ExpenseNotes?.Trim();
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = adminUserId;

        try
        {
            await SyncAssignmentDriversAsync(assignment, validDriverIds, model.IsDriverless, model.IsExternalDriver, adminUserId);
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                adminUserId,
                oldState,
                new
                {
                    assignment.VehicleId,
                    assignment.DriverId,
                    assignment.RequestedByDepartment,
                    assignment.Destination,
                    assignment.Purpose,
                    assignment.Route,
                    assignment.IsDriverless,
                    assignment.IsExternalDriver,
                    assignment.ExternalDriverFullName,
                    AssignedDriverIds = validDriverIds,
                    assignment.PlannedStartTime,
                    assignment.PlannedEndTime
                }
            );

            _logger.LogInformation("Görev güncellendi. Id: {Id}", assignment.Id);
            return (true, "Görevlendirme bilgileri başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görev güncellenirken hata oluştu. Id: {Id}", model.Id);
            return (false, "Görev güncellenirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ExtendAssignmentDurationAsync(int id, DateTime newPlannedEndTime, string? reason, string adminUserId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        if (assignment.Status != AssignmentStatus.InProgress && assignment.Status != AssignmentStatus.Planned)
        {
            return (false, $"Yalnızca devam eden (Seyirde) veya planlanan görevlerin süresi uzatılabilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        if (newPlannedEndTime <= assignment.PlannedStartTime)
        {
            return (false, "Yeni planlanan bitiş zamanı, görevin başlangıç zamanından ileri bir tarih/saat olmalıdır.");
        }

        if (newPlannedEndTime <= assignment.PlannedEndTime)
        {
            return (false, $"Yeni bitiş tarihi, mevcut planlanan bitiş tarihinden ({assignment.PlannedEndTime:dd.MM.yyyy HH:mm}) daha ileri bir tarih olmalıdır.");
        }

        var oldPlannedEndTime = assignment.PlannedEndTime;
        assignment.PlannedEndTime = newPlannedEndTime;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = adminUserId;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            assignment.ExpenseNotes = string.IsNullOrWhiteSpace(assignment.ExpenseNotes)
                ? $"[Süre Uzatma - {DateTime.Now:dd.MM.yyyy HH:mm}]: {reason.Trim()}"
                : $"{assignment.ExpenseNotes}\n[Süre Uzatma - {DateTime.Now:dd.MM.yyyy HH:mm}]: {reason.Trim()}";
        }

        try
        {
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                adminUserId,
                new { PlannedEndTime = oldPlannedEndTime.ToString("yyyy-MM-dd HH:mm") },
                new
                {
                    PlannedEndTime = newPlannedEndTime.ToString("yyyy-MM-dd HH:mm"),
                    Reason = reason,
                    Note = "Seyir halindeki görevin planlanan dönüş tarihi Şef/Yönetici tarafından uzatıldı."
                }
            );

            _logger.LogInformation("Görev #{Id} süresi {OldEnd} tarihinden {NewEnd} tarihine uzatıldı. Kullanıcı: {User}",
                assignment.Id, oldPlannedEndTime, newPlannedEndTime, adminUserId);

            return (true, $"Görev süresi başarıyla {newPlannedEndTime:dd.MM.yyyy HH:mm} tarihine kadar uzatıldı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görev süresi uzatılırken hata oluştu. Id: {Id}", id);
            return (false, "Görev süresi uzatılırken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> CancelAssignmentAsync(int id, string cancellationReason, string adminUserId)
    {
        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null)
        {
            return (false, "İptal edilmek istenen görev bulunamadı.");
        }

        // Sadece Planned durumdaki görevler iptal edilebilir (ENDPOINT-SPESIFIKASYONU 3.50)
        if (assignment.Status != AssignmentStatus.Planned)
        {
            return (false, $"Yalnızca 'Planlandı' durumundaki görevler iptal edilebilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        if (string.IsNullOrWhiteSpace(cancellationReason))
        {
            return (false, "Görev iptal gerekçesi zorunludur.");
        }

        var oldState = new { Status = assignment.Status.ToString() };

        assignment.Status = AssignmentStatus.Cancelled;
        assignment.CancellationReason = cancellationReason.Trim();
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = adminUserId;

        try
        {
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                adminUserId,
                oldState,
                new { Status = assignment.Status.ToString(), assignment.CancellationReason }
            );

            _logger.LogInformation("Görev iptal edildi. Id: {Id}, Sebep: {Reason}", assignment.Id, cancellationReason);
            return (true, "Görev başarıyla iptal edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görev iptal edilirken hata oluştu. Id: {Id}", id);
            return (false, "Görev iptal edilirken bir veritabanı hatası oluştu.");
        }
    }

    // =========================================================================
    // 4. ŞEF / YÖNETİCİ ONAY VE DOĞRUDAN İŞLEM METOTLARI
    // =========================================================================

    public async Task<(bool Success, string Message)> ApproveStartAsync(int assignmentId, string approverUserId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        // Kural: Sadece StartReportedBySelf == true && StartApprovedByUserId == null olan kayıtlarda çalışır
        if (!assignment.StartReportedBySelf || assignment.StartApprovedByUserId != null)
        {
            return (false, "Bu görev için onay bekleyen bir başlangıç bildirimi bulunmamaktadır.");
        }

        if (assignment.Status != AssignmentStatus.Planned)
        {
            return (false, $"Yalnızca 'Planlandı' durumundaki görevlerin başlangıcı onaylanabilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        // Kural: Başlatılacak aracın üzerinde hâlihazırda InProgress olan başka bir görev varsa onaylanamaz!
        var hasActiveAssignmentOnVehicle = await _context.Assignments
            .AnyAsync(a => !a.IsDeleted &&
                           a.VehicleId == assignment.VehicleId &&
                           a.Id != assignment.Id &&
                           a.Status == AssignmentStatus.InProgress);

        if (hasActiveAssignmentOnVehicle)
        {
            return (false, "Bu araç şu anda başka bir görevde, önce mevcut görevi tamamlayın.");
        }

        // Kural: Şoförün üzerinde hâlihazırda InProgress olan başka bir görev varsa onaylanamaz!
        var hasActiveAssignmentOnDriver = await _context.Assignments
            .AnyAsync(a => !a.IsDeleted &&
                           a.DriverId == assignment.DriverId &&
                           a.Id != assignment.Id &&
                           a.Status == AssignmentStatus.InProgress);

        if (hasActiveAssignmentOnDriver)
        {
            return (false, "Bu şoför şu anda başka bir görevde seyirdedir, önce mevcut görevi tamamlayın.");
        }

        var vehicleStatusCheck = ValidateVehicleOperationalStatus(assignment.Vehicle);
        if (!vehicleStatusCheck.IsValid)
        {
            return (false, vehicleStatusCheck.ErrorMessage!);
        }

        var oldAssignmentState = new
        {
            assignment.Status,
            assignment.StartApprovedByUserId
        };
        var oldVehicleState = new
        {
            Status = assignment.Vehicle.Status.ToString()
        };

        // Onaylandığında: Status = InProgress, Vehicle = OnDuty, StartApprovedByUserId = approverUserId
        // Şoförün girdiği ActualStartTime ve StartKm DEĞERLERİ KORUNUR, üzerine yazılmaz!
        assignment.Status = AssignmentStatus.InProgress;
        assignment.StartApprovedByUserId = approverUserId;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = approverUserId;

        assignment.Vehicle.Status = VehicleStatus.OnDuty;
        assignment.Vehicle.UpdatedAt = DateTime.UtcNow;
        assignment.Vehicle.UpdatedBy = approverUserId;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                approverUserId,
                oldAssignmentState,
                new
                {
                    assignment.Status,
                    assignment.StartApprovedByUserId,
                    assignment.StartKm,
                    assignment.ActualStartTime,
                    Note = "Şef tarafından onaylandı"
                }
            );

            await _auditLogService.LogAsync(
                nameof(Vehicle),
                assignment.Vehicle.Id,
                AuditAction.Update,
                approverUserId,
                oldVehicleState,
                new { Status = assignment.Vehicle.Status.ToString() }
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Görev başlangıcı onaylandı. AssignmentId: {Id}, ApproverUserId: {UserId}", assignmentId, approverUserId);
            return (true, "Görev başlangıcı başarıyla onaylandı ve araç göreve çıktı.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Görev başlangıcı onaylanırken veritabanı hatası oluştu. AssignmentId: {Id}", assignmentId);
            return (false, "Görev başlangıcı onaylanırken bir sistem hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ApproveEndAsync(int assignmentId, string approverUserId)
    {
        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        var model = new AssignmentApproveEndViewModel
        {
            AssignmentId = assignmentId,
            ConfirmedStartTime = assignment.ActualStartTime ?? assignment.PlannedStartTime,
            ConfirmedEndTime = assignment.ActualEndTime ?? DateTime.Now,
            ConfirmedStartKm = assignment.StartKm,
            ConfirmedEndKm = assignment.EndKm,
            HourlyRate = assignment.HourlyRate,
            AccommodationNights = assignment.AccommodationNights,
            AccommodationCost = assignment.AccommodationCost,
            PerDiemDays = assignment.PerDiemDays,
            PerDiemAmount = assignment.PerDiemAmount,
            ExpenseNotes = assignment.ExpenseNotes
        };

        return await ApproveEndAsync(model, approverUserId);
    }

    public async Task<(bool Success, string Message)> ApproveEndAsync(AssignmentApproveEndViewModel model, string approverUserId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(a => a.Id == model.AssignmentId);

        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        // Kural: Sadece EndReportedBySelf == true && EndApprovedByUserId == null olan kayıtlarda çalışır
        if (!assignment.EndReportedBySelf || assignment.EndApprovedByUserId != null)
        {
            return (false, "Bu görev için onay bekleyen bir bitiş bildirimi bulunmamaktadır.");
        }

        if (assignment.Status == AssignmentStatus.Completed || assignment.Status == AssignmentStatus.Cancelled)
        {
            return (false, $"Bu görev zaten sonlandırılmıştır ({assignment.Status.ToTurkishString()}).");
        }

        var origStartKm = assignment.StartKm;
        var origEndKm = assignment.EndKm;

        // Şef KM değerlerini (başlangıç ve/veya bitiş) revize edebilir
        if (model.ConfirmedStartKm.HasValue)
        {
            assignment.StartKm = model.ConfirmedStartKm.Value;
        }
        if (model.ConfirmedEndKm.HasValue)
        {
            assignment.EndKm = model.ConfirmedEndKm.Value;
        }

        if (!assignment.EndKm.HasValue)
        {
            return (false, "Görev bitiş kilometresi girilmelidir.");
        }

        if (assignment.StartKm.HasValue && assignment.EndKm.Value < assignment.StartKm.Value)
        {
            return (false, $"Bitiş kilometresi ({assignment.EndKm.Value:N0} KM), başlangıç kilometresinden ({assignment.StartKm.Value:N0} KM) küçük olamaz.");
        }

        var isKmRevised = (origStartKm != assignment.StartKm) || (origEndKm != assignment.EndKm);

        // Şefin onayladığı başlangıç ve dönüş saatleri validasyonu
        if (model.ConfirmedEndTime <= model.ConfirmedStartTime)
        {
            return (false, "Dönüş saati, çıkış saatinden önce veya çıkış saatiyle aynı olamaz.");
        }

        // Net görev süresi ve ücret hesaplama
        var duration = model.ConfirmedEndTime - model.ConfirmedStartTime;
        var calculatedHours = Math.Round((decimal)duration.TotalHours, 2);
        if (calculatedHours < 0) calculatedHours = 0;

        var isHourly = string.Equals(model.PerDiemType, "Daily", StringComparison.OrdinalIgnoreCase) == false;
        decimal? effectiveRate = model.PerDiemRate ?? model.HourlyRate;
        decimal? totalFee = null;

        if (isHourly)
        {
            var hours = model.PerDiemHours ?? calculatedHours;
            if (effectiveRate.HasValue && effectiveRate.Value > 0)
            {
                totalFee = Math.Round(hours * effectiveRate.Value, 2);
            }
        }
        else // Daily
        {
            var days = model.PerDiemDays ?? 0;
            if (days > 0 && effectiveRate.HasValue && effectiveRate.Value > 0)
            {
                totalFee = Math.Round(days * effectiveRate.Value, 2);
            }
        }

        var oldAssignmentState = new
        {
            assignment.Status,
            assignment.StartApprovedByUserId,
            assignment.EndApprovedByUserId,
            StartKm = origStartKm,
            EndKm = origEndKm,
            assignment.ActualStartTime,
            assignment.ActualEndTime,
            assignment.HourlyRate,
            assignment.CalculatedDutyHours,
            assignment.TotalDriverFee,
            assignment.AccommodationCost,
            assignment.PerDiemAmount
        };
        var oldVehicleState = new
        {
            Status = assignment.Vehicle.Status.ToString(),
            CurrentKm = assignment.Vehicle.CurrentKm
        };

        // Eğer gece görevi gibi bir durumda Start henüz onaylanmadıysa, End onaylanırken Start onayı da tamamlanır
        if (assignment.StartReportedBySelf && assignment.StartApprovedByUserId == null)
        {
            assignment.StartApprovedByUserId = approverUserId;
        }

        // Onaylandığında: Status = Completed, Saatler güncellenir, Ücretler kaydedilir, Vehicle = Idle
        assignment.Status = AssignmentStatus.Completed;
        assignment.EndApprovedByUserId = approverUserId;
        assignment.ActualStartTime = model.ConfirmedStartTime;
        assignment.ActualEndTime = model.ConfirmedEndTime;
        // Eğer şef fiili çalışma süresini el ile revize ettiyse bu süre geçerli olur
        assignment.CalculatedDutyHours = (isHourly && model.PerDiemHours.HasValue && model.PerDiemHours.Value > 0) ? model.PerDiemHours.Value : calculatedHours;

        // Ücret ve Konaklama Alanları (Saatlik / Günlük Basit Çarpım)
        assignment.AccommodationNights = model.AccommodationNights;
        assignment.AccommodationCost = model.AccommodationCost;
        assignment.PerDiemType = isHourly ? "Hourly" : "Daily";
        assignment.PerDiemRate = (effectiveRate.HasValue && effectiveRate.Value > 0) ? effectiveRate.Value : null;
        assignment.PerDiemHours = isHourly ? (model.PerDiemHours ?? calculatedHours) : null;
        assignment.PerDiemDays = !isHourly ? model.PerDiemDays : null;
        assignment.PerDiemAmount = totalFee ?? model.PerDiemAmount;

        // Geriye dönük sorgu ve rapor uyumluluğu
        if (isHourly)
        {
            assignment.HourlyRate = assignment.PerDiemRate;
            assignment.TotalDriverFee = assignment.PerDiemAmount;
        }
        else
        {
            assignment.HourlyRate = null;
            assignment.TotalDriverFee = null;
        }

        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = approverUserId;

        if (!string.IsNullOrWhiteSpace(model.ExpenseNotes))
        {
            assignment.ExpenseNotes = string.IsNullOrWhiteSpace(assignment.ExpenseNotes)
                ? model.ExpenseNotes
                : $"{assignment.ExpenseNotes} | Onay Notu: {model.ExpenseNotes}";
        }

        var today = DateTime.Today;
        if (assignment.Vehicle.InspectionExpiryDate.Date < today || assignment.Vehicle.InsuranceExpiryDate.Date < today)
        {
            assignment.Vehicle.Status = VehicleStatus.DocumentExpired;
        }
        else
        {
            // İş Kuralı: Araç üzerinde halen devam eden başka bir aktif (InProgress) görev varsa araç OnDuty kalmalı; aksi halde Idle olmalı.
            var hasOtherInProgress = await _context.Assignments
                .AnyAsync(a => !a.IsDeleted &&
                               a.VehicleId == assignment.VehicleId &&
                               a.Id != assignment.Id &&
                               a.Status == AssignmentStatus.InProgress);

            assignment.Vehicle.Status = hasOtherInProgress ? VehicleStatus.OnDuty : VehicleStatus.Idle;
        }

        // İŞ KURALI: Şef onayında KM düzeltilince Vehicle.CurrentKm'yi SADECE şu iki koşul birlikte doğruysa güncelle:
        // (a) bu görev aracın en son tamamlanan görevi
        // (b) yeni değer mevcut CurrentKm'den büyük
        // Aksi halde araç KM'sine dokunma.
        var newEndKm = assignment.EndKm.Value;
        var hasLaterCompletedAssignment = await _context.Assignments
            .AnyAsync(a => !a.IsDeleted &&
                           a.VehicleId == assignment.VehicleId &&
                           a.Id != assignment.Id &&
                           a.Status == AssignmentStatus.Completed &&
                           (a.ActualEndTime > assignment.ActualEndTime || (a.ActualEndTime == assignment.ActualEndTime && a.Id > assignment.Id)));

        bool isLatestCompleted = !hasLaterCompletedAssignment;
        bool shouldUpdateVehicleKm = isLatestCompleted && (newEndKm > assignment.Vehicle.CurrentKm);

        if (shouldUpdateVehicleKm)
        {
            assignment.Vehicle.CurrentKm = newEndKm;
        }

        assignment.Vehicle.UpdatedAt = DateTime.UtcNow;
        assignment.Vehicle.UpdatedBy = approverUserId;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                approverUserId,
                oldAssignmentState,
                new
                {
                    assignment.Status,
                    assignment.StartApprovedByUserId,
                    assignment.EndApprovedByUserId,
                    assignment.StartKm,
                    assignment.EndKm,
                    assignment.ActualStartTime,
                    assignment.ActualEndTime,
                    assignment.HourlyRate,
                    assignment.CalculatedDutyHours,
                    assignment.TotalDriverFee,
                    IsKmRevised = isKmRevised,
                    Note = isKmRevised 
                        ? (shouldUpdateVehicleKm 
                            ? "Şef tarafından KM revizyonu yapılarak onaylandı (Araç KM güncellendi)" 
                            : "Şef tarafından KM revizyonu yapılarak onaylandı (Araç KM korundu - en son görev veya büyük değer kuralı)")
                        : "Şef tarafından saatler ve ücret onaylandı"
                }
            );

            await _auditLogService.LogAsync(
                nameof(Vehicle),
                assignment.Vehicle.Id,
                AuditAction.Update,
                approverUserId,
                oldVehicleState,
                new
                {
                    Status = assignment.Vehicle.Status.ToString(),
                    CurrentKm = assignment.Vehicle.CurrentKm
                }
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Görev bitişi onaylandı. AssignmentId: {Id}, Approver: {UserId}, Hours: {Hours}, DriverFee: {Fee} TL, PerDiem: {PerDiem} TL, AccCost: {Acc} TL", 
                model.AssignmentId, approverUserId, calculatedHours, totalFee ?? 0, assignment.PerDiemAmount ?? 0, assignment.AccommodationCost ?? 0);

            var durationFormatted = aractakipsys.Common.Helpers.DurationHelper.FormatDurationHours(calculatedHours);
            var successMessage = (totalFee.HasValue && totalFee.Value > 0)
                ? $"Görev bitişi başarıyla onaylandı. Çalışma süresi: {durationFormatted}, Şoför ek ücreti: {totalFee.Value:N2} ₺ olarak kaydedildi."
                : $"Görev bitişi başarıyla onaylandı. Onaylanan sefer süresi: {durationFormatted}.";

            return (true, successMessage);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Görev bitişi onaylanırken veritabanı hatası oluştu. AssignmentId: {Id}", model.AssignmentId);
            return (false, "Görev bitişi onaylanırken bir sistem hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> StartAssignmentDirectAsync(int assignmentId, string userId, int? startKm = null, bool confirmKmDifference = false)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        if (assignment.Status != AssignmentStatus.Planned)
        {
            return (false, $"Yalnızca 'Planlandı' durumundaki görevler doğrudan başlatılabilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        // Kural: Başlatılacak aracın üzerinde hâlihazırda InProgress olan başka bir görev varsa doğrudan başlatılamaz!
        var hasActiveAssignmentOnVehicleDirect = await _context.Assignments
            .AnyAsync(a => !a.IsDeleted &&
                           a.VehicleId == assignment.VehicleId &&
                           a.Id != assignment.Id &&
                           a.Status == AssignmentStatus.InProgress);

        if (hasActiveAssignmentOnVehicleDirect)
        {
            return (false, "Bu araç şu anda başka bir görevde, önce mevcut görevi tamamlayın.");
        }

        // Kural: Başlatılacak şoförün üzerinde hâlihazırda InProgress olan başka bir görev varsa doğrudan başlatılamaz!
        // Yalnızca kurum şoförleri (DriverId.HasValue) için kontrol edilir; dış şoförlerde DriverId NULL olduğundan kontrol atlanır.
        if (assignment.DriverId.HasValue)
        {
            var hasActiveAssignmentOnDriverDirect = await _context.Assignments
                .AnyAsync(a => !a.IsDeleted &&
                               a.DriverId != null &&
                               a.DriverId == assignment.DriverId.Value &&
                               a.Id != assignment.Id &&
                               a.Status == AssignmentStatus.InProgress);

            if (hasActiveAssignmentOnDriverDirect)
            {
                return (false, "Bu şoför şu anda başka bir görevde seyirdedir, önce mevcut görevi tamamlayın.");
            }
        }

        var vehicleStatusCheck = ValidateVehicleOperationalStatus(assignment.Vehicle);
        if (!vehicleStatusCheck.IsValid)
        {
            return (false, vehicleStatusCheck.ErrorMessage!);
        }

        var effectiveKm = startKm ?? assignment.Vehicle.CurrentKm;
        var kmCheck = _kmValidationService.ValidateStartKm(effectiveKm, assignment.Vehicle.CurrentKm, confirmKmDifference);
        if (!kmCheck.IsValid)
        {
            return (false, kmCheck.Message!);
        }

        var oldAssignmentState = new { assignment.Status, assignment.ActualStartTime, assignment.StartKm };
        var oldVehicleState = new { Status = assignment.Vehicle.Status.ToString() };

        assignment.ActualStartTime = DateTime.Now;
        assignment.StartKm = effectiveKm;
        assignment.StartReportedBySelf = false;
        assignment.StartApprovedByUserId = userId;
        assignment.Status = AssignmentStatus.InProgress;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = userId;

        assignment.Vehicle.Status = VehicleStatus.OnDuty;
        assignment.Vehicle.UpdatedAt = DateTime.UtcNow;
        assignment.Vehicle.UpdatedBy = userId;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                userId,
                oldAssignmentState,
                new
                {
                    assignment.Status,
                    assignment.ActualStartTime,
                    assignment.StartKm,
                    StartReportedBySelf = false,
                    StartApprovedByUserId = userId,
                    Note = "Birim yetkilisi/Admin tarafından doğrudan başlatıldı"
                }
            );

            await _auditLogService.LogAsync(
                nameof(Vehicle),
                assignment.Vehicle.Id,
                AuditAction.Update,
                userId,
                oldVehicleState,
                new { Status = assignment.Vehicle.Status.ToString() }
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Görev doğrudan başlatıldı. AssignmentId: {Id}, UserId: {UserId}, StartKm: {Km}", 
                assignment.Id, userId, effectiveKm);

            return (true, "Görev başarıyla başlatıldı.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Görev doğrudan başlatılırken hata oluştu. AssignmentId: {Id}", assignmentId);
            return (false, "Görev başlatılırken bir sistem hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> EndAssignmentDirectAsync(int assignmentId, string userId, int endKm, string? driverReturnNotes = null, bool confirmKmDifference = false)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        if (assignment.Status != AssignmentStatus.InProgress)
        {
            return (false, $"Yalnızca devam eden ('Görevde') görevler tamamlanabilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        var minAllowedKm = assignment.StartKm ?? assignment.Vehicle.CurrentKm;
        var kmCheck = _kmValidationService.ValidateEndKm(endKm, minAllowedKm, confirmKmDifference);
        if (!kmCheck.IsValid)
        {
            return (false, kmCheck.Message!);
        }

        var oldAssignmentState = new { assignment.Status, assignment.ActualEndTime, assignment.EndKm, assignment.DriverReturnNotes };
        var oldVehicleState = new { Status = assignment.Vehicle.Status.ToString(), CurrentKm = assignment.Vehicle.CurrentKm };

        assignment.ActualEndTime = DateTime.Now;
        assignment.EndKm = endKm;
        assignment.DriverReturnNotes = string.IsNullOrWhiteSpace(driverReturnNotes) ? null : driverReturnNotes.Trim();
        assignment.EndReportedBySelf = false;
        assignment.EndApprovedByUserId = userId;
        assignment.Status = AssignmentStatus.Completed;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = userId;

        assignment.Vehicle.CurrentKm = endKm;
        var today = DateTime.Today;
        if (assignment.Vehicle.InspectionExpiryDate.Date < today || assignment.Vehicle.InsuranceExpiryDate.Date < today)
        {
            assignment.Vehicle.Status = VehicleStatus.DocumentExpired;
        }
        else
        {
            // İş Kuralı: Araç üzerinde halen devam eden başka bir aktif (InProgress) görev varsa araç OnDuty kalmalı; aksi halde Idle olmalı.
            var hasOtherInProgress = await _context.Assignments
                .AnyAsync(a => !a.IsDeleted &&
                               a.VehicleId == assignment.VehicleId &&
                               a.Id != assignment.Id &&
                               a.Status == AssignmentStatus.InProgress);

            assignment.Vehicle.Status = hasOtherInProgress ? VehicleStatus.OnDuty : VehicleStatus.Idle;
        }
        assignment.Vehicle.UpdatedAt = DateTime.UtcNow;
        assignment.Vehicle.UpdatedBy = userId;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                userId,
                oldAssignmentState,
                new
                {
                    assignment.Status,
                    assignment.ActualEndTime,
                    assignment.EndKm,
                    assignment.DriverReturnNotes,
                    EndReportedBySelf = false,
                    EndApprovedByUserId = userId,
                    Note = "Birim yetkilisi/Admin tarafından doğrudan tamamlandı"
                }
            );

            await _auditLogService.LogAsync(
                nameof(Vehicle),
                assignment.Vehicle.Id,
                AuditAction.Update,
                userId,
                oldVehicleState,
                new
                {
                    Status = assignment.Vehicle.Status.ToString(),
                    CurrentKm = assignment.Vehicle.CurrentKm
                }
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Görev doğrudan tamamlandı. AssignmentId: {Id}, UserId: {UserId}, EndKm: {Km}", 
                assignment.Id, userId, endKm);

            return (true, "Görev başarıyla tamamlandı ve araç teslim alındı.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Görev doğrudan tamamlanırken hata oluştu. AssignmentId: {Id}", assignmentId);
            return (false, "Görev tamamlanırken bir sistem hatası oluştu.");
        }
    }

    /// <summary>
    /// Tamamlanmış bir görev için sonradan fatura veya harcırah belgelerine istinaden maliyet bilgilerini revize etme
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateFinancialDetailsAsync(AssignmentFinancialRevisionViewModel model, string userId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.Id == model.AssignmentId);

        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        if (assignment.Status != AssignmentStatus.Completed)
        {
            return (false, "Sadece tamamlanmış görevlerin harcırah ve maliyet bilgileri güncellenebilir.");
        }

        if (string.IsNullOrWhiteSpace(model.RevisionReason))
        {
            return (false, "Revizyon gerekçesi belirtilmelidir.");
        }

        var oldState = new
        {
            assignment.HourlyRate,
            assignment.TotalDriverFee,
            assignment.AccommodationNights,
            assignment.AccommodationCost,
            assignment.PerDiemType,
            assignment.PerDiemRate,
            assignment.PerDiemHours,
            assignment.PerDiemDays,
            assignment.PerDiemAmount
        };

        var isHourly = string.Equals(model.PerDiemType, "Daily", StringComparison.OrdinalIgnoreCase) == false;
        decimal? effectiveRate = model.PerDiemRate ?? model.HourlyRate;
        decimal? totalFee = null;

        if (isHourly)
        {
            var dutyHours = model.PerDiemHours ?? assignment.CalculatedDutyHours ?? 0;
            if (effectiveRate.HasValue && effectiveRate.Value > 0)
            {
                totalFee = Math.Round(dutyHours * effectiveRate.Value, 2);
            }
        }
        else // Daily
        {
            var days = model.PerDiemDays ?? 0;
            if (days > 0 && effectiveRate.HasValue && effectiveRate.Value > 0)
            {
                totalFee = Math.Round(days * effectiveRate.Value, 2);
            }
        }

        assignment.AccommodationNights = model.AccommodationNights;
        assignment.AccommodationCost = model.AccommodationCost;
        assignment.PerDiemType = isHourly ? "Hourly" : "Daily";
        assignment.PerDiemRate = (effectiveRate.HasValue && effectiveRate.Value > 0) ? effectiveRate.Value : null;
        assignment.PerDiemHours = isHourly ? (model.PerDiemHours ?? assignment.CalculatedDutyHours) : null;
        assignment.PerDiemDays = !isHourly ? model.PerDiemDays : null;
        assignment.PerDiemAmount = totalFee ?? model.PerDiemAmount;

        if (isHourly)
        {
            assignment.HourlyRate = assignment.PerDiemRate;
            assignment.TotalDriverFee = assignment.PerDiemAmount;
        }
        else
        {
            assignment.HourlyRate = null;
            assignment.TotalDriverFee = null;
        }

        var nowStr = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        var revisionEntry = $"[Revizyon {nowStr}]: {model.RevisionReason.Trim()}";
        assignment.ExpenseNotes = string.IsNullOrWhiteSpace(assignment.ExpenseNotes)
            ? revisionEntry
            : $"{assignment.ExpenseNotes} | {revisionEntry}";

        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = userId;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Update,
                userId,
                oldState,
                new
                {
                    assignment.HourlyRate,
                    assignment.TotalDriverFee,
                    assignment.AccommodationNights,
                    assignment.AccommodationCost,
                    assignment.PerDiemDays,
                    assignment.PerDiemAmount,
                    Reason = model.RevisionReason.Trim()
                }
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Görev harcırah/maliyet revizyonu yapıldı. AssignmentId: {Id}, UserId: {UserId}", assignment.Id, userId);
            return (true, "Harcırah ve görev maliyet bilgileri başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Görev harcırah revizyonu sırasında hata oluştu. AssignmentId: {Id}", model.AssignmentId);
            return (false, "Maliyet bilgileri güncellenirken bir sistem hatası oluştu.");
        }
    }

    // =========================================================================
    // 5. YARDIMCI DOĞRULAMA (VALIDATION) METOTLARI
    // =========================================================================

    private static (bool IsValid, string? ErrorMessage) ValidateVehicleOperationalStatus(Vehicle vehicle)
    {
        if (vehicle.Status == VehicleStatus.OutOfService)
        {
            return (false, "Araç 'Hizmet Dışı' durumdadır, görev atanamaz veya başlatılamaz.");
        }

        if (vehicle.Status == VehicleStatus.Maintenance)
        {
            return (false, "Araç şu anda bakım/serviste bulunmaktadır, görev atanamaz veya başlatılamaz.");
        }

        if (vehicle.Status == VehicleStatus.DocumentExpired)
        {
            return (false, "Aracın muayene veya sigorta belgesinin geçerlilik süresi dolmuştur (Belgesi Geçersiz), görev atanamaz veya başlatılamaz.");
        }

        // Asıl güvenlik katmanı: Status alanı henüz güncellenmemiş olsa dahi doğrudan tarih kontrolü
        var today = DateTime.Today;
        if (vehicle.InspectionExpiryDate.Date < today)
        {
            return (false, $"Aracın fenni muayene süresi ({vehicle.InspectionExpiryDate:dd.MM.yyyy}) dolmuştur, görev atanamaz veya başlatılamaz.");
        }

        if (vehicle.InsuranceExpiryDate.Date < today)
        {
            return (false, $"Aracın trafik sigortası süresi ({vehicle.InsuranceExpiryDate:dd.MM.yyyy}) dolmuştur, görev atanamaz veya başlatılamaz.");
        }

        return (true, null);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateStartKm(int startKm, int vehicleCurrentKm)
    {
        if (startKm < vehicleCurrentKm)
        {
            return (false, $"Başlangıç kilometresi ({startKm:N0} KM), aracın güncel kilometresinden ({vehicleCurrentKm:N0} KM) küçük olamaz.");
        }

        return (true, null);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateEndKm(int endKm, int minAllowedKm)
    {
        if (endKm < minAllowedKm)
        {
            return (false, $"Bitiş kilometresi ({endKm:N0} KM), başlangıç kilometresinden ({minAllowedKm:N0} KM) küçük olamaz.");
        }

        return (true, null);
    }

    #region İmzalı Görevlendirme Belgesi İşlemleri (Aşama H)

    private static bool IsValidDocumentFile(Microsoft.AspNetCore.Http.IFormFile file, out string? errorMessage)
    {
        errorMessage = null;

        if (file.Length == 0)
        {
            errorMessage = "Yüklenen dosya boş olamaz.";
            return false;
        }

        // 10 MB = 10 * 1024 * 1024 bytes
        if (file.Length > 10 * 1024 * 1024)
        {
            errorMessage = "Dosya boyutu en fazla 10 MB olabilir.";
            return false;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        if (!allowedExtensions.Contains(ext))
        {
            errorMessage = "Yalnızca PDF, JPG, JPEG veya PNG formatında dosya yükleyebilirsiniz.";
            return false;
        }

        // Magic number (dosya başlığı baytları) doğrulaması
        using var stream = file.OpenReadStream();
        var headerBytes = new byte[8];
        var bytesRead = stream.Read(headerBytes, 0, 8);

        if (bytesRead < 4)
        {
            errorMessage = "Geçersiz veya bozuk dosya içeriği.";
            return false;
        }

        // PDF: %PDF (0x25, 0x50, 0x44, 0x46)
        if (ext == ".pdf")
        {
            if (headerBytes[0] != 0x25 || headerBytes[1] != 0x50 || headerBytes[2] != 0x44 || headerBytes[3] != 0x46)
            {
                errorMessage = "Dosya içeriği geçerli bir PDF formatında değil.";
                return false;
            }
        }
        // JPEG: FF D8 FF
        else if (ext is ".jpg" or ".jpeg")
        {
            if (headerBytes[0] != 0xFF || headerBytes[1] != 0xD8 || headerBytes[2] != 0xFF)
            {
                errorMessage = "Dosya içeriği geçerli bir JPEG/JPG formatında değil.";
                return false;
            }
        }
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        else if (ext == ".png")
        {
            if (bytesRead < 8 ||
                headerBytes[0] != 0x89 || headerBytes[1] != 0x50 || headerBytes[2] != 0x4E || headerBytes[3] != 0x47 ||
                headerBytes[4] != 0x0D || headerBytes[5] != 0x0A || headerBytes[6] != 0x1A || headerBytes[7] != 0x0A)
            {
                errorMessage = "Dosya içeriği geçerli bir PNG formatında değil.";
                return false;
            }
        }

        return true;
    }

    public async Task<(bool Success, string? ErrorMessage)> UploadSignedDocumentAsync(
        int assignmentId,
        Microsoft.AspNetCore.Http.IFormFile file,
        string userId,
        int? driverId = null,
        string? description = null)
    {
        var assignment = await _context.Assignments
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        if (!IsValidDocumentFile(file, out var validationError))
        {
            return (false, validationError);
        }

        // Opsiyonel şoför ilişkisi doğrulaması: yalnızca bu göreve atanmış şoförlerden biri seçilebilir
        int? validDriverId = null;
        if (driverId.HasValue)
        {
            var isAssignedDriver = assignment.DriverId == driverId.Value ||
                                   assignment.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driverId.Value);
            if (!isAssignedDriver)
            {
                return (false, "Seçilen şoför bu göreve atanmamış.");
            }
            validDriverId = driverId.Value;
        }

        var uploadDir = Path.Combine(_environment.ContentRootPath, "App_Data", "AssignmentDocuments");
        if (!Directory.Exists(uploadDir))
        {
            Directory.CreateDirectory(uploadDir);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{assignmentId}_{Guid.NewGuid():N}{ext}";
        var relativePath = Path.Combine("App_Data", "AssignmentDocuments", uniqueFileName);
        var fullPath = Path.Combine(uploadDir, uniqueFileName);

        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var nowUtc = DateTime.UtcNow;
        var cleanOriginalName = Path.GetFileName(file.FileName);
        var cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (cleanDescription != null && cleanDescription.Length > 250)
        {
            cleanDescription = cleanDescription[..250];
        }

        var doc = new AssignmentDocument
        {
            AssignmentId = assignment.Id,
            DriverId = validDriverId,
            FilePath = relativePath,
            OriginalFileName = cleanOriginalName,
            FileSizeBytes = file.Length,
            Description = cleanDescription,
            UploadedAt = nowUtc,
            UploadedByUserId = userId,
            CreatedAt = nowUtc,
            CreatedBy = userId
        };

        _context.AssignmentDocuments.Add(doc);

        // Eski tekil özet alanlarını en son yüklenen belgeyle senkron tut (geriye dönük uyumluluk)
        assignment.SignedDocumentPath = relativePath;
        assignment.SignedDocumentOriginalFileName = cleanOriginalName;
        assignment.SignedDocumentUploadedAt = nowUtc;
        assignment.SignedDocumentUploadedByUserId = userId;
        assignment.UpdatedAt = nowUtc;

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogAsync(
            entityName: nameof(AssignmentDocument),
            entityId: doc.Id,
            action: AuditAction.Create,
            changedByUserId: userId,
            oldValues: null,
            newValues: new
            {
                doc.AssignmentId,
                doc.DriverId,
                doc.FilePath,
                doc.OriginalFileName,
                doc.FileSizeBytes,
                doc.Description,
                doc.UploadedAt,
                doc.UploadedByUserId
            }
        );

        _logger.LogInformation("Görev #{AssignmentId} için imzalı belge eklendi (DocId: {DocId}): {FileName}", assignmentId, doc.Id, uniqueFileName);
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteSignedDocumentAsync(int assignmentId, int documentId, string userId)
    {
        var doc = await _context.AssignmentDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.AssignmentId == assignmentId);

        if (doc == null)
        {
            return (false, "Silinecek imzalı belge kaydı bulunamadı.");
        }

        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment == null)
        {
            return (false, "Görev kaydı bulunamadı.");
        }

        var oldValues = new
        {
            doc.Id,
            doc.AssignmentId,
            doc.DriverId,
            doc.FilePath,
            doc.OriginalFileName,
            doc.Description,
            doc.UploadedAt
        };

        var nowUtc = DateTime.UtcNow;
        doc.IsDeleted = true;
        doc.UpdatedAt = nowUtc;
        doc.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        // Kalan en güncel aktif belge varsa Assignment üzerindeki özet alanlarını güncelle; yoksa temizle
        var latestRemaining = await _context.AssignmentDocuments
            .Where(d => d.AssignmentId == assignmentId)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefaultAsync();

        if (latestRemaining != null)
        {
            assignment.SignedDocumentPath = latestRemaining.FilePath;
            assignment.SignedDocumentOriginalFileName = latestRemaining.OriginalFileName;
            assignment.SignedDocumentUploadedAt = latestRemaining.UploadedAt;
            assignment.SignedDocumentUploadedByUserId = latestRemaining.UploadedByUserId;
        }
        else
        {
            assignment.SignedDocumentPath = null;
            assignment.SignedDocumentOriginalFileName = null;
            assignment.SignedDocumentUploadedAt = null;
            assignment.SignedDocumentUploadedByUserId = null;
        }
        assignment.UpdatedAt = nowUtc;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            entityName: nameof(AssignmentDocument),
            entityId: doc.Id,
            action: AuditAction.Delete,
            changedByUserId: userId,
            oldValues: oldValues,
            newValues: new { IsDeleted = true }
        );

        _logger.LogInformation("Görev #{AssignmentId} için imzalı belge silindi (DocId: {DocId}, UserId: {UserId})", assignmentId, documentId, userId);
        return (true, null);
    }

    public async Task<(byte[]? FileContents, string? ContentType, string? OriginalFileName)?> GetSignedDocumentFileAsync(int assignmentId, int? documentId = null)
    {
        string? filePath = null;
        string? originalFileName = null;

        if (documentId.HasValue && documentId.Value > 0)
        {
            var doc = await _context.AssignmentDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId.Value && d.AssignmentId == assignmentId);

            if (doc == null)
            {
                return null;
            }

            filePath = doc.FilePath;
            originalFileName = doc.OriginalFileName;
        }
        else
        {
            var latestDoc = await _context.AssignmentDocuments.AsNoTracking()
                .Where(d => d.AssignmentId == assignmentId)
                .OrderByDescending(d => d.UploadedAt)
                .FirstOrDefaultAsync();

            if (latestDoc != null)
            {
                filePath = latestDoc.FilePath;
                originalFileName = latestDoc.OriginalFileName;
            }
            else
            {
                var assignment = await _context.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
                if (assignment == null || string.IsNullOrWhiteSpace(assignment.SignedDocumentPath))
                {
                    return null;
                }

                filePath = assignment.SignedDocumentPath;
                originalFileName = assignment.SignedDocumentOriginalFileName;
            }
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var allowedRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "AssignmentDocuments"));
        var fullPath = Path.GetFullPath(Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(_environment.ContentRootPath, filePath));

        // Dizin aşımı (Path Traversal) güvenlik kontrolü
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Yetkisiz dosya yolu erişimi engellendi: {FullPath}", fullPath);
            return null;
        }

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("İmzalı belge diskte bulunamadı: {FullPath}", fullPath);
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(fullPath);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        var downloadName = originalFileName
            ?? $"Imzali_Belge_Gorev_{assignmentId}{ext}";

        return (bytes, contentType, downloadName);
    }

    public async Task<SignedDocumentArchiveViewModel> GetSignedDocumentArchiveAsync(SignedDocumentArchiveFilterViewModel filter)
    {
        var model = new SignedDocumentArchiveViewModel
        {
            Filter = filter
        };

        // Araç ve şoför dropdown listeleri
        var vehicles = await _context.Vehicles.AsNoTracking()
            .OrderBy(v => v.PlateNumber)
            .Select(v => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.PlateNumber} - {v.Brand} {v.Model}",
                Selected = filter.VehicleId.HasValue && filter.VehicleId.Value == v.Id
            })
            .ToListAsync();
        model.VehicleList = vehicles;

        var drivers = await _context.Drivers.AsNoTracking()
            .Include(d => d.User)
            .Where(d => d.IsActive)
            .OrderBy(d => d.User.FullName)
            .Select(d => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.User.FullName,
                Selected = filter.DriverId.HasValue && filter.DriverId.Value == d.Id
            })
            .ToListAsync();
        model.DriverList = drivers;

        // KPI İstatistikleri
        var totalAssignments = await _context.Assignments.CountAsync();
        var assignmentsWithDocs = await _context.Assignments.CountAsync(a => a.SignedDocuments.Any() || !string.IsNullOrEmpty(a.SignedDocumentPath));
        var totalUploadedDocs = await _context.AssignmentDocuments.CountAsync(d => !d.Assignment.IsDeleted);
        var totalMissing = await _context.Assignments.CountAsync(a => !a.SignedDocuments.Any() && string.IsNullOrEmpty(a.SignedDocumentPath) && a.Status == AssignmentStatus.Completed);

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var uploadedThisMonth = await _context.AssignmentDocuments.CountAsync(d => !d.Assignment.IsDeleted && d.UploadedAt >= startOfMonth);

        model.TotalAssignmentsCount = totalAssignments;
        model.TotalUploadedCount = totalUploadedDocs;
        model.AssignmentsWithDocumentCount = assignmentsWithDocs;
        model.TotalMissingCount = totalMissing;
        model.UploadedThisMonthCount = uploadedThisMonth;

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        static string GetAssignmentDisplayDrivers(Assignment a)
        {
            var driverNamesList = a.AssignmentDrivers
                .Where(ad => !ad.IsDeleted)
                .OrderByDescending(ad => ad.IsPrimary)
                .Select(ad => ad.Driver.User.FullName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            return driverNamesList.Count > 0
                ? string.Join(", ", driverNamesList)
                : (a.Driver?.User?.FullName ?? (a.IsExternalDriver ? a.ExternalDriverFullName ?? "Dış Şoför" : (a.IsDriverless ? "Şoförsüz" : "-")));
        }

        // 1. YÜKLENENLER (ARŞİV): Her yüklenen belge ayrı bir satır olarak alt alta listelenir
        if (filter.DocumentFilter == SignedDocumentFilterType.Uploaded)
        {
            var docQuery = _context.AssignmentDocuments.AsNoTracking()
                .Include(d => d.Driver).ThenInclude(dr => dr!.User)
                .Include(d => d.UploadedByUser)
                .Include(d => d.Assignment).ThenInclude(a => a.Vehicle)
                .Include(d => d.Assignment).ThenInclude(a => a.Driver).ThenInclude(dr => dr!.User)
                .Include(d => d.Assignment).ThenInclude(a => a.AssignmentDrivers).ThenInclude(ad => ad.Driver).ThenInclude(dr => dr.User)
                .Where(d => !d.Assignment.IsDeleted)
                .AsQueryable();

            if (filter.VehicleId.HasValue)
            {
                docQuery = docQuery.Where(d => d.Assignment.VehicleId == filter.VehicleId.Value);
            }

            if (filter.DriverId.HasValue)
            {
                docQuery = docQuery.Where(d =>
                    d.DriverId == filter.DriverId.Value ||
                    (d.DriverId == null && (d.Assignment.DriverId == filter.DriverId.Value ||
                                            d.Assignment.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == filter.DriverId.Value))));
            }

            if (filter.StartDate.HasValue)
            {
                var startUtc = filter.StartDate.Value.Date;
                docQuery = docQuery.Where(d => d.Assignment.PlannedStartTime >= startUtc);
            }

            if (filter.EndDate.HasValue)
            {
                var endUtc = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                docQuery = docQuery.Where(d => d.Assignment.PlannedStartTime <= endUtc);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                docQuery = docQuery.Where(d =>
                    d.Assignment.Vehicle.PlateNumber.ToLower().Contains(term) ||
                    d.Assignment.Vehicle.Brand.ToLower().Contains(term) ||
                    d.Assignment.Vehicle.Model.ToLower().Contains(term) ||
                    (d.Driver != null && d.Driver.User.FullName.ToLower().Contains(term)) ||
                    (d.Assignment.Driver != null && d.Assignment.Driver.User.FullName.ToLower().Contains(term)) ||
                    d.Assignment.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.Driver.User.FullName.ToLower().Contains(term)) ||
                    (d.Assignment.RequestedByDepartment != null && d.Assignment.RequestedByDepartment.ToLower().Contains(term)) ||
                    d.Assignment.Purpose.ToLower().Contains(term) ||
                    d.OriginalFileName.ToLower().Contains(term) ||
                    (d.Description != null && d.Description.ToLower().Contains(term))
                );
            }

            filter.TotalCount = await docQuery.CountAsync();

            var pagedDocs = await docQuery
                .OrderByDescending(d => d.UploadedAt)
                .ThenByDescending(d => d.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            model.Items = pagedDocs.Select(d => new SignedDocumentItemViewModel
            {
                AssignmentId = d.AssignmentId,
                VehiclePlate = d.Assignment.Vehicle.PlateNumber,
                VehicleBrandModel = $"{d.Assignment.Vehicle.Brand} {d.Assignment.Vehicle.Model}",
                DriverFullName = !string.IsNullOrWhiteSpace(d.Driver?.User?.FullName)
                    ? d.Driver.User.FullName
                    : GetAssignmentDisplayDrivers(d.Assignment),
                RequestedByDepartment = d.Assignment.RequestedByDepartment ?? string.Empty,
                Purpose = d.Assignment.Purpose,
                PlannedStartTime = d.Assignment.PlannedStartTime,
                PlannedEndTime = d.Assignment.PlannedEndTime,
                Status = d.Assignment.Status,
                DocumentId = d.Id,
                DocumentCount = 1,
                DocumentOriginalFileName = d.OriginalFileName,
                DocumentPath = d.FilePath,
                UploadedAt = d.UploadedAt,
                UploadedByFullName = d.UploadedByUser?.FullName ?? d.UploadedByUser?.UserName
            }).ToList();

            return model;
        }

        // 2. BELGESİ EKSİK veya TÜMÜ
        var query = _context.Assignments.AsNoTracking()
            .Include(a => a.Vehicle)
            .Include(a => a.Driver).ThenInclude(d => d!.User)
            .Include(a => a.AssignmentDrivers).ThenInclude(ad => ad.Driver).ThenInclude(d => d.User)
            .Include(a => a.SignedDocuments).ThenInclude(sd => sd.Driver).ThenInclude(d => d!.User)
            .Include(a => a.SignedDocuments).ThenInclude(sd => sd.UploadedByUser)
            .AsQueryable();

        if (filter.DocumentFilter == SignedDocumentFilterType.Missing)
        {
            query = query.Where(a => !a.SignedDocuments.Any() && string.IsNullOrEmpty(a.SignedDocumentPath));
        }

        if (filter.VehicleId.HasValue)
        {
            query = query.Where(a => a.VehicleId == filter.VehicleId.Value);
        }

        if (filter.DriverId.HasValue)
        {
            query = query.Where(a => a.DriverId == filter.DriverId.Value ||
                                     a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == filter.DriverId.Value));
        }

        if (filter.StartDate.HasValue)
        {
            var startUtc = filter.StartDate.Value.Date;
            query = query.Where(a => a.PlannedStartTime >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.PlannedStartTime <= endUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Vehicle.PlateNumber.ToLower().Contains(term) ||
                a.Vehicle.Brand.ToLower().Contains(term) ||
                a.Vehicle.Model.ToLower().Contains(term) ||
                (a.Driver != null && a.Driver.User.FullName.ToLower().Contains(term)) ||
                a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.Driver.User.FullName.ToLower().Contains(term)) ||
                (a.RequestedByDepartment != null && a.RequestedByDepartment.ToLower().Contains(term)) ||
                a.Purpose.ToLower().Contains(term) ||
                (a.SignedDocumentOriginalFileName != null && a.SignedDocumentOriginalFileName.ToLower().Contains(term)) ||
                a.SignedDocuments.Any(d => d.OriginalFileName.ToLower().Contains(term) || (d.Description != null && d.Description.ToLower().Contains(term)))
            );
        }

        if (filter.DocumentFilter == SignedDocumentFilterType.Missing)
        {
            filter.TotalCount = await query.CountAsync();
            var pagedMissing = await query
                .OrderByDescending(a => a.PlannedStartTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            model.Items = pagedMissing.Select(a => new SignedDocumentItemViewModel
            {
                AssignmentId = a.Id,
                VehiclePlate = a.Vehicle.PlateNumber,
                VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                DriverFullName = GetAssignmentDisplayDrivers(a),
                RequestedByDepartment = a.RequestedByDepartment ?? string.Empty,
                Purpose = a.Purpose,
                PlannedStartTime = a.PlannedStartTime,
                PlannedEndTime = a.PlannedEndTime,
                Status = a.Status
            }).ToList();

            return model;
        }

        // SignedDocumentFilterType.All: Çoklu belgeleri alt alta satırlara aç
        var allAssignments = await query
            .OrderByDescending(a => a.PlannedStartTime)
            .ToListAsync();

        var allExpandedItems = new List<SignedDocumentItemViewModel>();
        foreach (var a in allAssignments)
        {
            var activeDocs = a.SignedDocuments
                .Where(sd => !sd.IsDeleted)
                .OrderByDescending(sd => sd.UploadedAt)
                .ToList();

            if (activeDocs.Count > 0)
            {
                foreach (var d in activeDocs)
                {
                    allExpandedItems.Add(new SignedDocumentItemViewModel
                    {
                        AssignmentId = a.Id,
                        VehiclePlate = a.Vehicle.PlateNumber,
                        VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                        DriverFullName = !string.IsNullOrWhiteSpace(d.Driver?.User?.FullName)
                            ? d.Driver.User.FullName
                            : GetAssignmentDisplayDrivers(a),
                        RequestedByDepartment = a.RequestedByDepartment ?? string.Empty,
                        Purpose = a.Purpose,
                        PlannedStartTime = a.PlannedStartTime,
                        PlannedEndTime = a.PlannedEndTime,
                        Status = a.Status,
                        DocumentId = d.Id,
                        DocumentCount = 1,
                        DocumentOriginalFileName = d.OriginalFileName,
                        DocumentPath = d.FilePath,
                        UploadedAt = d.UploadedAt,
                        UploadedByFullName = d.UploadedByUser?.FullName ?? d.UploadedByUser?.UserName
                    });
                }
            }
            else
            {
                allExpandedItems.Add(new SignedDocumentItemViewModel
                {
                    AssignmentId = a.Id,
                    VehiclePlate = a.Vehicle.PlateNumber,
                    VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                    DriverFullName = GetAssignmentDisplayDrivers(a),
                    RequestedByDepartment = a.RequestedByDepartment ?? string.Empty,
                    Purpose = a.Purpose,
                    PlannedStartTime = a.PlannedStartTime,
                    PlannedEndTime = a.PlannedEndTime,
                    Status = a.Status
                });
            }
        }

        filter.TotalCount = allExpandedItems.Count;
        model.Items = allExpandedItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return model;
    }

    public async Task<(int PendingCount, List<AssignmentRouteChangeItemViewModel> LatestChanges)> GetPendingRouteChangeNotificationSummaryAsync()
    {
        var threshold = DateTime.UtcNow.AddHours(-24);
        var query = _context.AssignmentRouteChanges
            .Include(rc => rc.Assignment)
                .ThenInclude(a => a.Vehicle)
            .Where(rc => !rc.IsDeleted && (rc.Assignment.Status == AssignmentStatus.InProgress || rc.CreatedAt >= threshold));

        var count = await query.CountAsync();
        var items = await query
            .OrderByDescending(rc => rc.CreatedAt)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        var dtos = items.Select(rc => new AssignmentRouteChangeItemViewModel
        {
            Id = rc.Id,
            AssignmentId = rc.AssignmentId,
            VehiclePlate = rc.Assignment?.Vehicle?.PlateNumber ?? "-",
            OldRoute = rc.OldRoute,
            NewRoute = rc.NewRoute,
            Reason = rc.Reason,
            ChangedByUserId = rc.ChangedByUserId,
            ChangedByFullName = rc.ChangedByFullName,
            CreatedAt = rc.CreatedAt
        }).ToList();

        return (count, dtos);
    }

    #endregion
}
