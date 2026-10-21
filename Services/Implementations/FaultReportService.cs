// File: Services/Implementations/FaultReportService.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels;
using aractakipsys.Models.ViewModels.FaultReport;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class FaultReportService : IFaultReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<FaultReportService> _logger;

    public FaultReportService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        UserManager<ApplicationUser> userManager,
        ILogger<FaultReportService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<FaultReportItemViewModel>> GetFaultReportsAsync(FaultReportStatus? status = null, int? vehicleId = null)
    {
        var query = _context.VehicleFaultReports
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
                .ThenInclude(d => d.User)
            .Include(x => x.ReviewerUser)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (vehicleId.HasValue && vehicleId.Value > 0)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        var list = await query
            .OrderByDescending(x => x.ReportDate)
            .ToListAsync();

        return list.Select(x => new FaultReportItemViewModel
        {
            Id = x.Id,
            VehicleId = x.VehicleId,
            VehiclePlate = x.Vehicle.PlateNumber,
            VehicleBrandModel = $"{x.Vehicle.Brand} {x.Vehicle.Model}",
            DriverId = x.DriverId,
            DriverFullName = x.Driver != null ? (x.Driver.User?.FullName ?? x.Driver.User?.UserName ?? "-") : (x.CreatedBy ?? "-"),
            AssignmentId = x.AssignmentId,
            CurrentKm = x.CurrentKm,
            Description = x.Description,
            ReportDate = x.ReportDate,
            Status = x.Status,
            ReviewerUserId = x.ReviewerUserId,
            ReviewerFullName = x.ReviewerUser?.FullName,
            ReviewerNotes = x.ReviewerNotes,
            ReviewedAt = x.ReviewedAt
        }).ToList();
    }

    public async Task<FaultReportItemViewModel?> GetFaultReportByIdAsync(int id)
    {
        var item = await _context.VehicleFaultReports
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
                .ThenInclude(d => d.User)
            .Include(x => x.ReviewerUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null) return null;

        return new FaultReportItemViewModel
        {
            Id = item.Id,
            VehicleId = item.VehicleId,
            VehiclePlate = item.Vehicle.PlateNumber,
            VehicleBrandModel = $"{item.Vehicle.Brand} {item.Vehicle.Model}",
            DriverId = item.DriverId,
            DriverFullName = item.Driver != null ? (item.Driver.User?.FullName ?? item.Driver.User?.UserName ?? "-") : (item.CreatedBy ?? "-"),
            AssignmentId = item.AssignmentId,
            CurrentKm = item.CurrentKm,
            Description = item.Description,
            ReportDate = item.ReportDate,
            Status = item.Status,
            ReviewerUserId = item.ReviewerUserId,
            ReviewerFullName = item.ReviewerUser?.FullName,
            ReviewerNotes = item.ReviewerNotes,
            ReviewedAt = item.ReviewedAt
        };
    }

    public async Task<AjaxResultViewModel> CreateFaultReportAsync(FaultReportCreateRequest request, string userId)
    {
        if (request == null)
            return AjaxResultViewModel.Fail("Geçersiz form verisi.");

        if (string.IsNullOrWhiteSpace(request.Description))
            return AjaxResultViewModel.Fail("Lütfen arıza açıklamasını detaylıca belirtiniz.");

        var vehicle = await _context.Vehicles.FindAsync(request.VehicleId);
        if (vehicle == null)
            return AjaxResultViewModel.Fail("Belirtilen araç sistemde bulunamadı.");

        var user = await _userManager.FindByIdAsync(userId);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        if (request.AssignmentId.HasValue && request.AssignmentId.Value > 0 && driver != null)
        {
            var isAssigned = await _context.Assignments
                .Include(a => a.AssignmentDrivers)
                .AnyAsync(a => a.Id == request.AssignmentId.Value && !a.IsDeleted &&
                               (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)));

            if (!isAssigned)
            {
                return AjaxResultViewModel.Fail("Bu görev için arıza bildirme yetkiniz bulunmamaktadır.");
            }
        }

        var report = new VehicleFaultReport
        {
            VehicleId = request.VehicleId,
            DriverId = driver?.Id,
            AssignmentId = request.AssignmentId,
            CurrentKm = request.CurrentKm > 0 ? request.CurrentKm : vehicle.CurrentKm,
            Description = request.Description.Trim(),
            ReportDate = DateTime.Now,
            Status = FaultReportStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user?.FullName ?? user?.UserName ?? "Şoför"
        };

        // Kural: Arıza kaydı araç durumunu otomatik DEĞİŞTİRMEZ (araç durumu mevcut kurallarla yönetilir)
        _context.VehicleFaultReports.Add(report);
        await _context.SaveChangesAsync();

        // AuditLog Kaydı
        await _auditLogService.LogAsync(
            nameof(VehicleFaultReport),
            report.Id,
            AuditAction.Create,
            userId,
            user?.FullName ?? user?.UserName ?? "Kullanıcı",
            new
            {
                FaultReportId = report.Id,
                VehiclePlate = vehicle.PlateNumber,
                report.CurrentKm,
                report.Description,
                Status = "Pending"
            });

        _logger.LogInformation("Yeni araç arıza bildirimi alındı. ID: {ReportId}, Araç: {Plate}, Bildiren: {User}",
            report.Id, vehicle.PlateNumber, user?.UserName);

        return AjaxResultViewModel.Ok("Arıza bildiriminiz başarıyla iletildi. Ulaşım Şefliği bilgilendirilmiştir.", new { reportId = report.Id });
    }

    public async Task<AjaxResultViewModel> ApproveFaultReportAsync(FaultReportApproveRequest request, string reviewerUserId)
    {
        var report = await _context.VehicleFaultReports
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (report == null)
            return AjaxResultViewModel.Fail("Onaylanacak arıza bildirimi bulunamadı.");

        var reviewer = await _userManager.FindByIdAsync(reviewerUserId);

        report.CurrentKm = request.CurrentKm;
        report.Description = request.Description.Trim();
        report.ReviewerUserId = reviewerUserId;
        report.ReviewerNotes = request.ReviewerNotes?.Trim();
        report.ReviewedAt = DateTime.Now;
        report.Status = FaultReportStatus.Approved;
        report.UpdatedAt = DateTime.UtcNow;
        report.UpdatedBy = reviewer?.FullName ?? reviewer?.UserName ?? "Ulaştırma Amiri";

        // Kural: Araç durumu otomatik değiştirilmez
        await _context.SaveChangesAsync();

        // AuditLog
        await _auditLogService.LogAsync(
            nameof(VehicleFaultReport),
            report.Id,
            AuditAction.Update,
            reviewerUserId,
            reviewer?.FullName ?? "Ulaştırma Amiri",
            new
            {
                FaultReportId = report.Id,
                VehiclePlate = report.Vehicle.PlateNumber,
                Action = "Approve",
                report.CurrentKm,
                report.Description,
                report.ReviewerNotes,
                NewStatus = "Approved"
            });

        return AjaxResultViewModel.Ok("Arıza bildirimi onaylandı ve inceleme kaydı güncellendi.");
    }

    public async Task<AjaxResultViewModel> UpdateStatusAsync(int id, FaultReportStatus status, string? notes, string reviewerUserId)
    {
        var report = await _context.VehicleFaultReports
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report == null)
            return AjaxResultViewModel.Fail("Arıza bildirimi bulunamadı.");

        var reviewer = await _userManager.FindByIdAsync(reviewerUserId);

        report.Status = status;
        report.ReviewerUserId = reviewerUserId;
        report.ReviewedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            report.ReviewerNotes = notes.Trim();
        }
        report.UpdatedAt = DateTime.UtcNow;
        report.UpdatedBy = reviewer?.FullName ?? reviewer?.UserName ?? "Ulaştırma Amiri";

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(VehicleFaultReport),
            report.Id,
            AuditAction.Update,
            reviewerUserId,
            reviewer?.FullName ?? "Ulaştırma Amiri",
            new
            {
                FaultReportId = report.Id,
                VehiclePlate = report.Vehicle.PlateNumber,
                NewStatus = status.ToString(),
                Notes = notes
            });

        return AjaxResultViewModel.Ok($"Arıza durumu '{status.ToTurkishString()}' olarak güncellendi.");
    }

    public async Task<FaultReportPdfModel?> GetFaultReportPdfModelAsync(int id)
    {
        var report = await _context.VehicleFaultReports
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
                .ThenInclude(d => d.User)
            .Include(x => x.ReviewerUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (report == null) return null;

        return new FaultReportPdfModel
        {
            Id = report.Id,
            VehiclePlate = report.Vehicle.PlateNumber,
            DriverFullName = report.Driver?.User?.FullName ?? report.Driver?.User?.UserName ?? report.CreatedBy ?? "-",
            ReportDate = report.ReportDate,
            CurrentKm = report.CurrentKm,
            Description = report.Description,
            ReviewerFullName = report.ReviewerUser?.FullName ?? string.Empty,
            IsBlank = false
        };
    }

    public Task<FaultReportPdfModel> GetBlankFaultReportPdfModelAsync(string reviewerFullName = "")
    {
        return Task.FromResult(new FaultReportPdfModel
        {
            Id = 0,
            VehiclePlate = "",
            DriverFullName = "",
            ReportDate = null,
            CurrentKm = null,
            Description = "",
            ReviewerFullName = reviewerFullName,
            IsBlank = true
        });
    }

    public async Task<int> GetPendingFaultCountAsync()
    {
        return await _context.VehicleFaultReports
            .CountAsync(x => x.Status == FaultReportStatus.Pending);
    }

    public async Task<List<FaultReportItemViewModel>> GetLatestPendingFaultsAsync(int take = 5)
    {
        var list = await _context.VehicleFaultReports
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
                .ThenInclude(d => d.User)
            .Where(x => x.Status == FaultReportStatus.Pending)
            .OrderByDescending(x => x.ReportDate)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return list.Select(x => new FaultReportItemViewModel
        {
            Id = x.Id,
            VehicleId = x.VehicleId,
            VehiclePlate = x.Vehicle.PlateNumber,
            VehicleBrandModel = $"{x.Vehicle.Brand} {x.Vehicle.Model}",
            DriverId = x.DriverId,
            DriverFullName = x.Driver != null ? (x.Driver.User?.FullName ?? x.Driver.User?.UserName ?? "-") : (x.CreatedBy ?? "-"),
            CurrentKm = x.CurrentKm,
            Description = x.Description,
            ReportDate = x.ReportDate,
            Status = x.Status
        }).ToList();
    }
}
