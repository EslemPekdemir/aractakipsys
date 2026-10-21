// File: Services/Implementations/DriverService.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels;
using aractakipsys.Models.ViewModels.Assignment;
using aractakipsys.Models.ViewModels.Driver;
using aractakipsys.Models.ViewModels.Surucu;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class DriverService : IDriverService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DriverService> _logger;
    private readonly IKmValidationService _kmValidationService;

    public DriverService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService,
        ILogger<DriverService> logger,
        IKmValidationService kmValidationService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _logger = logger;
        _kmValidationService = kmValidationService;
    }

    // =========================================================================
    // 1. ŞOFÖR MOBİL PANELİ İŞLEMLERİ (SURUCU CONTROLLER)
    // =========================================================================

    public async Task<DriverAssignmentListViewModel> GetDriverAssignmentsAsync(string userId)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

        var viewModel = new DriverAssignmentListViewModel
        {
            DriverFullName = driver?.User?.FullName ?? "Şoför",
            DriverDepartment = driver?.Department ?? "Ulaştırma Birimi"
        };

        if (driver == null)
        {
            return viewModel;
        }

        var assignments = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .Where(a => !a.IsDeleted && (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)))
            .OrderByDescending(a => a.PlannedStartTime)
            .ToListAsync();

        var today = DateTime.Today;

        foreach (var item in assignments)
        {
            var card = new AssignmentCardViewModel
            {
                Id = item.Id,
                VehicleId = item.VehicleId,
                VehiclePlate = item.Vehicle.PlateNumber,
                VehicleBrandModel = $"{item.Vehicle.Brand} {item.Vehicle.Model}",
                VehicleType = item.Vehicle.VehicleType,
                RequestedByDepartment = item.RequestedByDepartment,
                Destination = item.Destination,
                Route = item.Route,
                Purpose = item.Purpose,
                PlannedStartTime = item.PlannedStartTime,
                PlannedEndTime = item.PlannedEndTime,
                ActualStartTime = item.ActualStartTime,
                ActualEndTime = item.ActualEndTime,
                StartKm = item.StartKm,
                EndKm = item.EndKm,
                Status = item.Status,
                DriverReturnNotes = item.DriverReturnNotes,
                VehicleCurrentKm = item.Vehicle.CurrentKm,
                StartReportedBySelf = item.StartReportedBySelf,
                StartApprovedByUserId = item.StartApprovedByUserId,
                EndReportedBySelf = item.EndReportedBySelf,
                EndApprovedByUserId = item.EndApprovedByUserId
            };

            // Eğer görev InProgress ama şoför bitiş bildirmişse, araç teslim edilmiş ve Şef onayı beklemektedir (aktif sürüşte değildir)
            if (item.Status == AssignmentStatus.InProgress && item.EndReportedBySelf && string.IsNullOrEmpty(item.EndApprovedByUserId))
            {
                viewModel.PendingApprovalAssignments.Add(card);
            }
            else if (item.Status == AssignmentStatus.InProgress)
            {
                viewModel.ActiveAssignment = card;
            }
            else if (item.Status == AssignmentStatus.Planned && item.PlannedStartTime.Date == today)
            {
                viewModel.TodayPlannedAssignments.Add(card);
            }
            else if (item.Status == AssignmentStatus.Planned && item.PlannedStartTime.Date > today)
            {
                viewModel.UpcomingAssignments.Add(card);
            }
            else if (item.Status == AssignmentStatus.Completed || item.Status == AssignmentStatus.Cancelled)
            {
                viewModel.PastAssignments.Add(card);
            }
        }

        // Otoparktaki müsait araçları yükle (Gece vardiyası / acil görev için)
        if (viewModel.ActiveAssignment == null)
        {
            var idleVehicles = await _context.Vehicles
                .Where(v => !v.IsDeleted && v.Status == VehicleStatus.Idle)
                .Where(v => !_context.Maintenances.Any(m => !m.IsDeleted && m.VehicleId == v.Id && m.EndDate == null))
                .OrderBy(v => v.PlateNumber)
                .Select(v => new SelectableVehicleViewModel
                {
                    Id = v.Id,
                    PlateNumber = v.PlateNumber,
                    BrandModel = v.Brand + " " + v.Model,
                    CurrentKm = v.CurrentKm,
                    IsAvailable = true
                })
                .ToListAsync();

            viewModel.AvailableVehicles = idleVehicles;
        }

        return viewModel;
    }

    public async Task<DriverAssignmentDetailViewModel?> GetAssignmentDetailAsync(int assignmentId, string userId)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

        if (driver == null)
        {
            return null;
        }

        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted && (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)));

        if (assignment == null)
        {
            return null;
        }

        return new DriverAssignmentDetailViewModel
        {
            AssignmentId = assignment.Id,
            VehicleId = assignment.VehicleId,
            VehiclePlate = assignment.Vehicle.PlateNumber,
            VehicleBrand = assignment.Vehicle.Brand,
            VehicleModel = assignment.Vehicle.Model,
            VehicleColor = assignment.Vehicle.Color,
            VehicleType = assignment.Vehicle.VehicleType,
            VehicleFuelType = assignment.Vehicle.FuelType,
            CurrentVehicleKm = assignment.Vehicle.CurrentKm,
            VehicleStatus = assignment.Vehicle.Status,
            RequestedByDepartment = assignment.RequestedByDepartment,
            Destination = string.IsNullOrWhiteSpace(assignment.Destination) ? assignment.Purpose : assignment.Destination,
            Purpose = assignment.Purpose,
            Route = assignment.Route,
            PlannedStartTime = assignment.PlannedStartTime,
            PlannedEndTime = assignment.PlannedEndTime,
            ActualStartTime = assignment.ActualStartTime,
            ActualEndTime = assignment.ActualEndTime,
            StartKm = assignment.StartKm,
            EndKm = assignment.EndKm,
            Status = assignment.Status,
            DriverReturnNotes = assignment.DriverReturnNotes,
            StartReportedBySelf = assignment.StartReportedBySelf,
            StartApprovedByUserId = assignment.StartApprovedByUserId,
            EndReportedBySelf = assignment.EndReportedBySelf,
            EndApprovedByUserId = assignment.EndApprovedByUserId,
            CheckOil = assignment.CheckOil,
            CheckWater = assignment.CheckWater,
            CheckFuel = assignment.CheckFuel,
            CheckHydraulic = assignment.CheckHydraulic,
            CheckTires = assignment.CheckTires,
            ChecksCompletedAt = assignment.ChecksCompletedAt
        };
    }

    public async Task<AjaxResultViewModel> StartAssignmentAsync(int assignmentId, string userId, StartAssignmentRequest request)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

        if (driver == null)
        {
            return AjaxResultViewModel.Error("Şoför profiliniz bulunamadı veya hesabınız pasif durumda.");
        }

        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted && (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)));

        if (assignment == null)
        {
            return AjaxResultViewModel.Error("Görev bulunamadı veya bu göreve erişim yetkiniz yok.");
        }

        if (assignment.Status == AssignmentStatus.Cancelled)
        {
            return AjaxResultViewModel.Error("Bu görev iptal edilmiş, işlem yapılamaz.");
        }

        if (assignment.Status == AssignmentStatus.Completed)
        {
            return AjaxResultViewModel.Error("Bu görev tamamlanmış, işlem yapılamaz.");
        }

        if (assignment.Status != AssignmentStatus.Planned)
        {
            return AjaxResultViewModel.Error($"Yalnızca planlanmış görevler için başlangıç bildirilebilir. Mevcut durum: {assignment.Status.ToTurkishString()}");
        }

        if (assignment.StartReportedBySelf && assignment.StartApprovedByUserId == null)
        {
            return AjaxResultViewModel.Error("Bu görev için başlangıç bildiriminiz zaten alınmış olup Şef onayı beklemektedir.");
        }

        // Kural: Başlatılacak aracın üzerinde hâlihazırda InProgress olan başka bir görev varsa şoför başlangıç bildiremez!
        var hasActiveAssignmentOnVehicle = await _context.Assignments
            .AnyAsync(a => !a.IsDeleted &&
                           a.VehicleId == assignment.VehicleId &&
                           a.Id != assignment.Id &&
                           a.Status == AssignmentStatus.InProgress);

        if (hasActiveAssignmentOnVehicle)
        {
            return AjaxResultViewModel.Error("Bu araç şu anda başka bir görevde, önce mevcut görevi tamamlayın.");
        }

        // Kural: Şoförün hâlihazırda InProgress olan başka bir görevi varsa başlangıç bildiremez!
        var hasActiveAssignmentOnDriver = await _context.Assignments
            .Include(a => a.AssignmentDrivers)
            .AnyAsync(a => !a.IsDeleted &&
                           (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)) &&
                           a.Id != assignment.Id &&
                           a.Status == AssignmentStatus.InProgress);

        if (hasActiveAssignmentOnDriver)
        {
            return AjaxResultViewModel.Error("Şu anda devam eden başka bir göreviniz bulunmaktadır. Önce mevcut görevi tamamlamalısınız.");
        }

        var kmCheck = _kmValidationService.ValidateStartKm(request.StartKm, assignment.Vehicle.CurrentKm, false);
        if (!kmCheck.IsValid)
        {
            return AjaxResultViewModel.Error(kmCheck.Message!);
        }

        var oldAssignmentState = new
        {
            assignment.Status,
            assignment.ActualStartTime,
            assignment.StartKm,
            assignment.StartReportedBySelf,
            assignment.StartApprovedByUserId
        };

        // NOT: Status ve Vehicle.Status DEĞİŞTİRİLMEZ — Şef onayı beklenir (Türkiye yerel saati kullanılır)
        assignment.ActualStartTime = DateTime.Now;
        assignment.StartKm = request.StartKm;
        assignment.StartReportedBySelf = true;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = userId;

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
                    assignment.ActualStartTime,
                    assignment.StartKm,
                    assignment.StartReportedBySelf,
                    ReportedByDriverFullName = driver.User?.FullName ?? "Şoför",
                    Note = "Şoför tarafından bildirildi, onay bekliyor"
                }
            );

            _logger.LogInformation("Görev başlangıcı şoför tarafından bildirildi (Onay Bekliyor). AssignmentId: {Id}, DriverUserId: {UserId}, StartKm: {StartKm}", 
                assignment.Id, userId, request.StartKm);

            return AjaxResultViewModel.Ok("Bilgileriniz kaydedildi, Şef onayı bekleniyor.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görev başlangıcı bildirilirken veritabanı hatası oluştu. AssignmentId: {Id}", assignmentId);
            return AjaxResultViewModel.Error("Görev başlangıcı bildirilirken bir sistem hatası oluştu. Lütfen tekrar deneyiniz.");
        }
    }

    public async Task<AjaxResultViewModel> EndAssignmentAsync(int assignmentId, string userId, EndAssignmentRequest request)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

        if (driver == null)
        {
            return AjaxResultViewModel.Error("Şoför profiliniz bulunamadı veya hesabınız pasif durumda.");
        }

        var assignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted && (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)));

        if (assignment == null)
        {
            return AjaxResultViewModel.Error("Görev bulunamadı veya bu göreve erişim yetkiniz yok.");
        }

        if (assignment.Status == AssignmentStatus.Cancelled)
        {
            return AjaxResultViewModel.Error("Bu görev iptal edilmiş, işlem yapılamaz.");
        }

        if (assignment.Status == AssignmentStatus.Completed)
        {
            return AjaxResultViewModel.Error("Bu görev zaten tamamlanmış, işlem yapılamaz.");
        }

        if (assignment.Status != AssignmentStatus.InProgress && !assignment.StartReportedBySelf)
        {
            return AjaxResultViewModel.Error("Görevi bitirebilmek için önce göreve başlandığının bildirilmesi veya görevin başlatılmış olması gerekir.");
        }

        if (assignment.EndReportedBySelf && assignment.EndApprovedByUserId == null)
        {
            return AjaxResultViewModel.Error("Bu görev için bitiş bildiriminiz zaten alınmış olup Şef onayı beklemektedir.");
        }

        if (!assignment.StartKm.HasValue)
        {
            return AjaxResultViewModel.Error("Görevin başlangıç kilometresi sistemde kayıtlı değil.");
        }

        var kmCheck = _kmValidationService.ValidateEndKm(request.EndKm, assignment.StartKm.Value, false);
        if (!kmCheck.IsValid)
        {
            return AjaxResultViewModel.Error(kmCheck.Message!);
        }

        var oldAssignmentState = new
        {
            assignment.Status,
            assignment.ActualEndTime,
            assignment.EndKm,
            assignment.DriverReturnNotes,
            assignment.EndReportedBySelf,
            assignment.EndApprovedByUserId
        };

        // NOT: Status ve Vehicle.Status / CurrentKm DEĞİŞTİRİLMEZ — Şef onayı beklenir (Türkiye yerel saati kullanılır)
        assignment.ActualEndTime = DateTime.Now;
        assignment.EndKm = request.EndKm;
        assignment.DriverReturnNotes = string.IsNullOrWhiteSpace(request.DriverReturnNotes) ? null : request.DriverReturnNotes.Trim();
        assignment.EndReportedBySelf = true;
        assignment.CheckOil = request.CheckOil;
        assignment.CheckWater = request.CheckWater;
        assignment.CheckFuel = request.CheckFuel;
        assignment.CheckHydraulic = request.CheckHydraulic;
        assignment.CheckTires = request.CheckTires;
        if (request.CheckOil || request.CheckWater || request.CheckFuel || request.CheckHydraulic || request.CheckTires)
        {
            assignment.ChecksCompletedAt = DateTime.Now;
        }
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = userId;

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
                    assignment.ActualEndTime,
                    assignment.EndKm,
                    assignment.DriverReturnNotes,
                    assignment.EndReportedBySelf,
                    assignment.CheckOil,
                    assignment.CheckWater,
                    assignment.CheckFuel,
                    assignment.CheckHydraulic,
                    assignment.CheckTires,
                    assignment.ChecksCompletedAt,
                    ReportedByDriverFullName = driver.User?.FullName ?? "Şoför",
                    Note = "Şoför tarafından bildirildi, onay bekliyor"
                }
            );

            _logger.LogInformation("Görev bitişi şoför tarafından bildirildi (Onay Bekliyor). AssignmentId: {Id}, EndKm: {EndKm}", 
                assignment.Id, request.EndKm);

            return AjaxResultViewModel.Ok("Bilgileriniz kaydedildi, Şef onayı bekleniyor.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görev bitişi bildirilirken veritabanı hatası oluştu. AssignmentId: {Id}", assignmentId);
            return AjaxResultViewModel.Error("Görev tamamlanırken bir sistem hatası oluştu. Lütfen tekrar deneyiniz.");
        }
    }

    public async Task<AjaxResultViewModel> SaveVehicleChecksAsync(int assignmentId, string userId, DriverVehicleChecksRequest request)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

        if (driver == null)
        {
            return AjaxResultViewModel.Error("Şoför profiliniz bulunamadı veya hesabınız pasif durumda.");
        }

        var assignment = await _context.Assignments
            .Include(a => a.AssignmentDrivers)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted && (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)));

        if (assignment == null)
        {
            return AjaxResultViewModel.Error("Görev bulunamadı veya bu göreve erişim yetkiniz yok.");
        }

        // Görev başlamamış (Planned), bitmiş (Completed) veya iptal edilmişse kontroller değiştirilemez
        if (assignment.Status != AssignmentStatus.InProgress)
        {
            return AjaxResultViewModel.Error("Araç kontrol tikleri yalnızca devam eden (başlatılmış) görevlerde güncellenebilir.");
        }

        var oldChecks = new
        {
            assignment.CheckOil,
            assignment.CheckWater,
            assignment.CheckFuel,
            assignment.CheckHydraulic,
            assignment.CheckTires,
            assignment.ChecksCompletedAt
        };

        assignment.CheckOil = request.CheckOil;
        assignment.CheckWater = request.CheckWater;
        assignment.CheckFuel = request.CheckFuel;
        assignment.CheckHydraulic = request.CheckHydraulic;
        assignment.CheckTires = request.CheckTires;
        assignment.ChecksCompletedAt = DateTime.Now;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(Assignment),
            assignment.Id,
            AuditAction.Update,
            userId,
            oldChecks,
            new
            {
                AssignmentId = assignment.Id,
                Action = "VehicleChecksSaved",
                ReportedByDriverFullName = driver.User?.FullName ?? "Şoför",
                assignment.CheckOil,
                assignment.CheckWater,
                assignment.CheckFuel,
                assignment.CheckHydraulic,
                assignment.CheckTires,
                assignment.ChecksCompletedAt
            });

        return AjaxResultViewModel.Ok("Araç kontrol bilgileri başarıyla kaydedildi.");
    }

    /// <summary>
    /// Şoförün gece vardiyasında / acil durumda kendine görev atayıp başlatması
    /// </summary>
    public async Task<AjaxResultViewModel> CreateSelfAssignmentAsync(string userId, EmergencySelfAssignmentRequest request)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive);

        if (driver == null)
        {
            return AjaxResultViewModel.Error("Sistemde aktif şoför kaydınız bulunamadı.");
        }

        if (driver.LicenseExpiryDate.Date < DateTime.Today)
        {
            return AjaxResultViewModel.Error($"Ehliyetinizin geçerlilik süresi ({driver.LicenseExpiryDate:dd.MM.yyyy}) dolmuştur. Görev başlatamazsınız.");
        }

        // Halihazırda devam eden ve henüz teslim edilmemiş aktif bir görev var mı?
        var hasActiveAssignment = await _context.Assignments
            .AnyAsync(a => !a.IsDeleted && a.DriverId == driver.Id && a.Status == AssignmentStatus.InProgress && !a.EndReportedBySelf);

        if (hasActiveAssignment)
        {
            return AjaxResultViewModel.Error("Halihazırda seyir halinde devam eden aktif bir göreviniz bulunmaktadır. Yeni bir görev başlatmadan önce mevcut görevi bitirip teslim etmelisiniz.");
        }

        // Seçilen araç kontrolü
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted);
        if (vehicle == null)
        {
            return AjaxResultViewModel.Error("Seçilen araç sistemde bulunamadı.");
        }

        if (vehicle.Status != VehicleStatus.Idle)
        {
            return AjaxResultViewModel.Error($"Seçilen araç şu anda müsait değil (Durum: {vehicle.Status.ToTurkishString()}). Lütfen otoparktaki müsait başka bir aracı seçiniz.");
        }

        var isUnderMaintenance = await _context.Maintenances
            .AnyAsync(m => !m.IsDeleted && m.VehicleId == vehicle.Id && m.EndDate == null);

        if (isUnderMaintenance)
        {
            return AjaxResultViewModel.Error("Seçilen araç şu anda aktif bakımda olduğu için sefere çıkamaz.");
        }

        // KM kontrolü
        if (request.StartKm < vehicle.CurrentKm)
        {
            return AjaxResultViewModel.Error($"Başlangıç kilometresi ({request.StartKm:N0} KM), aracın güncel kilometresinden ({vehicle.CurrentKm:N0} KM) küçük olamaz.");
        }

        var now = DateTime.Now;
        var estHours = request.EstimatedHours > 0 ? request.EstimatedHours : 4;
        var plannedEnd = now.AddHours(estHours);

        var assignment = new Assignment
        {
            VehicleId = vehicle.Id,
            DriverId = driver.Id,
            RequestedByDepartment = string.IsNullOrWhiteSpace(request.RequestedByDepartment) ? "Gece Vardiyası / Acil İntikal" : request.RequestedByDepartment.Trim(),
            Purpose = request.Purpose.Trim(),
            PlannedStartTime = now,
            PlannedEndTime = plannedEnd,
            ActualStartTime = now,
            StartKm = request.StartKm,
            Status = AssignmentStatus.InProgress,
            StartReportedBySelf = true,
            StartApprovedByUserId = null,
            EndReportedBySelf = false,
            EndApprovedByUserId = null,
            CreatedAt = now,
            CreatedBy = userId
        };

        // Araç durumunu OnDuty yap
        vehicle.Status = VehicleStatus.OnDuty;
        vehicle.UpdatedAt = now;
        vehicle.UpdatedBy = userId;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Assignment),
                assignment.Id,
                AuditAction.Create,
                userId,
                null,
                new
                {
                    assignment.VehicleId,
                    vehicle.PlateNumber,
                    assignment.DriverId,
                    DriverName = driver.User.FullName,
                    assignment.RequestedByDepartment,
                    assignment.Purpose,
                    assignment.Status,
                    assignment.StartKm,
                    assignment.StartReportedBySelf,
                    Note = "Şoför gece vardiyası / acil görevi başlattı, Şef onayı bekleniyor"
                }
            );

            await transaction.CommitAsync();

            _logger.LogInformation("Şoför tarafından acil görev başlatıldı. AssignmentId: {AssignmentId}, Plate: {Plate}, Driver: {Driver}",
                assignment.Id, vehicle.PlateNumber, driver.User.FullName);

            return AjaxResultViewModel.Ok("Gece vardiyası / acil görev başarıyla başlatıldı.", new { assignmentId = assignment.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Gece vardiyası acil görevi başlatılırken hata oluştu. UserId: {UserId}", userId);
            return AjaxResultViewModel.Error("Görev başlatılırken beklenmeyen bir hata oluştu: " + ex.Message);
        }
    }

    // =========================================================================
    // 2. ADMIN ŞOFÖR YÖNETİMİ (CRUD) İŞLEMLERİ
    // =========================================================================

    public async Task<DriverFilterViewModel> GetDriversPagedAsync(DriverFilterViewModel filter)
    {
        var query = _context.Drivers
            .Include(d => d.User)
            .AsNoTracking()
            .AsQueryable();

        // Departman listesi (dropdown için)
        filter.DepartmentList = await _context.Drivers
            .Select(d => d.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        // Filtreler
        if (filter.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            query = query.Where(d => d.Department == filter.Department);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(d =>
                d.User.FullName.ToLower().Contains(search) ||
                d.User.Email.ToLower().Contains(search) ||
                (d.User.PhoneNumber != null && d.User.PhoneNumber.ToLower().Contains(search)) ||
                d.LicenseNumber.ToLower().Contains(search));
        }

        filter.TotalCount = await query.CountAsync();
        filter.PageNumber = Math.Max(1, filter.PageNumber);
        filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DriverListItemViewModel
            {
                Id = d.Id,
                UserId = d.UserId,
                FullName = d.User.FullName,
                Email = d.User.Email ?? string.Empty,
                PhoneNumber = d.User.PhoneNumber,
                LicenseNumber = d.LicenseNumber,
                LicenseClass = d.LicenseClass,
                LicenseExpiryDate = d.LicenseExpiryDate,
                Department = d.Department,
                IsActive = d.IsActive
            })
            .ToListAsync();

        filter.Drivers = items;
        return filter;
    }

    public async Task<Driver?> GetDriverByIdAsync(int id)
    {
        return await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DriverEditViewModel?> GetDriverEditModelAsync(int id)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
        {
            return null;
        }

        return new DriverEditViewModel
        {
            Id = driver.Id,
            UserId = driver.UserId,
            FullName = driver.User.FullName,
            Email = driver.User.Email ?? string.Empty,
            PhoneNumber = driver.User.PhoneNumber,
            LicenseNumber = driver.LicenseNumber,
            LicenseClass = driver.LicenseClass,
            LicenseExpiryDate = driver.LicenseExpiryDate,
            Department = driver.Department,
            IsActive = driver.IsActive
        };
    }

    public async Task<DriverDetailsViewModel?> GetDriverDetailsAsync(int id)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
        {
            return null;
        }

        // Şoförün devam eden aktif görevi
        var activeAssignment = await _context.Assignments
            .Include(a => a.Vehicle)
            .Where(a => a.DriverId == id && a.Status == AssignmentStatus.InProgress)
            .Select(a => new DriverActiveAssignmentViewModel
            {
                Id = a.Id,
                VehiclePlate = a.Vehicle.PlateNumber,
                VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                Purpose = a.Purpose,
                PlannedStartTime = a.PlannedStartTime,
                ActualStartTime = a.ActualStartTime,
                StartKm = a.StartKm
            })
            .FirstOrDefaultAsync();

        // Son 5 Görevlendirme
        var recentAssignments = await _context.Assignments
            .Include(a => a.Vehicle)
            .Where(a => a.DriverId == id)
            .OrderByDescending(a => a.PlannedStartTime)
            .Take(5)
            .Select(a => new DriverAssignmentHistoryViewModel
            {
                Id = a.Id,
                VehiclePlate = a.Vehicle.PlateNumber,
                VehicleBrandModel = $"{a.Vehicle.Brand} {a.Vehicle.Model}",
                Purpose = a.Purpose,
                PlannedStartTime = a.PlannedStartTime,
                PlannedEndTime = a.PlannedEndTime,
                StartKm = a.StartKm,
                EndKm = a.EndKm,
                Status = a.Status
            })
            .ToListAsync();

        // İstatistikler
        var completedAssignments = await _context.Assignments
            .Where(a => a.DriverId == id && a.Status == AssignmentStatus.Completed && a.StartKm.HasValue && a.EndKm.HasValue)
            .Select(a => new { Distance = a.EndKm!.Value - a.StartKm!.Value })
            .ToListAsync();

        return new DriverDetailsViewModel
        {
            Id = driver.Id,
            UserId = driver.UserId,
            FullName = driver.User.FullName,
            Email = driver.User.Email ?? string.Empty,
            PhoneNumber = driver.User.PhoneNumber,
            LicenseNumber = driver.LicenseNumber,
            LicenseClass = driver.LicenseClass,
            LicenseExpiryDate = driver.LicenseExpiryDate,
            Department = driver.Department,
            IsActive = driver.IsActive,
            CreatedAt = driver.CreatedAt,
            UpdatedAt = driver.UpdatedAt,
            CurrentActiveAssignment = activeAssignment,
            RecentAssignments = recentAssignments,
            TotalCompletedAssignments = completedAssignments.Count,
            TotalDrivenKm = completedAssignments.Sum(a => a.Distance)
        };
    }

    public async Task<(bool Success, string Message, int DriverId)> CreateDriverAsync(DriverCreateViewModel model, string adminUserId)
    {
        var cleanEmail = model.Email.Trim().ToLower();
        var cleanLicense = model.LicenseNumber.Trim().ToUpper();

        // 1. E-posta tekillik kontrolü
        var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingUser != null)
        {
            return (false, $"'{cleanEmail}' e-posta adresi ile kayıtlı bir kullanıcı zaten mevcuttur.", 0);
        }

        // 2. Ehliyet numarası tekillik kontrolü (Silinmişler dahil - IgnoreQueryFilters)
        var existingLicense = await _context.Drivers
            .IgnoreQueryFilters()
            .AnyAsync(d => d.LicenseNumber == cleanLicense);

        if (existingLicense)
        {
            return (false, "Bu ehliyet numarası sistemde daha önce kayıtlı (veya silinmiş) bir şoföre aittir, kullanılamaz.", 0);
        }

        // 3. Atomik Transaction (AGENTS.md kuralı)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Identity User oluştur
            var user = new ApplicationUser
            {
                UserName = cleanEmail.Split('@')[0] + "_" + Guid.NewGuid().ToString("N")[..4],
                Email = cleanEmail,
                FullName = model.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var userResult = await _userManager.CreateAsync(user, model.Password);
            if (!userResult.Succeeded)
            {
                await transaction.RollbackAsync();
                var errors = string.Join(", ", userResult.Errors.Select(e => e.Description));
                return (false, $"Kullanıcı oluşturulamadı: {errors}", 0);
            }

            // Driver Rolü Ata
            var roleResult = await _userManager.AddToRoleAsync(user, "Driver");
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return (false, "Kullanıcıya Şoför rolü atanırken hata oluştu.", 0);
            }

            // Driver Entity Oluştur
            var driver = new Driver
            {
                UserId = user.Id,
                LicenseNumber = cleanLicense,
                LicenseClass = model.LicenseClass.Trim().ToUpper(),
                LicenseExpiryDate = model.LicenseExpiryDate.Date,
                Department = model.Department.Trim(),
                IsActive = true,
                CreatedBy = adminUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Driver),
                driver.Id,
                AuditAction.Create,
                adminUserId,
                null,
                new { driver.LicenseNumber, driver.LicenseClass, driver.Department, user.FullName, user.Email }
            );

            _logger.LogInformation("Yeni şoför eklendi. DriverId: {Id}, FullName: {FullName}, Email: {Email}",
                driver.Id, user.FullName, user.Email);

            return (true, $"'{user.FullName}' adlı şoför başarıyla sisteme eklendi.", driver.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Şoför eklenirken transaction hatası oluştu.");
            return (false, "Şoför kaydı oluşturulurken bir sistem hatası oluştu.", 0);
        }
    }

    public async Task<(bool Success, string Message)> UpdateDriverAsync(DriverEditViewModel model, string adminUserId)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == model.Id);

        if (driver == null)
        {
            return (false, "Güncellenmek istenen şoför kaydı bulunamadı.");
        }

        var cleanEmail = model.Email.Trim().ToLower();
        var cleanLicense = model.LicenseNumber.Trim().ToUpper();

        // 1. E-posta tekillik kontrolü
        var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingUser != null && existingUser.Id != driver.UserId)
        {
            return (false, $"'{cleanEmail}' e-posta adresi başka bir kullanıcı tarafından kullanılmaktadır.");
        }

        // 2. Ehliyet numarası tekillik kontrolü (Kendi ID'si hariç, silinmişler dahil - IgnoreQueryFilters)
        var existingLicense = await _context.Drivers
            .IgnoreQueryFilters()
            .AnyAsync(d => d.LicenseNumber == cleanLicense && d.Id != model.Id);

        if (existingLicense)
        {
            return (false, "Bu ehliyet numarası sistemde daha önce kayıtlı (veya silinmiş) bir şoföre aittir, kullanılamaz.");
        }

        // 3. Pasife alma kontrolü: Aktif görevi varsa pasife alınamaz
        if (!model.IsActive && driver.IsActive)
        {
            var hasActiveAssignments = await _context.Assignments.AnyAsync(a =>
                a.DriverId == model.Id &&
                (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress));

            if (hasActiveAssignments)
            {
                return (false, $"'{driver.User.FullName}' adlı şoförün devam eden veya planlanmış aktif görevleri bulunmaktadır. Görevler tamamlanmadan veya iptal edilmeden şoför pasife alınamaz.");
            }
        }

        var oldState = new
        {
            driver.User.FullName,
            driver.User.Email,
            driver.User.PhoneNumber,
            driver.LicenseNumber,
            driver.LicenseClass,
            driver.LicenseExpiryDate,
            driver.Department,
            driver.IsActive
        };

        // Değerleri güncelle
        driver.User.FullName = model.FullName.Trim();
        driver.User.Email = cleanEmail;
        driver.User.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        driver.User.IsActive = model.IsActive;

        driver.LicenseNumber = cleanLicense;
        driver.LicenseClass = model.LicenseClass.Trim().ToUpper();
        driver.LicenseExpiryDate = model.LicenseExpiryDate.Date;
        driver.Department = model.Department.Trim();
        driver.IsActive = model.IsActive;
        driver.UpdatedAt = DateTime.UtcNow;
        driver.UpdatedBy = adminUserId;

        try
        {
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Driver),
                driver.Id,
                AuditAction.Update,
                adminUserId,
                oldState,
                new
                {
                    driver.User.FullName,
                    driver.User.Email,
                    driver.User.PhoneNumber,
                    driver.LicenseNumber,
                    driver.LicenseClass,
                    driver.LicenseExpiryDate,
                    driver.Department,
                    driver.IsActive
                }
            );

            _logger.LogInformation("Şoför bilgileri güncellendi. DriverId: {Id}", driver.Id);
            return (true, $"'{driver.User.FullName}' adlı şoförün bilgileri başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şoför güncellenirken hata oluştu. DriverId: {Id}", model.Id);
            return (false, "Şoför bilgileri güncellenirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleDriverStatusAsync(int id, string adminUserId)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
        {
            return (false, "Şoför kaydı bulunamadı.");
        }

        // Aktiften pasife geçişte görev kontrolü
        if (driver.IsActive)
        {
            var hasActiveAssignments = await _context.Assignments.AnyAsync(a =>
                a.DriverId == id &&
                (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress));

            if (hasActiveAssignments)
            {
                return (false, $"'{driver.User.FullName}' adlı şoförün devam eden veya planlanmış aktif görevi bulunmaktadır. Şoför pasife alınamaz.");
            }
        }

        var oldStatus = driver.IsActive;
        driver.IsActive = !driver.IsActive;
        driver.User.IsActive = driver.IsActive;
        driver.UpdatedAt = DateTime.UtcNow;
        driver.UpdatedBy = adminUserId;

        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Driver),
                driver.Id,
                AuditAction.Update,
                adminUserId,
                new { IsActive = oldStatus },
                new { IsActive = driver.IsActive }
            );

            var statusStr = driver.IsActive ? "Aktif" : "Pasif";
            _logger.LogInformation("Şoför durumu değiştirildi. DriverId: {Id}, Yeni Durum: {Status}", driver.Id, statusStr);
            return (true, $"'{driver.User.FullName}' adlı şoförün durumu '{statusStr}' olarak güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şoför durumu değiştirilirken hata oluştu. DriverId: {Id}", id);
            return (false, "Şoför durumu güncellenirken bir hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteDriverAsync(int id, string adminUserId)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (driver == null)
        {
            return (false, "Silinmek istenen şoför bulunamadı.");
        }

        // Aktif/Planlı görev kontrolü (ENDPOINT-SPESIFIKASYONU 2.27)
        var hasActiveAssignments = await _context.Assignments.AnyAsync(a =>
            a.DriverId == id &&
            (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress));

        if (hasActiveAssignments)
        {
            return (false, $"'{driver.User.FullName}' adlı şoföre ait planlanan veya devam eden aktif görev(ler) bulunmaktadır. Görevler tamamlanmadan veya iptal edilmeden şoför silinemez.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Soft delete
            driver.IsDeleted = true;
            driver.IsActive = false;
            driver.UpdatedAt = DateTime.UtcNow;
            driver.UpdatedBy = adminUserId;

            driver.User.IsActive = false;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Driver),
                driver.Id,
                AuditAction.Delete,
                adminUserId,
                new { driver.User.FullName, driver.LicenseNumber, driver.Department },
                new { IsDeleted = true }
            );

            _logger.LogInformation("Şoför silindi (Soft Delete). DriverId: {Id}, FullName: {FullName}", driver.Id, driver.User.FullName);
            return (true, $"'{driver.User.FullName}' adlı şoför başarıyla sistemden silindi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Şoför silinirken transaction hatası oluştu. DriverId: {Id}", id);
            return (false, "Şoför silinirken bir veritabanı hatası oluştu.");
        }
    }

    /// <summary>
    /// Yönetici tarafından şoförün şifresini doğrudan sıfırlama / yeni parola belirleme
    /// </summary>
    public async Task<(bool Success, string Message)> ResetDriverPasswordAsync(int driverId, string newPassword, string adminUserId)
    {
        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted);

        if (driver == null || driver.User == null)
        {
            return (false, "Şoför kaydı bulunamadı.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(driver.User);
        var result = await _userManager.ResetPasswordAsync(driver.User, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Şifre güncellenemedi: {errors}");
        }

        // Kilitlenmişse kilidini aç
        if (await _userManager.IsLockedOutAsync(driver.User))
        {
            await _userManager.SetLockoutEndDateAsync(driver.User, null);
        }

        await _auditLogService.LogAsync(
            nameof(Driver),
            driver.Id,
            AuditAction.Update,
            adminUserId,
            new { Note = "Şifre sıfırlandı" },
            new { Note = "Yönetici tarafından yeni şifre atandı", driver.User.UserName }
        );

        _logger.LogInformation("Yönetici {AdminId} tarafından şoför {DriverId} ({UserName}) için yeni şifre atandı.",
            adminUserId, driver.Id, driver.User.UserName);

        return (true, $"'{driver.User.FullName}' adlı şoförün şifresi başarıyla güncellendi.");
    }
}
