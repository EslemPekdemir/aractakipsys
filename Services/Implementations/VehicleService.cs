// File: Services/Implementations/VehicleService.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Vehicle;
using aractakipsys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<VehicleService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<VehicleFilterViewModel> GetVehiclesPagedAsync(VehicleFilterViewModel filter)
    {
        var query = _context.Vehicles.AsNoTracking().AsQueryable();

        // 1. Filtreler
        if (filter.Status.HasValue)
        {
            query = query.Where(v => v.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.VehicleType))
        {
            query = query.Where(v => v.VehicleType == filter.VehicleType);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(v =>
                v.PlateNumber.ToLower().Contains(search) ||
                v.Brand.ToLower().Contains(search) ||
                v.Model.ToLower().Contains(search));
        }

        // 2. Toplam Kayıt ve Sayfalama
        filter.TotalCount = await query.CountAsync();
        filter.PageNumber = Math.Max(1, filter.PageNumber);
        filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new VehicleListItemViewModel
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                Brand = v.Brand,
                Model = v.Model,
                Year = v.Year,
                Color = v.Color,
                VehicleType = v.VehicleType,
                FuelType = v.FuelType,
                CurrentKm = v.CurrentKm,
                Status = v.Status
            })
            .ToListAsync();

        filter.Vehicles = items;

        var defaultTypes = new[] { "Sedan", "Hatchback", "SUV", "Crossover", "Pikap", "Minibüs", "Panelvan", "Kamyonet", "Kamyon", "Otobüs", "Diğer" };
        var dbTypes = await _context.Vehicles.Select(v => v.VehicleType).Distinct().ToListAsync();
        filter.AvailableVehicleTypes = defaultTypes.Union(dbTypes.Where(t => !string.IsNullOrWhiteSpace(t))).Distinct().OrderBy(t => t).ToList();

        return filter;
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<VehicleEditViewModel?> GetVehicleEditModelAsync(int id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            return null;
        }

        return new VehicleEditViewModel
        {
            Id = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Color = vehicle.Color,
            VehicleType = vehicle.VehicleType,
            FuelType = vehicle.FuelType,
            CurrentKm = vehicle.CurrentKm,
            Status = vehicle.Status,
            Notes = vehicle.Notes,
            InspectionStartDate = vehicle.InspectionStartDate,
            InspectionExpiryDate = vehicle.InspectionExpiryDate,
            InsuranceStartDate = vehicle.InsuranceStartDate,
            InsuranceExpiryDate = vehicle.InsuranceExpiryDate,
            TireBrand = vehicle.TireBrand,
            TireYear = vehicle.TireYear,
            TireSeason = vehicle.TireSeason,
            FineNotes = vehicle.FineNotes
        };
    }

    public async Task<VehicleDetailsViewModel?> GetVehicleDetailsAsync(int id)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            return null;
        }

        // Son 5 Görevlendirme
        var recentAssignments = await _context.Assignments
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .Where(a => a.VehicleId == id)
            .OrderByDescending(a => a.PlannedStartTime)
            .Take(5)
            .Select(a => new VehicleAssignmentSummaryViewModel
            {
                Id = a.Id,
                DriverFullName = a.Driver.User.FullName,
                Purpose = a.Purpose,
                PlannedStartTime = a.PlannedStartTime,
                PlannedEndTime = a.PlannedEndTime,
                StartKm = a.StartKm,
                EndKm = a.EndKm,
                Status = a.Status
            })
            .ToListAsync();

        // Bakım Geçmişi
        var maintenanceHistory = await _context.Maintenances
            .Where(m => m.VehicleId == id)
            .OrderByDescending(m => m.StartDate)
            .Select(m => new VehicleMaintenanceSummaryViewModel
            {
                Id = m.Id,
                MaintenanceType = m.MaintenanceType,
                Description = m.Description,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                PerformedBy = m.PerformedBy
            })
            .ToListAsync();

        return new VehicleDetailsViewModel
        {
            Id = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Color = vehicle.Color,
            VehicleType = vehicle.VehicleType,
            FuelType = vehicle.FuelType,
            CurrentKm = vehicle.CurrentKm,
            Status = vehicle.Status,
            Notes = vehicle.Notes,
            InspectionStartDate = vehicle.InspectionStartDate,
            InspectionExpiryDate = vehicle.InspectionExpiryDate,
            InsuranceStartDate = vehicle.InsuranceStartDate,
            InsuranceExpiryDate = vehicle.InsuranceExpiryDate,
            TireBrand = vehicle.TireBrand,
            TireYear = vehicle.TireYear,
            TireSeason = vehicle.TireSeason,
            FineNotes = vehicle.FineNotes,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
            RecentAssignments = recentAssignments,
            MaintenanceHistory = maintenanceHistory
        };
    }

    public async Task<(bool Success, string Message, int VehicleId)> CreateVehicleAsync(VehicleCreateViewModel model, string userId)
    {
        var cleanPlate = model.PlateNumber.Trim().ToUpper();

        // Plaka tekillik kontrolü (Silinmiş araçlar dahil - IgnoreQueryFilters)
        if (await _context.Vehicles.IgnoreQueryFilters().AnyAsync(v => v.PlateNumber == cleanPlate))
        {
            return (false, "Bu plaka sistemde daha önce kayıtlı (veya silinmiş) bir araca aittir, kullanılamaz.", 0);
        }

        var vehicle = new Vehicle
        {
            PlateNumber = cleanPlate,
            Brand = model.Brand.Trim(),
            Model = model.Model.Trim(),
            Year = model.Year,
            Color = model.Color.Trim(),
            VehicleType = model.VehicleType.Trim(),
            FuelType = model.FuelType.Trim(),
            CurrentKm = model.CurrentKm,
            Status = VehicleStatus.Idle,
            InspectionStartDate = model.InspectionStartDate,
            InspectionExpiryDate = model.InspectionExpiryDate,
            InsuranceStartDate = model.InsuranceStartDate,
            InsuranceExpiryDate = model.InsuranceExpiryDate,
            TireBrand = string.IsNullOrWhiteSpace(model.TireBrand) ? null : model.TireBrand.Trim(),
            TireYear = model.TireYear,
            TireSeason = string.IsNullOrWhiteSpace(model.TireSeason) ? null : model.TireSeason.Trim(),
            FineNotes = string.IsNullOrWhiteSpace(model.FineNotes) ? null : model.FineNotes.Trim(),
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Vehicle),
                vehicle.Id,
                AuditAction.Create,
                userId,
                null,
                new { vehicle.PlateNumber, vehicle.Brand, vehicle.Model, vehicle.CurrentKm, Status = vehicle.Status.ToString() }
            );

            _logger.LogInformation("Yeni araç eklendi. Id: {Id}, Plaka: {PlateNumber}", vehicle.Id, vehicle.PlateNumber);
            return (true, $"'{vehicle.PlateNumber}' plakalı araç başarıyla sisteme eklendi.", vehicle.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç eklenirken hata oluştu.");
            return (false, "Araç kaydedilirken bir veritabanı hatası oluştu.", 0);
        }
    }

    public async Task<(bool Success, string Message)> UpdateVehicleAsync(VehicleEditViewModel model, string userId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == model.Id);
        if (vehicle == null)
        {
            return (false, "Güncellenmek istenen araç bulunamadı.");
        }

        var cleanPlate = model.PlateNumber.Trim().ToUpper();

        // Başka bir araçla plaka çakışması kontrolü (Silinmiş araçlar dahil - IgnoreQueryFilters)
        if (await _context.Vehicles.IgnoreQueryFilters().AnyAsync(v => v.PlateNumber == cleanPlate && v.Id != model.Id))
        {
            return (false, "Bu plaka sistemde daha önce kayıtlı (veya silinmiş) bir araca aittir, kullanılamaz.");
        }

        var oldValues = new
        {
            vehicle.PlateNumber,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            vehicle.Color,
            vehicle.VehicleType,
            FuelType = vehicle.FuelType,
            vehicle.Notes
        };

        // Güncellenebilir alanlar (CurrentKm ve Status iş kuralı gereği buradan güncellenmez)
        vehicle.PlateNumber = cleanPlate;
        vehicle.Brand = model.Brand.Trim();
        vehicle.Model = model.Model.Trim();
        vehicle.Year = model.Year;
        vehicle.Color = model.Color.Trim();
        vehicle.VehicleType = model.VehicleType.Trim();
        vehicle.FuelType = model.FuelType.Trim();
        vehicle.InspectionStartDate = model.InspectionStartDate;
        vehicle.InspectionExpiryDate = model.InspectionExpiryDate;
        vehicle.InsuranceStartDate = model.InsuranceStartDate;
        vehicle.InsuranceExpiryDate = model.InsuranceExpiryDate;
        vehicle.TireBrand = string.IsNullOrWhiteSpace(model.TireBrand) ? null : model.TireBrand.Trim();
        vehicle.TireYear = model.TireYear;
        vehicle.TireSeason = string.IsNullOrWhiteSpace(model.TireSeason) ? null : model.TireSeason.Trim();
        vehicle.FineNotes = string.IsNullOrWhiteSpace(model.FineNotes) ? null : model.FineNotes.Trim();
        vehicle.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        vehicle.UpdatedAt = DateTime.UtcNow;
        vehicle.UpdatedBy = userId;

        // Belgesi yenilenen aracı DocumentExpired durumundan normale döndür
        if (vehicle.Status == VehicleStatus.DocumentExpired &&
            model.InspectionExpiryDate.Date >= DateTime.Today &&
            model.InsuranceExpiryDate.Date >= DateTime.Today)
        {
            var hasActiveMaintenance = await _context.Maintenances
                .AnyAsync(m => !m.IsDeleted && m.VehicleId == vehicle.Id && m.EndDate == null);
            vehicle.Status = hasActiveMaintenance ? VehicleStatus.Maintenance : VehicleStatus.Idle;
        }

        try
        {
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Vehicle),
                vehicle.Id,
                AuditAction.Update,
                userId,
                oldValues,
                new
                {
                    vehicle.PlateNumber,
                    vehicle.Brand,
                    vehicle.Model,
                    vehicle.Year,
                    vehicle.Color,
                    vehicle.VehicleType,
                    FuelType = vehicle.FuelType,
                    vehicle.Notes
                }
            );

            _logger.LogInformation("Araç güncellendi. Id: {Id}, Plaka: {PlateNumber}", vehicle.Id, vehicle.PlateNumber);
            return (true, $"'{vehicle.PlateNumber}' plakalı araç bilgileri güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç güncellenirken hata oluştu. Id: {Id}", model.Id);
            return (false, "Araç güncellenirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteVehicleAsync(int id, string userId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (vehicle == null)
        {
            return (false, "Silinmek istenen araç bulunamadı.");
        }

        // 1. İş Kuralı: Aktif/Planlı görevi olan araç silinemez (ENDPOINT-SPESIFIKASYONU 1.27)
        var hasActiveAssignments = await _context.Assignments.AnyAsync(a =>
            a.VehicleId == id &&
            (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress));

        if (hasActiveAssignments)
        {
            return (false, $"'{vehicle.PlateNumber}' plakalı araca ait planlanan veya devam eden aktif görev(ler) bulunmaktadır. Görevler tamamlanmadan veya iptal edilmeden araç silinemez.");
        }

        // 2. İş Kuralı: Devam eden bakımı olan araç silinemez
        var hasActiveMaintenance = await _context.Maintenances.AnyAsync(m =>
            m.VehicleId == id && m.EndDate == null);

        if (hasActiveMaintenance)
        {
            return (false, $"'{vehicle.PlateNumber}' plakalı aracın devam eden bir bakım kaydı bulunmaktadır. Bakım süreci tamamlanmadan araç silinemez.");
        }

        try
        {
            // Soft delete
            vehicle.IsDeleted = true;
            vehicle.UpdatedAt = DateTime.UtcNow;
            vehicle.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(Vehicle),
                vehicle.Id,
                AuditAction.Delete,
                userId,
                new { vehicle.PlateNumber, vehicle.Brand, vehicle.Model, Status = vehicle.Status.ToString() },
                new { IsDeleted = true }
            );

            _logger.LogInformation("Araç silindi (Soft Delete). Id: {Id}, Plaka: {PlateNumber}", vehicle.Id, vehicle.PlateNumber);
            return (true, $"'{vehicle.PlateNumber}' plakalı araç başarıyla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç silinirken hata oluştu. Id: {Id}", id);
            return (false, "Araç silinirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<DocumentExpiryNotificationSummaryViewModel> GetExpiringDocumentsSummaryAsync()
    {
        var today = DateTime.Today;
        var threshold = today.AddDays(30);

        var targetVehicles = await _context.Vehicles
            .Where(v => !v.IsDeleted &&
                        (v.Status == VehicleStatus.DocumentExpired ||
                         v.InspectionExpiryDate.Date < today ||
                         v.InsuranceExpiryDate.Date < today ||
                         (v.InspectionExpiryDate.Date >= today && v.InspectionExpiryDate.Date <= threshold) ||
                         (v.InsuranceExpiryDate.Date >= today && v.InsuranceExpiryDate.Date <= threshold)))
            .OrderBy(v => (v.Status == VehicleStatus.DocumentExpired || v.InspectionExpiryDate.Date < today || v.InsuranceExpiryDate.Date < today) ? 0 : 1)
            .ThenBy(v => v.InspectionExpiryDate < v.InsuranceExpiryDate ? v.InspectionExpiryDate : v.InsuranceExpiryDate)
            .ToListAsync();

        var items = new List<DocumentExpiryNotificationItemViewModel>();

        foreach (var v in targetVehicles)
        {
            var inspDiff = (int)(v.InspectionExpiryDate.Date - today).TotalDays;
            var insDiff = (int)(v.InsuranceExpiryDate.Date - today).TotalDays;

            var isInspAlert = inspDiff <= 30;
            var isInsAlert = insDiff <= 30;

            string docType;
            string docDesc;
            int daysDiff;

            if (isInspAlert && isInsAlert)
            {
                docType = "Muayene & Sigorta";
                var inspText = inspDiff < 0 ? $"{Math.Abs(inspDiff)}g geçti" : (inspDiff == 0 ? "Bugün" : $"{inspDiff}g kaldı");
                var insText = insDiff < 0 ? $"{Math.Abs(insDiff)}g geçti" : (insDiff == 0 ? "Bugün" : $"{insDiff}g kaldı");
                docDesc = $"Muayene: {inspText} | Sigorta: {insText}";
                daysDiff = Math.Min(inspDiff, insDiff);
            }
            else if (isInspAlert)
            {
                docType = "Fenni Muayene";
                docDesc = inspDiff < 0 ? $"Muayene {Math.Abs(inspDiff)} gün önce doldu" : (inspDiff == 0 ? "Muayene bugün doluyor" : $"Muayeneye {inspDiff} gün kaldı");
                daysDiff = inspDiff;
            }
            else
            {
                docType = "Trafik Sigortası";
                docDesc = insDiff < 0 ? $"Sigorta {Math.Abs(insDiff)} gün önce doldu" : (insDiff == 0 ? "Sigorta bugün doluyor" : $"Sigortaya {insDiff} gün kaldı");
                daysDiff = insDiff;
            }

            items.Add(new DocumentExpiryNotificationItemViewModel
            {
                VehicleId = v.Id,
                PlateNumber = v.PlateNumber,
                BrandModel = $"{v.Brand} {v.Model}",
                Status = v.Status,
                DocumentType = docType,
                DocumentDescription = docDesc,
                DaysDiff = daysDiff
            });
        }

        // İş Kuralı: Belge Uyarıları - En acil olan (en çok gecikmiş / en az gün kalmış) en üstte
        items = items.OrderBy(x => x.DaysDiff).ToList();

        return new DocumentExpiryNotificationSummaryViewModel
        {
            TotalCount = items.Count,
            ExpiringVehicles = items
        };
    }
}
