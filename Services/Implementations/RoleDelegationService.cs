// File: Services/Implementations/RoleDelegationService.cs
using aractakipsys.Common;
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.RoleDelegation;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;

namespace aractakipsys.Services.Implementations;

public class RoleDelegationService : IRoleDelegationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RoleDelegationService> _logger;

    public const string CachePrefix = "delegation_user_";

    public RoleDelegationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService,
        IMemoryCache memoryCache,
        ILogger<RoleDelegationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<RoleDelegationListViewModel> GetDelegationListAsync(string currentUserId, bool isElevatedAdmin)
    {
        var currentUser = await _userManager.FindByIdAsync(currentUserId);
        var isSef = currentUser != null && await _userManager.IsInRoleAsync(currentUser, AppRoles.Kullanici);

        var query = _context.RoleDelegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.RevokedByUser)
            .AsNoTracking()
            .AsQueryable();

        // Eğer saf Admin / DaireBaskani / GenelSekreter değilse (örneğin normal Şef ise),
        // sadece kendisinin devrettiği ya da kendisine devredilen kayıtları görür.
        if (!isElevatedAdmin)
        {
            query = query.Where(d => d.DelegatorUserId == currentUserId || d.DelegateeUserId == currentUserId);
        }

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new RoleDelegationItemViewModel
            {
                Id = d.Id,
                DelegatorUserId = d.DelegatorUserId,
                DelegatorFullName = d.DelegatorUser != null ? d.DelegatorUser.FullName : d.DelegatorUserId,
                DelegatorRole = "Kullanici",
                DelegateeUserId = d.DelegateeUserId,
                DelegateeFullName = d.DelegateeUser != null ? d.DelegateeUser.FullName : d.DelegateeUserId,
                OriginalDelegateeRole = d.OriginalDelegateeRole,
                DelegatedRole = d.DelegatedRole,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                ActualRevokedAt = d.ActualRevokedAt,
                RevokedByFullName = d.RevokedByUser != null ? d.RevokedByUser.FullName : null,
                RevokeReason = d.RevokeReason,
                Reason = d.Reason
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var activeToMe = items.FirstOrDefault(x => x.DelegateeUserId == currentUserId && x.ActualRevokedAt == null && x.StartDate <= now && now <= x.EndDate);
        var activeByMe = items.FirstOrDefault(x => x.DelegatorUserId == currentUserId && x.ActualRevokedAt == null && x.StartDate <= now && now <= x.EndDate);

        return new RoleDelegationListViewModel
        {
            IsElevatedAdmin = isElevatedAdmin,
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUser?.FullName ?? "",
            Delegations = items,
            ActiveDelegatedToMe = activeToMe,
            ActiveDelegatedByMe = activeByMe
        };
    }

    public async Task<RoleDelegationCreateModalViewModel> GetCreateModalViewModelAsync(string currentUserId, bool isElevatedAdmin)
    {
        var currentUser = await _userManager.FindByIdAsync(currentUserId);
        var viewModel = new RoleDelegationCreateModalViewModel
        {
            IsElevatedAdmin = isElevatedAdmin,
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUser?.FullName ?? ""
        };

        // Devreden Adayları:
        // Eğer isElevatedAdmin (Admin/DaireBaskani) ise tüm aktif Şefleri seçebilir.
        // Şef ise sadece kendisi devredebilir.
        if (isElevatedAdmin)
        {
            var sefUsers = await (from u in _context.Users
                                  join ur in _context.UserRoles on u.Id equals ur.UserId
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  where r.Name == AppRoles.Kullanici && u.IsActive
                                  orderby u.FullName
                                  select new SelectListItem
                                  {
                                      Value = u.Id,
                                      Text = $"{u.FullName} ({u.Email})"
                                  }).ToListAsync();
            viewModel.EligibleDelegators = sefUsers;
        }

        // Devralan Adayları:
        // Yalnızca aktif ve silinmemiş Şef (Kullanici) veya Şoför (Driver)
        // Admin, DaireBaskani, GenelSekreter ASLA devralamaz.
        var eligibleUsers = await (from u in _context.Users
                                   join ur in _context.UserRoles on u.Id equals ur.UserId
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   where (r.Name == AppRoles.Kullanici || r.Name == AppRoles.Driver) && u.IsActive
                                   orderby r.Name, u.FullName
                                   select new
                                   {
                                       u.Id,
                                       u.FullName,
                                       u.Email,
                                       RoleName = r.Name
                                   }).ToListAsync();

        viewModel.EligibleDelegatees = eligibleUsers
            .Where(x => isElevatedAdmin || x.Id != currentUserId) // Şef kendisini listede görmesin
            .Select(x => new SelectListItem
            {
                Value = x.Id,
                Text = $"{x.FullName} ({(x.RoleName == AppRoles.Driver ? "Şoför" : "Ulaşım Şefi")}) - {x.Email}"
            }).ToList();

        return viewModel;
    }

    public async Task<(bool Success, string Message, int? DelegationId, List<string>? ConflictingAssignments)> CreateDelegationAsync(
        RoleDelegationCreateInputModel input, 
        string actorUserId, 
        bool isElevatedAdmin)
    {
        // 1. Devreden Belirleme (DÜZELTME 4)
        string delegatorId;
        if (isElevatedAdmin && !string.IsNullOrWhiteSpace(input.DelegatorUserId))
        {
            delegatorId = input.DelegatorUserId.Trim();
        }
        else
        {
            delegatorId = actorUserId;
        }

        var delegatorUser = await _userManager.FindByIdAsync(delegatorId);
        if (delegatorUser == null || !delegatorUser.IsActive)
        {
            return (false, "Görevi devreden kullanıcı bulunamadı veya hesabı pasif.", null, null);
        }

        var delegatorRoles = await _userManager.GetRolesAsync(delegatorUser);
        if (!delegatorRoles.Contains(AppRoles.Kullanici))
        {
            return (false, "Yalnızca 'Ulaşım Şefi' (Kullanici) rolündeki personel görevini devredebilir.", null, null);
        }

        // Devreden kişi Admin/DaireBaskani/GenelSekreter olamaz
        if (delegatorRoles.Contains(AppRoles.Admin) || delegatorRoles.Contains(AppRoles.DaireBaskani) || delegatorRoles.Contains(AppRoles.GenelSekreter))
        {
            return (false, "Sistem yöneticileri (Admin, Daire Başkanı, Genel Sekreter) yetki devri yapamaz.", null, null);
        }

        // 2. Devralan Belirleme (DÜZELTME 5)
        var delegateeUser = await _userManager.FindByIdAsync(input.DelegateeUserId);
        if (delegateeUser == null || !delegateeUser.IsActive)
        {
            return (false, "Görevi devralacak personel bulunamadı veya hesabı pasif.", null, null);
        }

        // Kendine devir engeli
        if (delegatorId == delegateeUser.Id)
        {
            return (false, "Kullanıcı kendi görevini yine kendisine devredemez.", null, null);
        }

        var delegateeRoles = await _userManager.GetRolesAsync(delegateeUser);
        if (delegateeRoles.Contains(AppRoles.Admin) || delegateeRoles.Contains(AppRoles.DaireBaskani) || delegateeRoles.Contains(AppRoles.GenelSekreter))
        {
            return (false, "Yönetici rollerine (Admin, Daire Başkanı, Genel Sekreter) görev devredilemez.", null, null);
        }

        string originalRole;
        if (delegateeRoles.Contains(AppRoles.Driver))
        {
            originalRole = AppRoles.Driver;
        }
        else if (delegateeRoles.Contains(AppRoles.Kullanici))
        {
            originalRole = AppRoles.Kullanici;
        }
        else
        {
            return (false, "Görev yalnızca aktif bir Ulaşım Şefine veya Şoföre devredilebilir.", null, null);
        }

        // 3. Tarih Doğrulamaları
        // Local veya UTC fark etmeksizin Start < End olmalı
        var startUtc = input.StartDate.Kind == DateTimeKind.Utc ? input.StartDate : input.StartDate.ToUniversalTime();
        var endUtc = input.EndDate.Kind == DateTimeKind.Utc ? input.EndDate : input.EndDate.ToUniversalTime();

        if (startUtc >= endUtc)
        {
            return (false, "Bitiş tarihi ve saati, başlangıç tarihinden ileri bir zaman olmalıdır.", null, null);
        }

        // 4. Zincirleme Devir Engeli (DÜZELTME 5)
        // Eğer devreden kişi, şu an başka bir vekâlet kapsamında vekil durumundaysa devir yapamaz.
        var isDelegatorCurrentlyDelegatee = await _context.RoleDelegations
            .AnyAsync(d => d.DelegateeUserId == delegatorId &&
                           d.ActualRevokedAt == null &&
                           !d.IsDeleted &&
                           startUtc < (d.ActualRevokedAt ?? d.EndDate) &&
                           endUtc > d.StartDate);

        if (isDelegatorCurrentlyDelegatee)
        {
            return (false, "Zincirleme yetki devri yasaktır: Başkasından devralınmış bir vekâlet görevi başka birine devredilemez.", null, null);
        }

        // Eğer devralan kişi, başka bir vekâlet kapsamında asıl yetkili olarak görevini devretmiş durumdaysa
        // veya zaten başka bir görevi devralmışsa
        var isDelegateeChained = await _context.RoleDelegations
            .AnyAsync(d => d.DelegateeUserId == delegateeUser.Id &&
                           d.ActualRevokedAt == null &&
                           !d.IsDeleted &&
                           startUtc < (d.ActualRevokedAt ?? d.EndDate) &&
                           endUtc > d.StartDate);

        if (isDelegateeChained)
        {
            return (false, $"'{delegateeUser.FullName}' adlı personelin belirtilen tarih aralığında devam eden başka bir vekâleti bulunmaktadır.", null, null);
        }

        // 5. Çakışan Devir Kontrolü (DÜZELTME 2 & 5)
        // Çakışma kontrolünde bitiş = ActualRevokedAt ?? EndDate (iptal edilmiş vekâlet çakışma sayılmaz)
        var delegatorConflict = await _context.RoleDelegations
            .AnyAsync(d => d.DelegatorUserId == delegatorId &&
                           d.ActualRevokedAt == null &&
                           !d.IsDeleted &&
                           startUtc < (d.ActualRevokedAt ?? d.EndDate) &&
                           endUtc > d.StartDate);

        if (delegatorConflict)
        {
            return (false, $"Görevi devreden personelin belirtilen tarih aralığıyla ({startUtc.ToLocalTime():dd.MM.yyyy HH:mm} - {endUtc.ToLocalTime():dd.MM.yyyy HH:mm}) çakışan aktif bir yetki devri zaten bulunmaktadır.", null, null);
        }

        // 6. Şoför İse Çakışan Görev Kontrolü (DÜZELTME 6)
        if (originalRole == AppRoles.Driver)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == delegateeUser.Id && !d.IsDeleted);

            if (driver != null)
            {
                var conflictingDuties = await _context.Assignments
                    .Include(a => a.Vehicle)
                    .Include(a => a.AssignmentDrivers)
                    .Where(a => !a.IsDeleted &&
                                (a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)) &&
                                (a.Status == AssignmentStatus.Planned || a.Status == AssignmentStatus.InProgress) &&
                                startUtc < a.PlannedEndTime &&
                                endUtc > a.PlannedStartTime)
                    .ToListAsync();

                if (conflictingDuties.Any())
                {
                    var conflictList = conflictingDuties.Select(c =>
                        $"Görev #{c.Id}: {c.Destination} ({c.Vehicle?.PlateNumber}) [{c.PlannedStartTime.ToLocalTime():dd.MM HH:mm} - {c.PlannedEndTime.ToLocalTime():dd.MM HH:mm}] - Durum: {c.Status.ToTurkishString()}"
                    ).ToList();

                    return (false, $"Görevi devralacak şoförün yetki devri aralığında atanmış veya devam eden görevi bulunmaktadır. Yetki devri oluşturulamaz. Çakışan Görev Sayısı: {conflictList.Count}", null, conflictList);
                }
            }
        }

        // 7. Kayıt Oluşturma
        var actorUser = await _userManager.FindByIdAsync(actorUserId);
        var delegation = new RoleDelegation
        {
            DelegatorUserId = delegatorId,
            DelegateeUserId = delegateeUser.Id,
            StartDate = startUtc,
            EndDate = endUtc,
            Reason = input.Reason.Trim(),
            OriginalDelegateeRole = originalRole,
            DelegatedRole = AppRoles.Kullanici,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUser?.FullName ?? actorUserId,
            IsDeleted = false
        };

        _context.RoleDelegations.Add(delegation);
        await _context.SaveChangesAsync();

        // Cache temizleme (DÜZELTME 1)
        InvalidateUserCache(delegatorId);
        InvalidateUserCache(delegateeUser.Id);

        // AuditLog (DÜZELTME 3)
        await _auditLogService.LogAsync(
            nameof(RoleDelegation),
            delegation.Id,
            AuditAction.Create,
            actorUserId,
            null,
            new
            {
                delegation.Id,
                Delegator = delegatorUser.FullName,
                DelegatorUserId = delegatorId,
                Delegatee = delegateeUser.FullName,
                DelegateeUserId = delegateeUser.Id,
                OriginalRole = originalRole,
                StartDate = startUtc,
                EndDate = endUtc,
                delegation.Reason
            });

        _logger.LogInformation("Vekâlet oluşturuldu. Id: {Id}, Devreden: {Delegator}, Devralan: {Delegatee}, Aktör: {Actor}",
            delegation.Id, delegatorUser.UserName, delegateeUser.UserName, actorUser?.UserName);

        return (true, $"Yetki devri başarıyla oluşturuldu. '{delegateeUser.FullName}' belirtilen tarihler arasında Ulaşım Şefi yetkileriyle görev yapacaktır.", delegation.Id, null);
    }

    public async Task<(bool Success, string Message)> RevokeDelegationAsync(
        int delegationId, 
        string actorUserId, 
        string? reason, 
        bool isElevatedAdmin)
    {
        var delegation = await _context.RoleDelegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .FirstOrDefaultAsync(d => d.Id == delegationId && !d.IsDeleted);

        if (delegation == null)
        {
            return (false, "Vekâlet kaydı bulunamadı.");
        }

        if (delegation.ActualRevokedAt.HasValue)
        {
            return (false, "Bu yetki devri zaten daha önce sonlandırılmıştır.");
        }

        // Yetki kontrolü:
        // Yalnızca Admin, Daire Başkanı VEYA görevi devreden Şef iptal edebilir.
        if (!isElevatedAdmin && delegation.DelegatorUserId != actorUserId)
        {
            return (false, "Bu yetki devrini sonlandırma yetkiniz bulunmamaktadır.");
        }

        var actorUser = await _userManager.FindByIdAsync(actorUserId);
        var revokedAt = DateTime.UtcNow;

        delegation.ActualRevokedAt = revokedAt;
        delegation.RevokedByUserId = actorUserId;
        delegation.RevokeReason = string.IsNullOrWhiteSpace(reason) ? "Göreve erken dönüş / yetkili tarafından sonlandırıldı." : reason.Trim();
        delegation.UpdatedAt = revokedAt;
        delegation.UpdatedBy = actorUser?.FullName ?? actorUserId;

        await _context.SaveChangesAsync();

        // Cache temizleme (DÜZELTME 1: Devri Sonlandır etkisi ANINDA olmalı)
        InvalidateUserCache(delegation.DelegatorUserId);
        InvalidateUserCache(delegation.DelegateeUserId);

        // AuditLog (DÜZELTME 3)
        await _auditLogService.LogAsync(
            nameof(RoleDelegation),
            delegation.Id,
            AuditAction.Update,
            actorUserId,
            new { ActualRevokedAt = (DateTime?)null },
            new
            {
                delegation.Id,
                Action = "RevokeDelegation",
                ActualRevokedAt = revokedAt,
                delegation.RevokeReason,
                RevokedBy = actorUser?.FullName ?? actorUserId
            });

        _logger.LogInformation("Vekâlet sonlandırıldı. Id: {Id}, Aktör: {Actor}", delegation.Id, actorUser?.UserName);
        return (true, "Yetki devri derhal sonlandırıldı. Devralan personelin şef yetkileri kaldırılmıştır.");
    }

    public async Task<RoleDelegation?> GetActiveDelegationForUserAsync(string userId, DateTime? targetTime = null)
    {
        var time = targetTime ?? DateTime.UtcNow;

        // Çok kısa (15 sn) in-memory cache (DÜZELTME 1)
        var cacheKey = $"{CachePrefix}{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out RoleDelegation? cachedDelegation))
        {
            // Zaman aralığını yine de doğrula
            if (cachedDelegation != null &&
                cachedDelegation.ActualRevokedAt == null &&
                !cachedDelegation.IsDeleted &&
                cachedDelegation.StartDate <= time &&
                time <= cachedDelegation.EndDate)
            {
                return cachedDelegation;
            }
        }

        var activeDelegation = await _context.RoleDelegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DelegateeUserId == userId &&
                                      d.ActualRevokedAt == null &&
                                      !d.IsDeleted &&
                                      d.StartDate <= time &&
                                      time <= d.EndDate);

        // 15 saniyelik önbellek
        _memoryCache.Set(cacheKey, activeDelegation, TimeSpan.FromSeconds(15));
        return activeDelegation;
    }

    public async Task<RoleDelegation?> GetActiveDelegationByDelegatorAsync(string delegatorUserId, DateTime? targetTime = null)
    {
        var time = targetTime ?? DateTime.UtcNow;
        return await _context.RoleDelegations
            .Include(d => d.DelegateeUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DelegatorUserId == delegatorUserId &&
                                      d.ActualRevokedAt == null &&
                                      !d.IsDeleted &&
                                      d.StartDate <= time &&
                                      time <= d.EndDate);
    }

    public async Task<bool> HasDelegationConflictForDriverAsync(int driverId, DateTime start, DateTime end)
    {
        var driver = await _context.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted);

        if (driver == null || string.IsNullOrEmpty(driver.UserId))
        {
            return false;
        }

        var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
        var endUtc = end.Kind == DateTimeKind.Utc ? end : end.ToUniversalTime();

        // DÜZELTME 7: Görev saat aralığına göre değerlendir, yalnızca "şimdi"ye göre değil
        return await _context.RoleDelegations
            .AnyAsync(d => d.DelegateeUserId == driver.UserId &&
                           d.ActualRevokedAt == null &&
                           !d.IsDeleted &&
                           startUtc < (d.ActualRevokedAt ?? d.EndDate) &&
                           endUtc > d.StartDate);
    }

    public async Task AutoRevokeDelegationsForUserAsync(string userId, string revokedByUserId, string reason)
    {
        // DÜZELTME 7b: Vekâlet sırasında hesap pasifleştirilir veya silinirse ilgili aktif vekâletler otomatik iptal edilsin
        var now = DateTime.UtcNow;
        var activeDelegations = await _context.RoleDelegations
            .Where(d => (d.DelegatorUserId == userId || d.DelegateeUserId == userId) &&
                        d.ActualRevokedAt == null &&
                        !d.IsDeleted &&
                        d.EndDate >= now)
            .ToListAsync();

        if (!activeDelegations.Any()) return;

        foreach (var del in activeDelegations)
        {
            del.ActualRevokedAt = now;
            del.RevokedByUserId = revokedByUserId;
            del.RevokeReason = reason;
            del.UpdatedAt = now;
            del.UpdatedBy = revokedByUserId;

            InvalidateUserCache(del.DelegatorUserId);
            InvalidateUserCache(del.DelegateeUserId);

            await _auditLogService.LogAsync(
                nameof(RoleDelegation),
                del.Id,
                AuditAction.Update,
                revokedByUserId,
                new { ActualRevokedAt = (DateTime?)null },
                new
                {
                    del.Id,
                    Action = "AutoRevokeOnAccountDeactivation",
                    ActualRevokedAt = now,
                    del.RevokeReason
                });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Kullanıcı ({UserId}) durumu değiştiği için {Count} adet vekâlet otomatik sonlandırıldı.", userId, activeDelegations.Count);
    }

    public async Task<byte[]> GenerateDelegationPdfAsync(int delegationId, string actorUserId, bool isElevatedAdmin)
    {
        var delegation = await _context.RoleDelegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateeUser)
            .Include(d => d.RevokedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == delegationId && !d.IsDeleted);

        if (delegation == null)
        {
            throw new KeyNotFoundException("Yetki devri belgesi bulunamadı.");
        }

        // Yetki kontrolü: GenelSekreter dahil görebilir, Şef ise sadece kendi taraf olduğu belgeyi
        if (!isElevatedAdmin && delegation.DelegatorUserId != actorUserId && delegation.DelegateeUserId != actorUserId)
        {
            throw excitingUnauthorizedException();
        }

        var document = new RoleDelegationPdfDocument(delegation);
        return document.GeneratePdf();
    }

    private void InvalidateUserCache(string userId)
    {
        _memoryCache.Remove($"{CachePrefix}{userId}");
    }

    private static UnauthorizedAccessException excitingUnauthorizedException()
    {
        return new UnauthorizedAccessException("Bu yetki devri belgesini görüntüleme yetkiniz bulunmamaktadır.");
    }
}
