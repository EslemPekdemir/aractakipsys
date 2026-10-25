// File: Models/Entities/VehicleTire.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aractakipsys.Models.Entities;

public class VehicleTire : BaseAuditableEntity
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Size { get; set; } // Örn: 205/55 R16 (eski veride yok, nullable)

    [StringLength(10)]
    public string? DotDate { get; set; } // Örn: "52/23" (hafta/yıl, eski veride yok, nullable)

    public int? ProductionYear { get; set; } // TireYear'dan taşınan yıl bilgisi

    [Required]
    [StringLength(50)]
    public string Season { get; set; } = "Yaz"; // Yaz / Kış / Dört Mevsim

    public DateTime? InstalledDate { get; set; } // Takılma/değişim tarihi (eski veride yok, nullable)

    public int? InstalledKm { get; set; } // Takıldığı andaki araç KM'si (eski veride yok, nullable)

    public int? RecommendedReplaceKm { get; set; } // Değiştirilmesi gereken KM (opsiyonel)

    public DateTime? RecommendedReplaceDate { get; set; } // Değiştirilmesi gereken tarih (opsiyonel)

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Cost { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation Property
    public Vehicle Vehicle { get; set; } = null!;
}
