// File: Services/Interfaces/IAuditLogService.cs
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.AuditLog;

namespace aractakipsys.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string entityName, int entityId, AuditAction action, string? changedByUserId, object? oldValues, object? newValues);
    Task<AuditLogFilterViewModel> GetAuditLogsPagedAsync(AuditLogFilterViewModel filter);
}
