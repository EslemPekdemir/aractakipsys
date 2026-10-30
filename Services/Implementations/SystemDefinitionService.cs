// File: Services/Implementations/SystemDefinitionService.cs
using aractakipsys.Common;
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.SystemDefinition;
using aractakipsys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class SystemDefinitionService : ISystemDefinitionService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SystemDefinitionService> _logger;

    public SystemDefinitionService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<SystemDefinitionService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public IReadOnlyList<SystemDefinitionCategories.CategoryMeta> GetCategories()
    {
        return SystemDefinitionCategories.All;
    }

    public async Task<SystemDefinitionPageViewModel> GetPageDataAsync(string? categoryKey, bool canManage, bool isReadOnly)
    {
        var meta = SystemDefinitionCategories.GetMeta(categoryKey ?? string.Empty) 
                   ?? SystemDefinitionCategories.All.First();

        var definitions = await _context.SystemDefinitions
            .Where(x => !x.IsDeleted && x.CategoryKey == meta.Key)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var items = new List<SystemDefinitionItemViewModel>();
        foreach (var def in definitions)
        {
            var usage = await GetUsageCountAsync(def.CategoryKey, def.Name);
            items.Add(new SystemDefinitionItemViewModel
            {
                Id = def.Id,
                CategoryKey = def.CategoryKey,
                Name = def.Name,
                DisplayOrder = def.DisplayOrder,
                IsActive = def.IsActive,
                IsSystemDefault = def.IsSystemDefault,
                UsageCount = usage,
                CreatedAt = def.CreatedAt,
                CreatedBy = def.CreatedBy,
                UpdatedAt = def.UpdatedAt
            });
        }

        return new SystemDefinitionPageViewModel
        {
            SelectedCategory = meta,
            AllCategories = SystemDefinitionCategories.All,
            Items = items,
            CanManage = canManage,
            IsReadOnly = isReadOnly
        };
    }

    public async Task<List<string>> GetActiveOptionNamesAsync(string categoryKey)
    {
        return await _context.SystemDefinitions
            .Where(x => !x.IsDeleted && x.CategoryKey == categoryKey && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, int? Id)> CreateAsync(string categoryKey, string name, int displayOrder, string? userId)
    {
        if (!SystemDefinitionCategories.IsValidCategory(categoryKey))
        {
            return (false, "Geçersiz sistem tanımı kategorisi.", null);
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (false, "Seçenek adı boş olamaz.", null);
        }

        var normalized = SystemDefinitionCategories.NormalizeName(trimmedName);

        // Tekillik Kontrolü (Aynı kategoride aynı NormalizedName olamaz)
        var exists = await _context.SystemDefinitions
            .AnyAsync(x => !x.IsDeleted && x.CategoryKey == categoryKey && x.NormalizedName == normalized);

        if (exists)
        {
            return (false, $"'{trimmedName}' seçeneği bu kategoride zaten mevcuttur.", null);
        }

        var entity = new SystemDefinition
        {
            CategoryKey = categoryKey,
            Name = trimmedName,
            NormalizedName = normalized,
            DisplayOrder = displayOrder,
            IsActive = true,
            IsSystemDefault = false,
            CreatedBy = userId ?? "System",
            CreatedAt = DateTime.UtcNow
        };

        _context.SystemDefinitions.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(SystemDefinition),
            entity.Id,
            AuditAction.Create,
            userId,
            null,
            new { entity.CategoryKey, entity.Name, entity.DisplayOrder });

        _logger.LogInformation("Yeni Sistem Tanımı Eklendi: [{Category}] {Name} (ID: {Id}) - Yapan: {User}",
            categoryKey, trimmedName, entity.Id, userId);

        return (true, $"'{trimmedName}' seçeneği başarıyla eklendi.", entity.Id);
    }

    public async Task<(bool Success, string Message)> UpdateNameAsync(int id, string newName, string? userId)
    {
        var entity = await _context.SystemDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            return (false, "Güncellenecek sistem tanımı bulunamadı.");
        }

        // Kural 1: Sistem varsayılanı seçeneklerin adı kilitlidir
        if (entity.IsSystemDefault)
        {
            return (false, "Sistem varsayılanı olan temel tanımların adı değiştirilemez; gerekirse pasife alabilirsiniz.");
        }

        var trimmedName = newName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (false, "Yeni seçenek adı boş olamaz.");
        }

        var oldName = entity.Name;
        if (string.Equals(oldName, trimmedName, StringComparison.OrdinalIgnoreCase))
        {
            return (true, $"Seçenek adı zaten '{trimmedName}'.");
        }

        var normalized = SystemDefinitionCategories.NormalizeName(trimmedName);

        // Kendi adı haricinde aynı isim var mı kontrolü
        var exists = await _context.SystemDefinitions
            .AnyAsync(x => !x.IsDeleted && x.Id != id && x.CategoryKey == entity.CategoryKey && x.NormalizedName == normalized);

        if (exists)
        {
            return (false, $"'{trimmedName}' seçeneği bu kategoride zaten mevcuttur.");
        }

        // Kaskat Güncelleme (Cascade Rename): İlgili tablolardaki eski adlar yeni adla tek transaction içinde güncellenir
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            entity.Name = trimmedName;
            entity.NormalizedName = normalized;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId ?? "System";

            int cascadedCount = 0;

            switch (entity.CategoryKey)
            {
                case SystemDefinitionCategories.TireSeason:
                    var tires = await _context.VehicleTires
                        .Where(t => !t.IsDeleted && t.Season == oldName)
                        .ToListAsync();
                    foreach (var t in tires)
                    {
                        t.Season = trimmedName;
                    }
                    cascadedCount += tires.Count;

                    var vehiclesWithSeason = await _context.Vehicles
                        .Where(v => !v.IsDeleted && v.TireSeason == oldName)
                        .ToListAsync();
                    foreach (var v in vehiclesWithSeason)
                    {
                        v.TireSeason = trimmedName;
                    }
                    cascadedCount += vehiclesWithSeason.Count;
                    break;

                case SystemDefinitionCategories.FuelType:
                    var vehiclesWithFuel = await _context.Vehicles
                        .Where(v => !v.IsDeleted && v.FuelType == oldName)
                        .ToListAsync();
                    foreach (var v in vehiclesWithFuel)
                    {
                        v.FuelType = trimmedName;
                    }
                    cascadedCount += vehiclesWithFuel.Count;
                    break;

                case SystemDefinitionCategories.VehicleType:
                    var vehiclesWithType = await _context.Vehicles
                        .Where(v => !v.IsDeleted && v.VehicleType == oldName)
                        .ToListAsync();
                    foreach (var v in vehiclesWithType)
                    {
                        v.VehicleType = trimmedName;
                    }
                    cascadedCount += vehiclesWithType.Count;
                    break;

                case SystemDefinitionCategories.MaintenanceType:
                    var maintenances = await _context.Maintenances
                        .Where(m => !m.IsDeleted && m.MaintenanceType == oldName)
                        .ToListAsync();
                    foreach (var m in maintenances)
                    {
                        m.MaintenanceType = trimmedName;
                    }
                    cascadedCount += maintenances.Count;
                    break;

                case SystemDefinitionCategories.LicenseClass:
                    var licenses = await _context.DriverLicenses
                        .Where(l => !l.IsDeleted && l.LicenseClass == oldName)
                        .ToListAsync();
                    foreach (var l in licenses)
                    {
                        l.LicenseClass = trimmedName;
                    }
                    cascadedCount += licenses.Count;

                    var drivers = await _context.Drivers
                        .Where(d => !d.IsDeleted && d.LicenseClass != null && d.LicenseClass.Contains(oldName))
                        .ToListAsync();
                    foreach (var d in drivers)
                    {
                        var parts = d.LicenseClass!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var updatedParts = parts.Select(p => string.Equals(p, oldName, StringComparison.OrdinalIgnoreCase) ? trimmedName : p);
                        d.LicenseClass = string.Join(", ", updatedParts);
                    }
                    break;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLogService.LogAsync(
                nameof(SystemDefinition),
                entity.Id,
                AuditAction.Update,
                userId,
                new { entity.CategoryKey, Name = oldName },
                new { entity.CategoryKey, Name = trimmedName, CascadedRecords = cascadedCount });

            _logger.LogInformation("Sistem Tanımı Adı Değiştirildi (Cascade): [{Category}] ID:{Id} '{Old}' -> '{New}' - Etkilenen Kayıt: {Count} - Yapan: {User}",
                entity.CategoryKey, entity.Id, oldName, trimmedName, cascadedCount, userId);

            var message = cascadedCount > 0
                ? $"'{oldName}' seçeneği '{trimmedName}' olarak güncellendi ve kullanıldığı {cascadedCount} adet kayda başarıyla uygulandı."
                : $"Seçenek adı başarıyla '{trimmedName}' olarak güncellendi.";

            return (true, message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Sistem Tanımı Adı Değiştirilirken Hata Oluştu (Cascade Rollback): [{Category}] ID:{Id} '{Old}' -> '{New}'",
                entity.CategoryKey, entity.Id, oldName, trimmedName);
            return (false, "Seçenek adı güncellenirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleStatusAsync(int id, string? userId)
    {
        var entity = await _context.SystemDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            return (false, "İşlem yapılacak sistem tanımı bulunamadı.");
        }

        // Kural 4: Bir kategorideki SON aktif seçenek pasife alınamaz
        if (entity.IsActive)
        {
            var activeCount = await _context.SystemDefinitions
                .CountAsync(x => !x.IsDeleted && x.CategoryKey == entity.CategoryKey && x.IsActive);

            if (activeCount <= 1)
            {
                return (false, "Bu kategorideki son aktif seçenek pasife alınamaz. Sistemde en az bir aktif seçenek bulunmalıdır.");
            }
        }

        var oldStatus = entity.IsActive ? "Active" : "Passive";
        entity.IsActive = !entity.IsActive;
        var newStatus = entity.IsActive ? "Active" : "Passive";
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId ?? "System";

        await _context.SaveChangesAsync();

        var statusText = entity.IsActive ? "aktif edildi" : "pasife alındı";

        await _auditLogService.LogAsync(
            nameof(SystemDefinition),
            entity.Id,
            AuditAction.Update,
            userId,
            new { entity.CategoryKey, entity.Name, Status = oldStatus },
            new { entity.CategoryKey, entity.Name, Status = newStatus });

        _logger.LogInformation("Sistem Tanımı Durumu Değiştirildi: [{Category}] '{Name}' (ID: {Id}) -> {Status} - Yapan: {User}",
            entity.CategoryKey, entity.Name, entity.Id, statusText, userId);

        return (true, $"'{entity.Name}' seçeneği başarıyla {statusText}.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, string? userId)
    {
        var entity = await _context.SystemDefinitions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
        {
            return (false, "Silinecek sistem tanımı bulunamadı.");
        }

        // Kural 1: Sistem varsayılanı seçenekler silinemez
        if (entity.IsSystemDefault)
        {
            return (false, "Sistem varsayılanı olan temel tanımlar silinemez; gerekirse pasife alabilirsiniz.");
        }

        // Kural 2: Kullanımda olan seçenekler silinemez
        var usageCount = await GetUsageCountAsync(entity.CategoryKey, entity.Name);
        if (usageCount > 0)
        {
            return (false, $"Bu seçenek şu anda {usageCount} adet kayıtta kullanıldığı için silinemez; yalnızca pasife alabilirsiniz.");
        }

        // Kural 3: Bir kategorideki son aktif seçenek silinemez
        if (entity.IsActive)
        {
            var activeCount = await _context.SystemDefinitions
                .CountAsync(x => !x.IsDeleted && x.CategoryKey == entity.CategoryKey && x.IsActive);

            if (activeCount <= 1)
            {
                return (false, "Bu kategorideki son aktif seçenek silinemez. Sistemde en az bir aktif seçenek bulunmalıdır.");
            }
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId ?? "System";

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(SystemDefinition),
            entity.Id,
            AuditAction.Delete,
            userId,
            new { entity.CategoryKey, entity.Name, IsDeleted = false },
            new { entity.CategoryKey, entity.Name, IsDeleted = true });

        _logger.LogInformation("Sistem Tanımı Silindi: [{Category}] '{Name}' (ID: {Id}) - Yapan: {User}",
            entity.CategoryKey, entity.Name, entity.Id, userId);

        return (true, $"'{entity.Name}' seçeneği başarıyla silindi.");
    }

    public async Task<int> GetUsageCountAsync(string categoryKey, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return 0;

        var cleanName = name.Trim();

        return categoryKey switch
        {
            SystemDefinitionCategories.TireSeason =>
                await _context.VehicleTires.CountAsync(t => !t.IsDeleted && t.Season == cleanName) +
                await _context.Vehicles.CountAsync(v => !v.IsDeleted && v.TireSeason == cleanName),

            SystemDefinitionCategories.FuelType =>
                await _context.Vehicles.CountAsync(v => !v.IsDeleted && v.FuelType == cleanName),

            SystemDefinitionCategories.VehicleType =>
                await _context.Vehicles.CountAsync(v => !v.IsDeleted && v.VehicleType == cleanName),

            SystemDefinitionCategories.MaintenanceType =>
                await _context.Maintenances.CountAsync(m => !m.IsDeleted && m.MaintenanceType == cleanName),

            SystemDefinitionCategories.LicenseClass =>
                await _context.DriverLicenses.CountAsync(l => !l.IsDeleted && l.LicenseClass == cleanName) +
                await _context.Drivers.CountAsync(d => !d.IsDeleted && d.LicenseClass != null && d.LicenseClass.Contains(cleanName)),

            _ => 0
        };
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateOptionAsync(string categoryKey, string? value, string? existingValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, "Lütfen geçerli bir seçenek belirleyiniz.");
        }

        var cleanValue = value.Trim();

        // Kural 2: Düzenlemede kaydın mevcut (eski) değeri pasif olsa dahi geçerli kabul edilir
        if (!string.IsNullOrWhiteSpace(existingValue) && 
            string.Equals(cleanValue, existingValue.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return (true, null);
        }

        var normalized = SystemDefinitionCategories.NormalizeName(cleanValue);

        var isActiveOption = await _context.SystemDefinitions
            .AnyAsync(x => !x.IsDeleted && x.CategoryKey == categoryKey && x.NormalizedName == normalized && x.IsActive);

        if (!isActiveOption)
        {
            var meta = SystemDefinitionCategories.GetMeta(categoryKey);
            var catName = meta?.DisplayName ?? categoryKey;
            return (false, $"'{cleanValue}' geçerli veya aktif bir {catName} seçeneği değildir. Lütfen listeden aktif bir seçenek seçiniz.");
        }

        return (true, null);
    }
}
