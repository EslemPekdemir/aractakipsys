// File: Models/ViewModels/AuditLog/AuditLogListItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.AuditLog;

public class AuditLogListItemViewModel
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public string ChangedByUserName { get; set; } = string.Empty;
    public string ChangedByUserEmail { get; set; } = string.Empty;
    public string? OnBehalfOfUserId { get; set; }
    public string? OnBehalfOfUserName { get; set; }
    public int? DelegationId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public string ActionDisplayName => Action switch
    {
        AuditAction.Create => "Oluşturma",
        AuditAction.Update => "Güncelleme",
        AuditAction.Delete => "Silme",
        _ => Action.ToString()
    };

    public string ActionBadgeClass => Action switch
    {
        AuditAction.Create => "badge-primary bg-primary text-white",
        AuditAction.Update => "badge-warning bg-warning text-dark",
        AuditAction.Delete => "badge-danger bg-danger text-white",
        _ => "badge-secondary"
    };

    public string EntityDisplayName => EntityName switch
    {
        "Vehicle" => "Araç",
        "Driver" => "Şoför",
        "Assignment" => "Görev / Atama",
        "Maintenance" => "Bakım / Servis",
        "VehicleRequest" => "Araç Talebi",
        "ApplicationUser" => "Kullanıcı",
        _ => EntityName
    };
}
