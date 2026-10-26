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
    private readonly IConfiguration _configuration;
    private readonly ISystemDefinitionService _systemDefinitionService;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IConfiguration configuration,
        ISystemDefinitionService systemDefinitionService,
        ILogger<VehicleService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _configuration = configuration;
        _systemDefinitionService = systemDefinitionService;
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
                .ThenInclude(d => d!.User)
            .Where(a => a.VehicleId == id)
            .OrderByDescending(a => a.PlannedStartTime)
            .Take(5)
            .Select(a => new VehicleAssignmentSummaryViewModel
            {
                Id = a.Id,
                DriverFullName = a.Driver != null ? a.Driver.User.FullName : (a.IsDriverless ? "Şoförsüz Görev" : (a.ExternalDriverFullName ?? "Dış Görevli")),
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

        // Güncel Lastik Bilgisi (Faz 5-B)
        var latestTire = await _context.VehicleTires
            .Where(t => t.VehicleId == id && !t.IsDeleted)
            .OrderByDescending(t => t.InstalledDate ?? t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        VehicleTireItemViewModel? currentTireVm = null;
        if (latestTire != null)
        {
            currentTireVm = new VehicleTireItemViewModel
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
            CurrentTire = currentTireVm,
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

        // Sunucu Doğrulaması (Madde 2): Yakıt Tipi ve Araç Türü Sistem Tanımlarında aktif olmalı
        var (fuelValid, fuelErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.FuelType, model.FuelType);
        if (!fuelValid)
        {
            return (false, fuelErr!, 0);
        }

        var (vehTypeValid, vehTypeErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.VehicleType, model.VehicleType);
        if (!vehTypeValid)
        {
            return (false, vehTypeErr!, 0);
        }

        if (!string.IsNullOrWhiteSpace(model.TireSeason))
        {
            var (tireSeasonValid, tireSeasonErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.TireSeason, model.TireSeason);
            if (!tireSeasonValid)
            {
                return (false, tireSeasonErr!, 0);
            }
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

            // TEK KAYNAK İLKESİ: Yeni araç eklendiğinde VehicleDocuments tablosuna ilk muayene ve ilk sigorta kayıtlarını da oluştur
            var firstInspection = new VehicleDocument
            {
                VehicleId = vehicle.Id,
                DocumentType = VehicleDocumentType.Inspection,
                StartDate = vehicle.InspectionStartDate,
                EndDate = vehicle.InspectionExpiryDate,
                Cost = 0,
                PolicyOrStationNumber = null,
                CompanyName = null,
                Notes = "Araç ilk kaydıyla oluşturulan muayene belgesi",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            var firstInsurance = new VehicleDocument
            {
                VehicleId = vehicle.Id,
                DocumentType = VehicleDocumentType.Insurance,
                StartDate = vehicle.InsuranceStartDate,
                EndDate = vehicle.InsuranceExpiryDate,
                Cost = 0,
                PolicyOrStationNumber = null,
                CompanyName = null,
                Notes = "Araç ilk kaydıyla oluşturulan trafik sigortası belgesi",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.VehicleDocuments.AddRange(firstInspection, firstInsurance);

            // TEK KAYNAK İLKESİ (Faz 5-B): Araç oluşturulunca ilk lastik kaydı da oluşturulsun
            if (!string.IsNullOrWhiteSpace(vehicle.TireBrand))
            {
                var firstTire = new VehicleTire
                {
                    VehicleId = vehicle.Id,
                    Brand = vehicle.TireBrand.Trim(),
                    Size = null,
                    DotDate = null,
                    ProductionYear = vehicle.TireYear,
                    Season = !string.IsNullOrWhiteSpace(vehicle.TireSeason) ? vehicle.TireSeason.Trim() : "Dört Mevsim",
                    InstalledDate = null,
                    InstalledKm = null,
                    RecommendedReplaceKm = null,
                    RecommendedReplaceDate = null,
                    Cost = null,
                    Notes = "Araç ilk kaydıyla oluşturulan lastik kaydı",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.VehicleTires.Add(firstTire);
            }

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

            _logger.LogInformation("Yeni araç ve ilk belgeleri eklendi. Id: {Id}, Plaka: {PlateNumber}", vehicle.Id, vehicle.PlateNumber);
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

        // Sunucu Doğrulaması (Madde 2): Mevcut değerler hariç, yeni değer aktif bir seçenek olmalı
        var (fuelValid, fuelErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.FuelType, model.FuelType, vehicle.FuelType);
        if (!fuelValid)
        {
            return (false, fuelErr!);
        }

        var (vehTypeValid, vehTypeErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.VehicleType, model.VehicleType, vehicle.VehicleType);
        if (!vehTypeValid)
        {
            return (false, vehTypeErr!);
        }

        // Güncellenebilir alanlar (CurrentKm, Status ve Belge tarihleri iş kuralı gereği buradan güncellenmez; tek kaynak VehicleDocuments)
        vehicle.PlateNumber = cleanPlate;
        vehicle.Brand = model.Brand.Trim();
        vehicle.Model = model.Model.Trim();
        vehicle.Year = model.Year;
        vehicle.Color = model.Color.Trim();
        vehicle.VehicleType = model.VehicleType.Trim();
        vehicle.FuelType = model.FuelType.Trim();
        // NOT: model.InspectionStartDate/ExpiryDate ve model.InsuranceStartDate/ExpiryDate YOK SAYILIR (Tek kaynak: VehicleDocuments)
        // NOT: model.TireBrand, TireYear, TireSeason YOK SAYILIR (Tek kaynak: VehicleTires; senkronizasyon SyncVehicleLegacyTireColumnsAsync ile sağlanır)
        vehicle.FineNotes = string.IsNullOrWhiteSpace(model.FineNotes) ? null : model.FineNotes.Trim();
        vehicle.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        vehicle.UpdatedAt = DateTime.UtcNow;
        vehicle.UpdatedBy = userId;

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

        // Araçlar ve aktif belgeleri
        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted)
            .ToListAsync();

        var activeDocs = await _context.VehicleDocuments
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .ToListAsync();

        var activeTires = await _context.VehicleTires
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.InstalledDate ?? t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        var items = new List<DocumentExpiryNotificationItemViewModel>();

        foreach (var v in vehicles)
        {
            // Muayene
            var latestInsp = activeDocs
                .Where(d => d.VehicleId == v.Id && d.DocumentType == VehicleDocumentType.Inspection)
                .OrderByDescending(d => d.EndDate)
                .FirstOrDefault();

            var inspEndDate = latestInsp?.EndDate ?? v.InspectionExpiryDate;
            var inspDiff = (int)(inspEndDate.Date - today).TotalDays;
            var isInspAlert = inspDiff <= 30;

            // Sigorta
            var latestIns = activeDocs
                .Where(d => d.VehicleId == v.Id && d.DocumentType == VehicleDocumentType.Insurance)
                .OrderByDescending(d => d.EndDate)
                .FirstOrDefault();

            var insEndDate = latestIns?.EndDate ?? v.InsuranceExpiryDate;
            var insDiff = (int)(insEndDate.Date - today).TotalDays;
            var isInsAlert = insDiff <= 30;

            // Kasko (Yalnızca uyarı üretir, araç durumunu DocumentExpired yapmaz)
            var latestKasko = activeDocs
                .Where(d => d.VehicleId == v.Id && d.DocumentType == VehicleDocumentType.Kasko)
                .OrderByDescending(d => d.EndDate)
                .FirstOrDefault();

            int? kaskoDiff = latestKasko != null ? (int)(latestKasko.EndDate.Date - today).TotalDays : null;
            var isKaskoAlert = kaskoDiff.HasValue && kaskoDiff.Value <= 30;

            // Lastik Uyarısı (Faz 5-B: Değişim tarihine 30 gün kala ya da KM eşiği yaklaşınca/geçince)
            var currentTire = activeTires.FirstOrDefault(t => t.VehicleId == v.Id);
            bool isTireAlert = false;
            string tireAlertDesc = string.Empty;
            int? tireDiff = null;

            if (currentTire != null)
            {
                var tireKmThreshold = _configuration.GetValue<int?>("VehicleSettings:TireKmWarningThreshold") ?? 1000;
                var isTireKmAlert = currentTire.RecommendedReplaceKm.HasValue && (currentTire.RecommendedReplaceKm.Value - v.CurrentKm <= tireKmThreshold);
                var isTireDateAlert = currentTire.RecommendedReplaceDate.HasValue && ((currentTire.RecommendedReplaceDate.Value.Date - today).TotalDays <= 30);

                if (isTireKmAlert || isTireDateAlert)
                {
                    isTireAlert = true;
                    if (isTireKmAlert && isTireDateAlert)
                    {
                        var remKm = currentTire.RecommendedReplaceKm!.Value - v.CurrentKm;
                        var remDays = (int)(currentTire.RecommendedReplaceDate!.Value.Date - today).TotalDays;
                        tireDiff = remDays;
                        tireAlertDesc = remKm <= 0 ? $"Lastik KM aşıldı ({Math.Abs(remKm):N0} KM)" : $"Lastik değişimi yaklaştı ({remKm:N0} KM / {remDays}g)";
                    }
                    else if (isTireKmAlert)
                    {
                        var remKm = currentTire.RecommendedReplaceKm!.Value - v.CurrentKm;
                        tireDiff = remKm <= 0 ? -1 : (remKm / 50); // Göreceli gün eşdeğeri
                        tireAlertDesc = remKm <= 0 ? $"Lastik KM aşıldı ({Math.Abs(remKm):N0} KM)" : $"Lastik KM yaklaştı ({remKm:N0} KM kaldı)";
                    }
                    else
                    {
                        var remDays = (int)(currentTire.RecommendedReplaceDate!.Value.Date - today).TotalDays;
                        tireDiff = remDays;
                        tireAlertDesc = remDays < 0 ? $"Lastik tarihi geçti ({Math.Abs(remDays)}g)" : $"Lastik tarihine {remDays}g kaldı";
                    }
                }
            }

            if (isInspAlert || isInsAlert || isKaskoAlert || isTireAlert)
            {
                var docTypesList = new List<string>();
                var docDescList = new List<string>();
                int minDiff = int.MaxValue;

                if (isInspAlert)
                {
                    docTypesList.Add("Muayene");
                    var text = inspDiff < 0 ? $"{Math.Abs(inspDiff)}g geçti" : (inspDiff == 0 ? "Bugün" : $"{inspDiff}g kaldı");
                    docDescList.Add($"Muayene: {text}");
                    if (inspDiff < minDiff) minDiff = inspDiff;
                }

                if (isInsAlert)
                {
                    docTypesList.Add("Sigorta");
                    var text = insDiff < 0 ? $"{Math.Abs(insDiff)}g geçti" : (insDiff == 0 ? "Bugün" : $"{insDiff}g kaldı");
                    docDescList.Add($"Sigorta: {text}");
                    if (insDiff < minDiff) minDiff = insDiff;
                }

                if (isKaskoAlert && kaskoDiff.HasValue)
                {
                    docTypesList.Add("Kasko");
                    var text = kaskoDiff.Value < 0 ? $"{Math.Abs(kaskoDiff.Value)}g geçti" : (kaskoDiff.Value == 0 ? "Bugün" : $"{kaskoDiff.Value}g kaldı");
                    docDescList.Add($"Kasko: {text}");
                    if (kaskoDiff.Value < minDiff) minDiff = kaskoDiff.Value;
                }

                if (isTireAlert && !string.IsNullOrEmpty(tireAlertDesc))
                {
                    docTypesList.Add("Lastik");
                    docDescList.Add(tireAlertDesc);
                    if (tireDiff.HasValue && tireDiff.Value < minDiff) minDiff = tireDiff.Value;
                }

                items.Add(new DocumentExpiryNotificationItemViewModel
                {
                    VehicleId = v.Id,
                    PlateNumber = v.PlateNumber,
                    BrandModel = $"{v.Brand} {v.Model}",
                    Status = v.Status,
                    DocumentType = string.Join(" & ", docTypesList),
                    DocumentDescription = string.Join(" | ", docDescList),
                    DaysDiff = minDiff
                });
            }
        }

        // İş Kuralı: Belge Uyarıları - En acil olan (en çok gecikmiş / en az gün kalmış) en üstte
        items = items.OrderBy(x => x.DaysDiff).ToList();

        return new DocumentExpiryNotificationSummaryViewModel
        {
            TotalCount = items.Count,
            ExpiringVehicles = items
        };
    }

    public async Task<(bool Success, string Message)> CorrectVehicleKmAsync(int vehicleId, int correctKm, string reason, string userId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle == null)
        {
            return (false, "Araç bulunamadı.");
        }

        var oldKm = vehicle.CurrentKm;
        vehicle.CurrentKm = correctKm;
        vehicle.UpdatedAt = DateTime.UtcNow;
        vehicle.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            nameof(Vehicle),
            vehicle.Id,
            AuditAction.Update,
            userId,
            new { CurrentKm = oldKm },
            new { CurrentKm = correctKm, Reason = reason, Note = "Araç KM Düzeltmesi (Servis Katmanı)" }
        );

        return (true, $"Araç {vehicle.PlateNumber} KM değeri {oldKm:N0} -> {correctKm:N0} olarak başarıyla güncellendi.");
    }

    public async Task<VehicleDocumentsPageViewModel> GetVehicleDocumentsPageAsync(int? vehicleId, string? activeTab)
    {
        var vehicles = await _context.Vehicles
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync();

        var selectedId = vehicleId ?? vehicles.FirstOrDefault()?.Id;
        var tab = string.IsNullOrWhiteSpace(activeTab) ? "Inspection" : activeTab;

        var viewModel = new VehicleDocumentsPageViewModel
        {
            SelectedVehicleId = selectedId,
            ActiveTab = tab,
            VehicleSelectList = vehicles.Select(v => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.PlateNumber} - {v.Brand} {v.Model} ({v.StatusDisplayName})",
                Selected = v.Id == selectedId
            }).ToList()
        };

        if (selectedId.HasValue)
        {
            var vehicle = vehicles.FirstOrDefault(v => v.Id == selectedId.Value);
            viewModel.SelectedVehicle = vehicle;

            var docs = await _context.VehicleDocuments
                .Where(d => d.VehicleId == selectedId.Value && !d.IsDeleted)
                .OrderByDescending(d => d.EndDate)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            var today = DateTime.Today;

            // Muayene listesi
            var inspDocs = docs.Where(d => d.DocumentType == VehicleDocumentType.Inspection).ToList();
            var latestActiveInsp = inspDocs.FirstOrDefault(d => d.EndDate.Date >= today) ?? inspDocs.FirstOrDefault();
            viewModel.InspectionDocuments = inspDocs.Select(d => new VehicleDocumentItemViewModel
            {
                Id = d.Id,
                VehicleId = d.VehicleId,
                PlateNumber = vehicle?.PlateNumber ?? "",
                BrandModel = vehicle != null ? $"{vehicle.Brand} {vehicle.Model}" : "",
                DocumentType = d.DocumentType,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                Cost = d.Cost,
                PolicyOrStationNumber = d.PolicyOrStationNumber,
                CompanyName = d.CompanyName,
                Notes = d.Notes,
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                IsCurrentValid = latestActiveInsp != null && latestActiveInsp.Id == d.Id
            }).ToList();

            // Trafik Sigortası listesi
            var insDocs = docs.Where(d => d.DocumentType == VehicleDocumentType.Insurance).ToList();
            var latestActiveIns = insDocs.FirstOrDefault(d => d.EndDate.Date >= today) ?? insDocs.FirstOrDefault();
            viewModel.InsuranceDocuments = insDocs.Select(d => new VehicleDocumentItemViewModel
            {
                Id = d.Id,
                VehicleId = d.VehicleId,
                PlateNumber = vehicle?.PlateNumber ?? "",
                BrandModel = vehicle != null ? $"{vehicle.Brand} {vehicle.Model}" : "",
                DocumentType = d.DocumentType,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                Cost = d.Cost,
                PolicyOrStationNumber = d.PolicyOrStationNumber,
                CompanyName = d.CompanyName,
                Notes = d.Notes,
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                IsCurrentValid = latestActiveIns != null && latestActiveIns.Id == d.Id
            }).ToList();

            // Kasko listesi
            var kaskoDocs = docs.Where(d => d.DocumentType == VehicleDocumentType.Kasko).ToList();
            var latestActiveKasko = kaskoDocs.FirstOrDefault(d => d.EndDate.Date >= today) ?? kaskoDocs.FirstOrDefault();
            viewModel.KaskoDocuments = kaskoDocs.Select(d => new VehicleDocumentItemViewModel
            {
                Id = d.Id,
                VehicleId = d.VehicleId,
                PlateNumber = vehicle?.PlateNumber ?? "",
                BrandModel = vehicle != null ? $"{vehicle.Brand} {vehicle.Model}" : "",
                DocumentType = d.DocumentType,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                Cost = d.Cost,
                PolicyOrStationNumber = d.PolicyOrStationNumber,
                CompanyName = d.CompanyName,
                Notes = d.Notes,
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                IsCurrentValid = latestActiveKasko != null && latestActiveKasko.Id == d.Id
            }).ToList();

            // Lastik listesi (Faz 5-B)
            var tires = await _context.VehicleTires
                .Where(t => t.VehicleId == selectedId.Value && !t.IsDeleted)
                .OrderByDescending(t => t.InstalledDate ?? t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            var latestTire = tires.FirstOrDefault();
            var tireKmThreshold = _configuration.GetValue<int?>("VehicleSettings:TireKmWarningThreshold") ?? 1000;
            viewModel.TireDocuments = tires.Select(t => new VehicleTireItemViewModel
            {
                Id = t.Id,
                VehicleId = t.VehicleId,
                PlateNumber = vehicle?.PlateNumber ?? "",
                Brand = t.Brand,
                Size = t.Size,
                DotDate = t.DotDate,
                ProductionYear = t.ProductionYear,
                Season = t.Season,
                InstalledDate = t.InstalledDate,
                InstalledKm = t.InstalledKm,
                RecommendedReplaceKm = t.RecommendedReplaceKm,
                RecommendedReplaceDate = t.RecommendedReplaceDate,
                Cost = t.Cost,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                VehicleCurrentKm = vehicle?.CurrentKm ?? 0,
                KmWarningThreshold = tireKmThreshold,
                IsCurrentTire = latestTire != null && latestTire.Id == t.Id
            }).ToList();
        }

        return viewModel;
    }

    public async Task<(bool Success, string Message)> AddVehicleDocumentAsync(VehicleDocumentCreateViewModel model, string userId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == model.VehicleId && !v.IsDeleted);
        if (vehicle == null)
        {
            return (false, "Araç bulunamadı.");
        }

        if (model.EndDate.Date < model.StartDate.Date)
        {
            return (false, "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        var document = new VehicleDocument
        {
            VehicleId = model.VehicleId,
            DocumentType = model.DocumentType,
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            Cost = model.Cost,
            PolicyOrStationNumber = model.PolicyOrStationNumber?.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.VehicleDocuments.Add(document);
        await _context.SaveChangesAsync();

        // Senkronizasyon: Yeni eklenen kayıt en ileri geçerlilik tarihine sahipse Vehicle üzerindeki eski alanları ve DocumentExpired durumunu senkronize et
        await SyncVehicleLegacyDocumentColumnsAsync(vehicle, userId);

        await _auditLogService.LogAsync(
            nameof(VehicleDocument),
            document.Id,
            AuditAction.Create,
            userId,
            null,
            new
            {
                VehicleId = document.VehicleId,
                PlateNumber = vehicle.PlateNumber,
                DocumentType = document.DocumentType.ToString(),
                StartDate = document.StartDate.ToString("yyyy-MM-dd"),
                EndDate = document.EndDate.ToString("yyyy-MM-dd"),
                Cost = document.Cost,
                PolicyOrStationNumber = document.PolicyOrStationNumber,
                CompanyName = document.CompanyName
            }
        );

        return (true, $"{vehicle.PlateNumber} plakalı araca yeni {document.DocumentType} belgesi başarıyla eklendi.");
    }

    public async Task<(bool Success, string Message)> UpdateVehicleDocumentAsync(int id, VehicleDocumentCreateViewModel model, string userId)
    {
        var document = await _context.VehicleDocuments
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (document == null)
        {
            return (false, "Belge kaydı bulunamadı.");
        }

        if (model.EndDate.Date < model.StartDate.Date)
        {
            return (false, "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        var oldState = new
        {
            document.DocumentType,
            StartDate = document.StartDate.ToString("yyyy-MM-dd"),
            EndDate = document.EndDate.ToString("yyyy-MM-dd"),
            document.Cost,
            document.PolicyOrStationNumber,
            document.CompanyName
        };

        document.StartDate = model.StartDate.Date;
        document.EndDate = model.EndDate.Date;
        document.Cost = model.Cost;
        document.PolicyOrStationNumber = model.PolicyOrStationNumber?.Trim();
        document.CompanyName = model.CompanyName?.Trim();
        document.Notes = model.Notes?.Trim();
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        // Senkronizasyon: Araç üzerindeki eski alanları ve durumunu senkronize et
        await SyncVehicleLegacyDocumentColumnsAsync(document.Vehicle, userId);

        await _auditLogService.LogAsync(
            nameof(VehicleDocument),
            document.Id,
            AuditAction.Update,
            userId,
            oldState,
            new
            {
                document.DocumentType,
                StartDate = document.StartDate.ToString("yyyy-MM-dd"),
                EndDate = document.EndDate.ToString("yyyy-MM-dd"),
                document.Cost,
                document.PolicyOrStationNumber,
                document.CompanyName
            }
        );

        return (true, $"{document.Vehicle.PlateNumber} plakalı aracın belge kaydı güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteVehicleDocumentAsync(int id, string userId)
    {
        var document = await _context.VehicleDocuments
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (document == null)
        {
            return (false, "Belge kaydı bulunamadı.");
        }

        // İŞ KURALI: Muayene veya Trafik Sigortası için son aktif kayıt silinemez (Kasko silinebilir)
        if (document.DocumentType == VehicleDocumentType.Inspection || document.DocumentType == VehicleDocumentType.Insurance)
        {
            var otherActiveCount = await _context.VehicleDocuments
                .CountAsync(d => d.VehicleId == document.VehicleId && 
                                 d.DocumentType == document.DocumentType && 
                                 !d.IsDeleted && 
                                 d.Id != id);

            if (otherActiveCount == 0)
            {
                return (false, "Araçta en az bir geçerli kayıt kalmalı.");
            }
        }

        document.IsDeleted = true;
        document.UpdatedAt = DateTime.UtcNow;
        document.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        // Senkronizasyon: Kalan en son aktif kayıtlara göre Vehicle tablosunu senkronize et
        await SyncVehicleLegacyDocumentColumnsAsync(document.Vehicle, userId);

        await _auditLogService.LogAsync(
            nameof(VehicleDocument),
            document.Id,
            AuditAction.Delete,
            userId,
            new { document.DocumentType, EndDate = document.EndDate.ToString("yyyy-MM-dd") },
            new { Note = "Araç belgesi soft-delete ile silindi" }
        );

        return (true, $"{document.Vehicle.PlateNumber} plakalı aracın belge kaydı silindi.");
    }

    private async Task SyncVehicleLegacyDocumentColumnsAsync(Vehicle vehicle, string? userId)
    {
        var today = DateTime.Today;
        var activeDocs = await _context.VehicleDocuments
            .Where(d => d.VehicleId == vehicle.Id && !d.IsDeleted)
            .ToListAsync();

        var latestInsp = activeDocs.Where(d => d.DocumentType == VehicleDocumentType.Inspection)
            .OrderByDescending(d => d.EndDate)
            .FirstOrDefault();

        var latestIns = activeDocs.Where(d => d.DocumentType == VehicleDocumentType.Insurance)
            .OrderByDescending(d => d.EndDate)
            .FirstOrDefault();

        if (latestInsp != null)
        {
            vehicle.InspectionStartDate = latestInsp.StartDate;
            vehicle.InspectionExpiryDate = latestInsp.EndDate;
        }

        if (latestIns != null)
        {
            vehicle.InsuranceStartDate = latestIns.StartDate;
            vehicle.InsuranceExpiryDate = latestIns.EndDate;
        }

        // Eğer araç DocumentExpired ise ve her iki belge de artık geçerliyse durumu normale döndür
        if (vehicle.Status == VehicleStatus.DocumentExpired &&
            vehicle.InspectionExpiryDate.Date >= today &&
            vehicle.InsuranceExpiryDate.Date >= today)
        {
            var hasActiveMaintenance = await _context.Maintenances
                .AnyAsync(m => !m.IsDeleted && m.VehicleId == vehicle.Id && m.EndDate == null);

            vehicle.Status = hasActiveMaintenance ? VehicleStatus.Maintenance : VehicleStatus.Idle;
        }
        // Eğer araç Idle veya Maintenance ise ve belgelerden biri geçmişse DocumentExpired yap (OnDuty hariç)
        else if (vehicle.Status != VehicleStatus.OnDuty &&
                 vehicle.Status != VehicleStatus.DocumentExpired &&
                 (vehicle.InspectionExpiryDate.Date < today || vehicle.InsuranceExpiryDate.Date < today))
        {
            vehicle.Status = VehicleStatus.DocumentExpired;
        }

        vehicle.UpdatedAt = DateTime.UtcNow;
        vehicle.UpdatedBy = userId;
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> AddVehicleTireAsync(VehicleTireCreateViewModel model, string userId)
    {
        var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == model.VehicleId && !v.IsDeleted);
        if (vehicle == null)
        {
            return (false, "Araç bulunamadı.");
        }

        // Sunucu Doğrulaması (Madde 2): Lastik Mevsimi Sistem Tanımlarında aktif bir seçenek olmalı
        var (seasonValid, seasonErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.TireSeason, model.Season);
        if (!seasonValid)
        {
            return (false, seasonErr!);
        }

        // DOT Tarihinden Üretim Yılı Çıkarımı (Örn: "52/23" -> 2023)
        int? prodYear = null;
        if (!string.IsNullOrWhiteSpace(model.DotDate) && model.DotDate.Contains('/'))
        {
            var parts = model.DotDate.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out int yy))
            {
                prodYear = 2000 + yy;
            }
        }

        var tire = new VehicleTire
        {
            VehicleId = model.VehicleId,
            Brand = model.Brand.Trim(),
            Size = model.Size?.Trim(),
            DotDate = model.DotDate?.Trim(),
            ProductionYear = prodYear,
            Season = model.Season.Trim(),
            InstalledDate = model.InstalledDate,
            InstalledKm = model.InstalledKm,
            RecommendedReplaceKm = model.RecommendedReplaceKm,
            RecommendedReplaceDate = model.RecommendedReplaceDate,
            Cost = model.Cost,
            Notes = model.Notes?.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.VehicleTires.Add(tire);
        await _context.SaveChangesAsync();

        // Eski kolonları en son aktif lastikle senkron tut
        await SyncVehicleLegacyTireColumnsAsync(vehicle, userId);

        await _auditLogService.LogAsync(
            nameof(VehicleTire),
            tire.Id,
            AuditAction.Create,
            userId,
            null,
            new
            {
                tire.VehicleId,
                tire.Brand,
                tire.Size,
                tire.DotDate,
                tire.Season,
                tire.InstalledDate,
                tire.InstalledKm
            }
        );

        return (true, $"{vehicle.PlateNumber} plakalı araca yeni lastik kaydı başarıyla eklendi.");
    }

    public async Task<(bool Success, string Message)> UpdateVehicleTireAsync(int id, VehicleTireCreateViewModel model, string userId)
    {
        var tire = await _context.VehicleTires
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (tire == null)
        {
            return (false, "Lastik kaydı bulunamadı.");
        }

        // Sunucu Doğrulaması (Madde 2): Mevcut değer hariç, yeni değer aktif bir seçenek olmalı
        var (seasonValid, seasonErr) = await _systemDefinitionService.ValidateOptionAsync(aractakipsys.Common.SystemDefinitionCategories.TireSeason, model.Season, tire.Season);
        if (!seasonValid)
        {
            return (false, seasonErr!);
        }

        var oldState = new
        {
            tire.Brand,
            tire.Size,
            tire.DotDate,
            tire.Season,
            tire.InstalledDate,
            tire.InstalledKm,
            tire.RecommendedReplaceKm,
            tire.RecommendedReplaceDate,
            tire.Cost
        };

        int? prodYear = null;
        if (!string.IsNullOrWhiteSpace(model.DotDate) && model.DotDate.Contains('/'))
        {
            var parts = model.DotDate.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out int yy))
            {
                prodYear = 2000 + yy;
            }
        }

        tire.Brand = model.Brand.Trim();
        tire.Size = model.Size?.Trim();
        tire.DotDate = model.DotDate?.Trim();
        tire.ProductionYear = prodYear;
        tire.Season = model.Season.Trim();
        tire.InstalledDate = model.InstalledDate;
        tire.InstalledKm = model.InstalledKm;
        tire.RecommendedReplaceKm = model.RecommendedReplaceKm;
        tire.RecommendedReplaceDate = model.RecommendedReplaceDate;
        tire.Cost = model.Cost;
        tire.Notes = model.Notes?.Trim();
        tire.UpdatedAt = DateTime.UtcNow;
        tire.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        // Senkronizasyon
        await SyncVehicleLegacyTireColumnsAsync(tire.Vehicle, userId);

        await _auditLogService.LogAsync(
            nameof(VehicleTire),
            tire.Id,
            AuditAction.Update,
            userId,
            oldState,
            new
            {
                tire.Brand,
                tire.Size,
                tire.DotDate,
                tire.Season,
                tire.InstalledDate,
                tire.InstalledKm
            }
        );

        return (true, $"{tire.Vehicle.PlateNumber} plakalı aracın lastik kaydı güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteVehicleTireAsync(int id, string userId)
    {
        var tire = await _context.VehicleTires
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (tire == null)
        {
            return (false, "Lastik kaydı bulunamadı.");
        }

        // İŞ KURALI: Bir aracın tek aktif lastik kaydı silinemez
        var otherActiveTiresCount = await _context.VehicleTires
            .CountAsync(t => t.VehicleId == tire.VehicleId && !t.IsDeleted && t.Id != id);

        if (otherActiveTiresCount == 0)
        {
            return (false, "Araçta en az bir kayıtlı lastik bulunmalıdır; son lastik kaydı silinemez.");
        }

        tire.IsDeleted = true;
        tire.UpdatedAt = DateTime.UtcNow;
        tire.UpdatedBy = userId;

        await _context.SaveChangesAsync();

        // Kalan en güncel lastikle senkronize et
        await SyncVehicleLegacyTireColumnsAsync(tire.Vehicle, userId);

        await _auditLogService.LogAsync(
            nameof(VehicleTire),
            tire.Id,
            AuditAction.Delete,
            userId,
            new { tire.Brand, tire.Season, tire.InstalledDate },
            new { Note = "Lastik kaydı soft-delete ile silindi" }
        );

        return (true, $"{tire.Vehicle.PlateNumber} plakalı aracın lastik kaydı silindi.");
    }

    public async Task<List<VehicleTireItemViewModel>> GetAllCurrentTiresReportAsync(int? vehicleId = null)
    {
        var vehiclesQuery = _context.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted);

        if (vehicleId.HasValue && vehicleId.Value > 0)
        {
            vehiclesQuery = vehiclesQuery.Where(v => v.Id == vehicleId.Value);
        }

        var vehicles = await vehiclesQuery
            .OrderBy(v => v.PlateNumber)
            .ToListAsync();

        var tiresQuery = _context.VehicleTires
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (vehicleId.HasValue && vehicleId.Value > 0)
        {
            tiresQuery = tiresQuery.Where(t => t.VehicleId == vehicleId.Value);
        }

        var tires = await tiresQuery
            .OrderByDescending(t => t.InstalledDate ?? t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        var reportList = new List<VehicleTireItemViewModel>();
        var tireKmThreshold = _configuration.GetValue<int?>("VehicleSettings:TireKmWarningThreshold") ?? 1000;

        if (vehicleId.HasValue && vehicleId.Value > 0)
        {
            // Seçili araç için TÜM lastik kayıtları (geçmişiyle)
            var v = vehicles.FirstOrDefault();
            if (v != null)
            {
                for (int i = 0; i < tires.Count; i++)
                {
                    var t = tires[i];
                    reportList.Add(new VehicleTireItemViewModel
                    {
                        Id = t.Id,
                        VehicleId = v.Id,
                        PlateNumber = v.PlateNumber,
                        VehicleBrand = v.Brand,
                        VehicleModel = v.Model,
                        Brand = t.Brand,
                        Size = t.Size,
                        DotDate = t.DotDate,
                        ProductionYear = t.ProductionYear,
                        Season = t.Season,
                        InstalledDate = t.InstalledDate,
                        InstalledKm = t.InstalledKm,
                        RecommendedReplaceKm = t.RecommendedReplaceKm,
                        RecommendedReplaceDate = t.RecommendedReplaceDate,
                        Cost = t.Cost,
                        Notes = t.Notes,
                        CreatedAt = t.CreatedAt,
                        VehicleCurrentKm = v.CurrentKm,
                        KmWarningThreshold = tireKmThreshold,
                        IsCurrentTire = (i == 0) // En son takılan güncel
                    });
                }
            }
            return reportList;
        }

        // Tüm araçlar için her aracın güncel (en son) takılı lastiği
        foreach (var v in vehicles)
        {
            var latestTire = tires.FirstOrDefault(t => t.VehicleId == v.Id);
            if (latestTire != null)
            {
                reportList.Add(new VehicleTireItemViewModel
                {
                    Id = latestTire.Id,
                    VehicleId = v.Id,
                    PlateNumber = v.PlateNumber,
                    VehicleBrand = v.Brand,
                    VehicleModel = v.Model,
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
                    VehicleCurrentKm = v.CurrentKm,
                    KmWarningThreshold = tireKmThreshold,
                    IsCurrentTire = true
                });
            }
        }

        return reportList;
    }

    private async Task SyncVehicleLegacyTireColumnsAsync(Vehicle vehicle, string? userId)
    {
        var latestTire = await _context.VehicleTires
            .Where(t => t.VehicleId == vehicle.Id && !t.IsDeleted)
            .OrderByDescending(t => t.InstalledDate ?? t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        if (latestTire != null)
        {
            vehicle.TireBrand = latestTire.Brand;
            vehicle.TireSeason = latestTire.Season;
            vehicle.TireYear = latestTire.ProductionYear;
        }

        vehicle.UpdatedAt = DateTime.UtcNow;
        vehicle.UpdatedBy = userId;
        await _context.SaveChangesAsync();
    }
}

