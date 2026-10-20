// File: Services/Implementations/AuditLogService.cs
using System.Text.Json;
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.AuditLog;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        string entityName,
        int entityId,
        AuditAction action,
        string? changedByUserId,
        object? oldValues,
        object? newValues)
    {
        try
        {
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("AuditLog kaydedildi. Entity: {EntityName}, Id: {EntityId}, Action: {Action}, UserId: {UserId}",
                entityName, entityId, action, changedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuditLog kaydı oluşturulurken hata oluştu. Entity: {EntityName}, Id: {EntityId}", entityName, entityId);
        }
    }

    public async Task<AuditLogFilterViewModel> GetAuditLogsPagedAsync(AuditLogFilterViewModel filter)
    {
        var query = _context.AuditLogs
            .Include(a => a.ChangedByUser)
            .AsNoTracking()
            .AsQueryable();

        // 1. Entity Filtresi
        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(a => a.EntityName == filter.EntityName.Trim());
        }

        // 2. İşlem (Action) Filtresi
        if (filter.Action.HasValue)
        {
            query = query.Where(a => a.Action == filter.Action.Value);
        }

        // 3. Kullanıcı Filtresi
        if (!string.IsNullOrWhiteSpace(filter.ChangedByUserId))
        {
            query = query.Where(a => a.ChangedByUserId == filter.ChangedByUserId.Trim());
        }

        // 4. Tarih Aralığı Filtresi
        if (filter.StartDate.HasValue)
        {
            var startUtc = filter.StartDate.Value.Date;
            query = query.Where(a => a.ChangedAt >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = filter.EndDate.Value.Date.AddDays(1);
            query = query.Where(a => a.ChangedAt < endUtc);
        }

        // 5. Serbest Metin Arama Filtresi (EntityName, User Email/Name, JSON içeriği)
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.EntityName.ToLower().Contains(term) ||
                (a.ChangedByUser != null && (a.ChangedByUser.FullName.ToLower().Contains(term) ||
                                            (a.ChangedByUser.Email != null && a.ChangedByUser.Email.ToLower().Contains(term)))) ||
                (a.OldValues != null && a.OldValues.ToLower().Contains(term)) ||
                (a.NewValues != null && a.NewValues.ToLower().Contains(term)));
        }

        // Toplam Kayıt Sayısı
        filter.TotalCount = await query.CountAsync();

        // Sayfalama & Sıralama (En yeni kayıt en üstte)
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

        var items = await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogListItemViewModel
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                ChangedByUserId = a.ChangedByUserId ?? "",
                ChangedByUserName = a.ChangedByUser != null
                    ? a.ChangedByUser.FullName
                    : (a.ChangedByUserId ?? "Sistem / Anonim"),
                ChangedByUserEmail = a.ChangedByUser != null ? (a.ChangedByUser.Email ?? "") : "",
                ChangedAt = a.ChangedAt.ToLocalTime(),
                OldValues = a.OldValues,
                NewValues = a.NewValues
            })
            .ToListAsync();

        filter.Logs = items;
        filter.PageNumber = pageNumber;
        filter.PageSize = pageSize;

        // Dropdown Verileri
        filter.EntityNameList = await _context.AuditLogs
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        // Dropdown ve Kullanıcı Çözümleme Haritası (UserMap)
        var usersWithRoles = await (from u in _context.Users
                                    join ur in _context.UserRoles on u.Id equals ur.UserId into urGroup
                                    from ur in urGroup.DefaultIfEmpty()
                                    join r in _context.Roles on ur.RoleId equals r.Id into rGroup
                                    from r in rGroup.DefaultIfEmpty()
                                    select new
                                    {
                                        u.Id,
                                        u.FullName,
                                        u.Email,
                                        RoleName = r != null ? r.Name : null
                                    })
                                    .ToListAsync();

        filter.UserMap = new Dictionary<string, string>();
        filter.UserList = new List<SelectListItem>();

        foreach (var u in usersWithRoles.OrderBy(x => x.FullName))
        {
            var fullName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName.Trim() : (u.Email ?? "Bilinmeyen Kullanıcı");

            var roleTitle = u.RoleName switch
            {
                "Admin" => "Sistem Yöneticisi",
                "Kullanici" => "Ulaşım Şefi",
                "Driver" => "Şoför",
                _ => !string.IsNullOrEmpty(u.RoleName) ? u.RoleName : "Personel"
            };

            // FullName zaten parantez veya unvan içeriyorsa (örn. 'Sistem Yöneticisi (Bilgi İşlem)') çift parantez oluşturma
            var userDisplay = (fullName.Contains('(') || (!string.IsNullOrEmpty(roleTitle) && fullName.Contains(roleTitle, StringComparison.OrdinalIgnoreCase)))
                ? fullName
                : $"{fullName} ({roleTitle})";

            filter.UserMap[u.Id] = userDisplay;

            if (!filter.UserList.Any(item => item.Value == u.Id))
            {
                filter.UserList.Add(new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.Email})"
                });
            }
        }

        return filter;
    }
}
