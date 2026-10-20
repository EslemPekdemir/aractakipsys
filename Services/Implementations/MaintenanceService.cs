// File: Services/Implementations/MaintenanceService.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Maintenance;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class MaintenanceService : IMaintenanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<MaintenanceService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    private static readonly string[] PredefinedMaintenanceTypes = new[]
    {
        "Periyodik Bakım",
        "Ağır Bakım / Triger",
        "Yağ ve Filtre Değişimi",
        "Fren Sistemi ve Balata Değişimi",
        "Lastik Değişimi / Rot-Balans",
        "Akü ve Elektrik Aksamı",
        "Kaporta / Boya Onarımı",
        "TÜVTÜRK Muayene Hazırlık",
        "Mekanik / Motor Onarımı",
        "Klima / Gaz Dolumu",
        "Genel Kontrol",
        "Diğer"
    };

    public async Task<MaintenanceFilterViewModel> GetMaintenancesPagedAsync(MaintenanceFilterViewModel filter)
    {
        var query = _context.Maintenances
            .Include(m => m.Vehicle)
            .AsNoTracking()
            .AsQueryable();

        // 1. Dropdown Listeleri
        filter.VehicleList = await _context.Vehicles
            .OrderBy(v => v.PlateNumber)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.PlateNumber} - {v.Brand} {v.Model}"
            })
            .ToListAsync();

        var existingDbTypes = await _context.Maintenances
            .Where(m => !m.IsDeleted && !string.IsNullOrWhiteSpace(m.MaintenanceType))
            .Select(m => m.MaintenanceType)
            .Distinct()
            .ToListAsync();

        filter.MaintenanceTypeList = PredefinedMaintenanceTypes
            .Union(existingDbTypes, StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        // 2. Filtreler
        if (filter.VehicleId.HasValue)
        {
            query = query.Where(m => m.VehicleId == filter.VehicleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.MaintenanceType))
        {
            query = query.Where(m => m.MaintenanceType == filter.MaintenanceType);
        }

        if (filter.IsOngoing.HasValue)
        {
            query = filter.IsOngoing.Value 
                ? query.Where(m => m.EndDate == null) 
                : query.Where(m => m.EndDate != null);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(m =>
                m.Vehicle.PlateNumber.ToLower().Contains(search) ||
                m.Vehicle.Brand.ToLower().Contains(search) ||
                m.Vehicle.Model.ToLower().Contains(search) ||
                m.MaintenanceType.ToLower().Contains(search) ||
                (m.Description != null && m.Description.ToLower().Contains(search)) ||
                (m.PerformedBy != null && m.PerformedBy.ToLower().Contains(search)));
        }

        filter.TotalCount = await query.CountAsync();
        filter.PageNumber = Math.Max(1, filter.PageNumber);
        filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        // İş Kuralı: Bakımlar - Devam edenlerde en uzun süredir açık olan (en eski başlangıç tarihli) en üstte
        var sortedQuery = (filter.IsOngoing.HasValue && filter.IsOngoing.Value)
            ? query.OrderBy(m => m.StartDate)
            : query.OrderByDescending(m => m.StartDate);

        var items = await sortedQuery
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => new MaintenanceListItemViewModel
            {
                Id = m.Id,
                VehicleId = m.VehicleId,
                VehiclePlate = m.Vehicle.PlateNumber,
                VehicleBrandModel = $"{m.Vehicle.Brand} {m.Vehicle.Model}",
                MaintenanceType = m.MaintenanceType,
                Description = m.Description,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                PerformedBy = m.PerformedBy
            })
            .ToListAsync();

        filter.Maintenances = items;
        return filter;
    }

    public async Task<Maintenance?> GetMaintenanceByIdAsync(int id)
    {
        return await _context.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<MaintenanceDetailsViewModel?> GetMaintenanceDetailsAsync(int id)
    {
        var m = await _context.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (m == null)
        {
            return null;
        }

        return new MaintenanceDetailsViewModel
        {
            Id = m.Id,
            VehicleId = m.VehicleId,
            VehiclePlate = m.Vehicle.PlateNumber,
            VehicleBrandModel = $"{m.Vehicle.Brand} {m.Vehicle.Model}",
            VehicleType = m.Vehicle.VehicleType.ToString(),
            VehicleColor = m.Vehicle.Color,
            VehicleCurrentKm = m.Vehicle.CurrentKm,
            VehicleStatus = m.Vehicle.Status,
            MaintenanceType = m.MaintenanceType,
            Description = m.Description,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            PerformedBy = m.PerformedBy,
            CreatedAt = m.CreatedAt,
            CreatedBy = m.CreatedBy,
            UpdatedAt = m.UpdatedAt
        };
    }

    public async Task<MaintenanceCreateViewModel> GetCreateViewModelAsync()
    {
        var rawVehicles = await _context.Vehicles
            .Where(v => v.Status != VehicleStatus.OutOfService)
            .OrderBy(v => v.PlateNumber)
            .Select(v => new
            {
                v.Id,
                v.PlateNumber,
                v.Brand,
                v.Model,
                v.Status
            })
            .ToListAsync();

        var vehicles = rawVehicles.Select(v => new SelectListItem
        {
            Value = v.Id.ToString(),
            Text = $"{v.PlateNumber} - {v.Brand} {v.Model} (Durum: {v.Status.ToTurkishString()})"
        }).ToList();

        return new MaintenanceCreateViewModel
        {
            StartDate = DateTime.Today,
            AvailableVehicles = vehicles
        };
    }

    public async Task<MaintenanceEditViewModel?> GetEditViewModelAsync(int id)
    {
        var m = await _context.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (m == null)
        {
            return null;
        }

        var rawVehicles = await _context.Vehicles
            .OrderBy(v => v.PlateNumber)
            .Select(v => new
            {
                v.Id,
                v.PlateNumber,
                v.Brand,
                v.Model,
                v.Status
            })
            .ToListAsync();

        var vehicles = rawVehicles.Select(v => new SelectListItem
        {
            Value = v.Id.ToString(),
            Text = $"{v.PlateNumber} - {v.Brand} {v.Model} (Durum: {v.Status.ToTurkishString()})"
        }).ToList();

        return new MaintenanceEditViewModel
        {
            Id = m.Id,
            VehicleId = m.VehicleId,
            VehiclePlate = m.Vehicle.PlateNumber,
            VehicleBrandModel = $"{m.Vehicle.Brand} {m.Vehicle.Model}",
            MaintenanceType = m.MaintenanceType,
            Description = m.Description,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            PerformedBy = m.PerformedBy,
            AvailableVehicles = vehicles
        };
    }

    public async Task<(bool Success, string Message, int MaintenanceId)> CreateMaintenanceAsync(
        MaintenanceCreateViewModel model, string adminUserId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == model.VehicleId);
        if (vehicle == null)
        {
            return (false, "Seçilen araç sistemde bulunamadı.", 0);
        }

        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
        {
            return (false, "Bakım bitiş tarihi, başlangıç tarihinden önce olamaz.", 0);
        }

        // 1. Devam eden bakım kontrolü (Aynı aracın zaten açık bir bakımı varsa)
        if (!model.EndDate.HasValue)
        {
            var ongoingMaintenance = await _context.Maintenances
                .AnyAsync(m => m.VehicleId == model.VehicleId && m.EndDate == null);

            if (ongoingMaintenance)
            {
                return (false, $"'{vehicle.PlateNumber}' plakalı aracın halihazırda devam eden bir bakım kaydı mevcuttur. Yeni bakım açmadan önce mevcut bakımı tamamlayınız.", 0);
            }
        }

        // 2. Aktif görev kontrolü (Araç şu anda seyirde/görevdeyse)
        if (!model.EndDate.HasValue && vehicle.Status == VehicleStatus.OnDuty)
        {
            return (false, $"'{vehicle.PlateNumber}' plakalı araç şu anda aktif bir görevde/seyirdedir. Görev tamamlanmadan araç bakıma alınamaz.", 0);
        }

        var maintenance = new Maintenance
        {
            VehicleId = model.VehicleId,
            MaintenanceType = model.MaintenanceType.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate?.Date,
            PerformedBy = string.IsNullOrWhiteSpace(model.PerformedBy) ? null : model.PerformedBy.Trim(),
            CreatedBy = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        var oldVehicleState = new { Status = vehicle.Status.ToString() };

        // Eğer bakım devam ediyorsa araç durumunu Maintenance yap
        if (!model.EndDate.HasValue)
        {
            vehicle.Status = VehicleStatus.Maintenance;
            vehicle.UpdatedAt = DateTime.UtcNow;
            vehicle.UpdatedBy = adminUserId;
        }

        try
        {
            _context.Maintenances.Add(maintenance);
            await _context.SaveChangesAsync();

            // Audit Log: Maintenance
            await _auditLogService.LogAsync(
                nameof(Maintenance),
                maintenance.Id,
                AuditAction.Create,
                adminUserId,
                null,
                new
                {
                    maintenance.VehicleId,
                    vehicle.PlateNumber,
                    maintenance.MaintenanceType,
                    maintenance.StartDate,
                    maintenance.EndDate,
                    maintenance.PerformedBy
                }
            );

            // Audit Log: Vehicle (durum değiştiyse)
            if (!model.EndDate.HasValue)
            {
                await _auditLogService.LogAsync(
                    nameof(Vehicle),
                    vehicle.Id,
                    AuditAction.Update,
                    adminUserId,
                    oldVehicleState,
                    new { Status = vehicle.Status.ToString() }
                );
            }

            _logger.LogInformation("Yeni bakım kaydı oluşturuldu. Id: {Id}, Vehicle: {Plate}, Type: {Type}",
                maintenance.Id, vehicle.PlateNumber, maintenance.MaintenanceType);

            return (true, $"'{vehicle.PlateNumber}' plakalı araca ait bakım kaydı başarıyla oluşturuldu.", maintenance.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bakım kaydı oluşturulurken hata oluştu.");
            return (false, "Bakım kaydı oluşturulurken bir veritabanı hatası oluştu.", 0);
        }
    }

    public async Task<(bool Success, string Message)> UpdateMaintenanceAsync(
        MaintenanceEditViewModel model, string adminUserId)
    {
        var maintenance = await _context.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == model.Id);

        if (maintenance == null)
        {
            return (false, "Güncellenmek istenen bakım kaydı bulunamadı.");
        }

        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
        {
            return (false, "Bakım bitiş tarihi, başlangıç tarihinden önce olamaz.");
        }

        var oldMaintenanceState = new
        {
            maintenance.VehicleId,
            maintenance.MaintenanceType,
            maintenance.Description,
            maintenance.StartDate,
            maintenance.EndDate,
            maintenance.PerformedBy
        };

        var oldVehicleState = new { Status = maintenance.Vehicle.Status.ToString() };

        maintenance.MaintenanceType = model.MaintenanceType.Trim();
        maintenance.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        maintenance.StartDate = model.StartDate.Date;
        maintenance.EndDate = model.EndDate?.Date;
        maintenance.PerformedBy = string.IsNullOrWhiteSpace(model.PerformedBy) ? null : model.PerformedBy.Trim();
        maintenance.UpdatedAt = DateTime.UtcNow;
        maintenance.UpdatedBy = adminUserId;

        // Bitiş tarihi girildiyse ve araç bakım durumundaysa aracı tekrar Idle (Müsait) yap
        var vehicleStatusChanged = false;
        if (maintenance.EndDate.HasValue && maintenance.Vehicle.Status == VehicleStatus.Maintenance)
        {
            var otherOngoing = await _context.Maintenances
                .AnyAsync(m => m.VehicleId == maintenance.VehicleId && m.Id != maintenance.Id && m.EndDate == null);

            if (!otherOngoing)
            {
                maintenance.Vehicle.Status = VehicleStatus.Idle;
                maintenance.Vehicle.UpdatedAt = DateTime.UtcNow;
                maintenance.Vehicle.UpdatedBy = adminUserId;
                vehicleStatusChanged = true;
            }
        }
        else if (!maintenance.EndDate.HasValue && maintenance.Vehicle.Status == VehicleStatus.Idle)
        {
            // Bitiş tarihi kaldırıldıysa araç tekrar bakıma girmiş sayılır
            maintenance.Vehicle.Status = VehicleStatus.Maintenance;
            maintenance.Vehicle.UpdatedAt = DateTime.UtcNow;
            maintenance.Vehicle.UpdatedBy = adminUserId;
            vehicleStatusChanged = true;
        }

        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Maintenance),
                maintenance.Id,
                AuditAction.Update,
                adminUserId,
                oldMaintenanceState,
                new
                {
                    maintenance.VehicleId,
                    maintenance.MaintenanceType,
                    maintenance.Description,
                    maintenance.StartDate,
                    maintenance.EndDate,
                    maintenance.PerformedBy
                }
            );

            if (vehicleStatusChanged)
            {
                await _auditLogService.LogAsync(
                    nameof(Vehicle),
                    maintenance.Vehicle.Id,
                    AuditAction.Update,
                    adminUserId,
                    oldVehicleState,
                    new { Status = maintenance.Vehicle.Status.ToString() }
                );
            }

            _logger.LogInformation("Bakım kaydı güncellendi. Id: {Id}", maintenance.Id);
            return (true, "Bakım kaydı başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bakım güncellenirken hata oluştu. Id: {Id}", model.Id);
            return (false, "Bakım güncellenirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> CompleteMaintenanceAsync(
        int id, DateTime endDate, string? performedBy, string adminUserId)
    {
        var maintenance = await _context.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (maintenance == null)
        {
            return (false, "Tamamlanmak istenen bakım kaydı bulunamadı.");
        }

        if (maintenance.EndDate.HasValue)
        {
            return (false, "Bu bakım kaydı zaten tamamlanmış durumdadır.");
        }

        if (endDate.Date < maintenance.StartDate.Date)
        {
            return (false, $"Bakım bitiş tarihi ({endDate:dd.MM.yyyy}), başlangıç tarihinden ({maintenance.StartDate:dd.MM.yyyy}) önce olamaz.");
        }

        var oldMaintenanceState = new { maintenance.EndDate, maintenance.PerformedBy };
        var oldVehicleState = new { Status = maintenance.Vehicle.Status.ToString() };

        maintenance.EndDate = endDate.Date;
        if (!string.IsNullOrWhiteSpace(performedBy))
        {
            maintenance.PerformedBy = performedBy.Trim();
        }
        maintenance.UpdatedAt = DateTime.UtcNow;
        maintenance.UpdatedBy = adminUserId;

        // Başka devam eden bakımı yoksa aracı Idle yap
        var otherOngoing = await _context.Maintenances
            .AnyAsync(m => m.VehicleId == maintenance.VehicleId && m.Id != id && m.EndDate == null);

        var vehicleStatusChanged = false;
        if (!otherOngoing && maintenance.Vehicle.Status == VehicleStatus.Maintenance)
        {
            maintenance.Vehicle.Status = VehicleStatus.Idle;
            maintenance.Vehicle.UpdatedAt = DateTime.UtcNow;
            maintenance.Vehicle.UpdatedBy = adminUserId;
            vehicleStatusChanged = true;
        }

        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Maintenance),
                maintenance.Id,
                AuditAction.Update,
                adminUserId,
                oldMaintenanceState,
                new { maintenance.EndDate, maintenance.PerformedBy }
            );

            if (vehicleStatusChanged)
            {
                await _auditLogService.LogAsync(
                    nameof(Vehicle),
                    maintenance.Vehicle.Id,
                    AuditAction.Update,
                    adminUserId,
                    oldVehicleState,
                    new { Status = maintenance.Vehicle.Status.ToString() }
                );
            }

            _logger.LogInformation("Bakım tamamlandı. Id: {Id}, Vehicle: {Plate}, EndDate: {End}",
                maintenance.Id, maintenance.Vehicle.PlateNumber, endDate);

            return (true, $"'{maintenance.Vehicle.PlateNumber}' plakalı aracın bakımı başarıyla tamamlandı ve araç 'Müsait' duruma getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bakım tamamlanırken hata oluştu. Id: {Id}", id);
            return (false, "Bakım tamamlanırken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteMaintenanceAsync(int id, string adminUserId)
    {
        var maintenance = await _context.Maintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (maintenance == null)
        {
            return (false, "Silinmek istenen bakım kaydı bulunamadı.");
        }

        var oldVehicleState = new { Status = maintenance.Vehicle.Status.ToString() };
        var wasOngoing = !maintenance.EndDate.HasValue;

        // Soft delete
        maintenance.IsDeleted = true;
        maintenance.UpdatedAt = DateTime.UtcNow;
        maintenance.UpdatedBy = adminUserId;

        // Devam eden bir bakım siliniyorsa ve aracın başka açık bakımı yoksa aracı Idle yap
        var vehicleStatusChanged = false;
        if (wasOngoing && maintenance.Vehicle.Status == VehicleStatus.Maintenance)
        {
            var otherOngoing = await _context.Maintenances
                .AnyAsync(m => m.VehicleId == maintenance.VehicleId && m.Id != id && m.EndDate == null);

            if (!otherOngoing)
            {
                maintenance.Vehicle.Status = VehicleStatus.Idle;
                maintenance.Vehicle.UpdatedAt = DateTime.UtcNow;
                maintenance.Vehicle.UpdatedBy = adminUserId;
                vehicleStatusChanged = true;
            }
        }

        try
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                nameof(Maintenance),
                maintenance.Id,
                AuditAction.Delete,
                adminUserId,
                new { maintenance.VehicleId, maintenance.MaintenanceType, maintenance.StartDate },
                new { IsDeleted = true }
            );

            if (vehicleStatusChanged)
            {
                await _auditLogService.LogAsync(
                    nameof(Vehicle),
                    maintenance.Vehicle.Id,
                    AuditAction.Update,
                    adminUserId,
                    oldVehicleState,
                    new { Status = maintenance.Vehicle.Status.ToString() }
                );
            }

            _logger.LogInformation("Bakım kaydı silindi (Soft Delete). Id: {Id}", id);
            return (true, "Bakım kaydı başarıyla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bakım kaydı silinirken hata oluştu. Id: {Id}", id);
            return (false, "Bakım kaydı silinirken bir veritabanı hatası oluştu.");
        }
    }
}
