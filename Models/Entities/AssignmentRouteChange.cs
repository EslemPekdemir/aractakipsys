// File: Models/Entities/AssignmentRouteChange.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aractakipsys.Models.Entities;

public class AssignmentRouteChange
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AssignmentId { get; set; }

    [ForeignKey(nameof(AssignmentId))]
    public virtual Assignment Assignment { get; set; } = null!;

    [StringLength(250)]
    public string? OldRoute { get; set; }

    [Required]
    [StringLength(250)]
    public string NewRoute { get; set; } = string.Empty;

    [StringLength(200)]
    public string? OldDestination { get; set; }

    [StringLength(200)]
    public string? NewDestination { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string ChangedByUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(ChangedByUserId))]
    public virtual ApplicationUser? ChangedByUser { get; set; }

    [Required]
    [StringLength(150)]
    public string ChangedByFullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsReviewedByAdmin { get; set; } = false;

    public DateTime? ReviewedAt { get; set; }

    [StringLength(450)]
    public string? ReviewedByUserId { get; set; }

    public bool IsDeleted { get; set; } = false;
}
