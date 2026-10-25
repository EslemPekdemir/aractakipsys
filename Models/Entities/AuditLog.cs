// File: Models/Entities/AuditLog.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    public int EntityId { get; set; }

    [Required]
    public AuditAction Action { get; set; }

    public string? ChangedByUserId { get; set; }

    [StringLength(450)]
    public string? OnBehalfOfUserId { get; set; }

    public int? DelegationId { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    // Navigation Properties
    public ApplicationUser? ChangedByUser { get; set; }
    public ApplicationUser? OnBehalfOfUser { get; set; }
    public RoleDelegation? Delegation { get; set; }
}
