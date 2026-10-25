// File: Models/Entities/Assignment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.Entities;

public class Assignment : BaseAuditableEntity
{
    [Required]
    public int VehicleId { get; set; }

    public int? DriverId { get; set; }

    [StringLength(250)]
    public string? Destination { get; set; }

    [StringLength(500)]
    public string? Route { get; set; }

    public bool IsDriverless { get; set; } = false;

    public bool IsExternalDriver { get; set; } = false;

    [StringLength(150)]
    public string? ExternalDriverFullName { get; set; }

    [StringLength(150)]
    public string? ExternalDriverDepartment { get; set; }

    [StringLength(150)]
    public string? RequestedByDepartment { get; set; }

    [Required]
    [StringLength(300)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    public DateTime PlannedStartTime { get; set; }

    [Required]
    public DateTime PlannedEndTime { get; set; }

    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public int? StartKm { get; set; }
    public int? EndKm { get; set; }

    [Required]
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Planned;

    [StringLength(300)]
    public string? CancellationReason { get; set; }

    [StringLength(500)]
    public string? DriverReturnNotes { get; set; }

    // Şoför Bildirimi ve Onay Alanları (Yeni 4 Kolon)
    public bool StartReportedBySelf { get; set; } = false;

    [StringLength(450)]
    public string? StartApprovedByUserId { get; set; }

    public bool EndReportedBySelf { get; set; } = false;

    [StringLength(450)]
    public string? EndApprovedByUserId { get; set; }

    // Konaklama ve Yevmiye Alanları (Çok Günlük ve Saatlik Görevler İçin - Aşama F)
    public int? AccommodationNights { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AccommodationCost { get; set; }

    [StringLength(20)]
    public string? PerDiemType { get; set; } = "Hourly";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PerDiemHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PerDiemRate { get; set; }

    public int? PerDiemDays { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PerDiemAmount { get; set; }

    [StringLength(500)]
    public string? ExpenseNotes { get; set; }

    // Şoför Görev Ücreti ve Çalışma Saati Alanları (Şef / Admin Hesaplama)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? HourlyRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CalculatedDutyHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalDriverFee { get; set; }

    // İmzalı Belge Alanları (Aşama H)
    [StringLength(500)]
    public string? SignedDocumentPath { get; set; }

    [StringLength(255)]
    public string? SignedDocumentOriginalFileName { get; set; }

    public DateTime? SignedDocumentUploadedAt { get; set; }

    [StringLength(450)]
    public string? SignedDocumentUploadedByUserId { get; set; }

    // Güzergah Dışı Sevk Alanları (Acil Durumlar İçin Şef Kaydı - Faz 3)
    [StringLength(150)]
    public string? OffRouteDispatcherName { get; set; }

    [StringLength(100)]
    public string? OffRouteDispatcherTitle { get; set; }

    [StringLength(500)]
    public string? OffRouteReason { get; set; }

    public DateTime? OffRouteReportedAt { get; set; }

    [StringLength(450)]
    public string? OffRouteReportedByUserId { get; set; }

    // Şoför Araç Kontrol Tikleri (Faz 3)
    public bool CheckOil { get; set; } = false;
    public bool CheckWater { get; set; } = false;
    public bool CheckFuel { get; set; } = false;
    public bool CheckHydraulic { get; set; } = false;
    public bool CheckTires { get; set; } = false;
    public DateTime? ChecksCompletedAt { get; set; }

    // Navigation Properties
    public Vehicle Vehicle { get; set; } = null!;
    public Driver? Driver { get; set; }
    public ICollection<AssignmentDriver> AssignmentDrivers { get; set; } = new List<AssignmentDriver>();
    public ApplicationUser? StartApprovedByUser { get; set; }
    public ApplicationUser? EndApprovedByUser { get; set; }
    public ApplicationUser? SignedDocumentUploadedByUser { get; set; }
    public ApplicationUser? OffRouteReportedByUser { get; set; }
    public ICollection<AssignmentRouteChange> RouteChanges { get; set; } = new List<AssignmentRouteChange>();
}
