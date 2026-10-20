// File: Services/BackgroundServices/VehicleDocumentExpirationWorker.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.BackgroundServices;

public class VehicleDocumentExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VehicleDocumentExpirationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public VehicleDocumentExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<VehicleDocumentExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Araç Belge Geçerlilik Takip Servisi (VehicleDocumentExpirationWorker) başlatıldı.");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExpireVehiclesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Araç belge geçerlilik kontrolü sırasında beklenmeyen bir hata oluştu.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Araç Belge Geçerlilik Takip Servisi durduruldu.");
    }

    public async Task<int> CheckAndExpireVehiclesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        var today = DateTime.Today;

        var targetVehicles = await context.Vehicles
            .Where(v => !v.IsDeleted &&
                        v.Status != VehicleStatus.OnDuty &&
                        v.Status != VehicleStatus.DocumentExpired &&
                        (v.InspectionExpiryDate.Date < today || v.InsuranceExpiryDate.Date < today))
            .ToListAsync(cancellationToken);

        if (targetVehicles.Count > 0)
        {
            _logger.LogWarning("Belge süresi dolmuş {Count} araç tespit edildi. Durumları 'DocumentExpired' yapılıyor.", targetVehicles.Count);

            foreach (var vehicle in targetVehicles)
            {
                var oldStatus = vehicle.Status;
                vehicle.Status = VehicleStatus.DocumentExpired;
                vehicle.UpdatedAt = DateTime.UtcNow;
                vehicle.UpdatedBy = null; 

                await auditLogService.LogAsync(
                    nameof(Vehicle),
                    vehicle.Id,
                    AuditAction.Update,
                    null,
                    new { Status = oldStatus.ToString() },
                    new
                    {
                        Status = VehicleStatus.DocumentExpired.ToString(),
                        Note = "Sigorta/Muayene süresi doldu, sistem tarafından otomatik pasife alındı",
                        InspectionExpiryDate = vehicle.InspectionExpiryDate.ToString("yyyy-MM-dd"),
                        InsuranceExpiryDate = vehicle.InsuranceExpiryDate.ToString("yyyy-MM-dd")
                    }
                );
            }
            _logger.LogInformation("{Count} aracın durumu başarıyla 'DocumentExpired' yapıldı.", targetVehicles.Count);
        }

        // 2. Kural: Belgeleri geçerli/yenilenmiş olduğu halde DocumentExpired kalan araçları kurtar
        var recoveredVehicles = await context.Vehicles
            .Where(v => !v.IsDeleted &&
                        v.Status == VehicleStatus.DocumentExpired &&
                        v.InspectionExpiryDate.Date >= today &&
                        v.InsuranceExpiryDate.Date >= today)
            .ToListAsync(cancellationToken);

        if (recoveredVehicles.Count > 0)
        {
            _logger.LogInformation("Belgeleri geçerli/yenilenmiş {Count} araç tespit edildi. Durumları normale döndürülüyor.", recoveredVehicles.Count);

            foreach (var vehicle in recoveredVehicles)
            {
                var hasActiveMaintenance = await context.Maintenances
                    .AnyAsync(m => !m.IsDeleted && m.VehicleId == vehicle.Id && m.EndDate == null, cancellationToken);

                var newStatus = hasActiveMaintenance ? VehicleStatus.Maintenance : VehicleStatus.Idle;

                vehicle.Status = newStatus;
                vehicle.UpdatedAt = DateTime.UtcNow;
                vehicle.UpdatedBy = null;

                await auditLogService.LogAsync(
                    nameof(Vehicle),
                    vehicle.Id,
                    AuditAction.Update,
                    null,
                    new { Status = VehicleStatus.DocumentExpired.ToString() },
                    new
                    {
                        Status = newStatus.ToString(),
                        Note = "Muayene ve Sigorta geçerli olduğu tespit edildi, durum otomatik olarak normale döndürüldü",
                        InspectionExpiryDate = vehicle.InspectionExpiryDate.ToString("yyyy-MM-dd"),
                        InsuranceExpiryDate = vehicle.InsuranceExpiryDate.ToString("yyyy-MM-dd")
                    }
                );
            }
            _logger.LogInformation("{Count} aracın durumu DocumentExpired'tan normale ({Status}) döndürüldü.", recoveredVehicles.Count, "Idle/Maintenance");
        }

        if (targetVehicles.Count > 0 || recoveredVehicles.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Belge süresi dolmuş veya düzeltilmesi gereken araç bulunamadı.");
        }

        return targetVehicles.Count + recoveredVehicles.Count;
    }
}
