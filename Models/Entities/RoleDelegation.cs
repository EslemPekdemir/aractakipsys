// File: Models/Entities/RoleDelegation.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

public class RoleDelegation : BaseAuditableEntity
{
    [Required]
    [StringLength(450)]
    public string DelegatorUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string DelegateeUserId { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public DateTime? ActualRevokedAt { get; set; }

    [StringLength(450)]
    public string? RevokedByUserId { get; set; }

    [StringLength(500)]
    public string? RevokeReason { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string OriginalDelegateeRole { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string DelegatedRole { get; set; } = "Kullanici";

    // Navigations
    public ApplicationUser? DelegatorUser { get; set; }
    public ApplicationUser? DelegateeUser { get; set; }
    public ApplicationUser? RevokedByUser { get; set; }
}
