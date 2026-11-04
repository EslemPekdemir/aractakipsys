// File: Models/ViewModels/Vehicle/VehicleTireItemViewModel.cs
using System;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleTireItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehiclePlate { get => PlateNumber; set => PlateNumber = value; }
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? DotDate { get; set; }
    public int? ProductionYear { get; set; }
    public string Season { get; set; } = "Yaz";
    public string SeasonDisplay => Season switch
    {
        "Summer" => "Yaz",
        "Winter" => "Kış",
        "AllSeason" => "Dört Mevsim",
        _ => Season
    };
    public string DotDateDisplay => !string.IsNullOrWhiteSpace(DotDate) ? DotDate : (ProductionYear.HasValue ? ProductionYear.Value.ToString() : "Bilinmiyor");

    public DateTime? InstalledDate { get; set; }
    public int? InstalledKm { get; set; }
    public int? RecommendedReplaceKm { get; set; }
    public DateTime? RecommendedReplaceDate { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Dinamik hesaplama: Aracın anlık güncel KM'si
    public int VehicleCurrentKm { get; set; }
    public int CurrentVehicleKm { get => VehicleCurrentKm; set => VehicleCurrentKm = value; }

    // Kullanım KM'si: Aracın güncel KM'si - Takıldığı KM (Takılma KM'si yoksa null)
    public int? UsedKm => InstalledKm.HasValue ? Math.Max(0, VehicleCurrentKm - InstalledKm.Value) : null;
    public string UsedKmDisplay => UsedKm.HasValue ? $"{UsedKm.Value:N0} KM" : "Bilinmiyor";

    // En son takılan (güncel aktif) lastik mi?
    public bool IsCurrentTire { get; set; }

    // KM Uyarı Eşiği (Varsayılan 1000 KM, appsettings: VehicleSettings:TireKmWarningThreshold üzerinden ayarlanabilir)
    public int KmWarningThreshold { get; set; } = 1000;

    public int? RemainingKm => RecommendedReplaceKm.HasValue ? (RecommendedReplaceKm.Value - VehicleCurrentKm) : null;
    public int? RemainingDays => RecommendedReplaceDate.HasValue ? (int)(RecommendedReplaceDate.Value.Date - DateTime.Today).TotalDays : null;

    // 4'LÜ DURUM MANTIĞI:
    // 1. Veri Eksik: Hiçbir değişim eşiği (ne KM ne tarih) girilmemiş VEYA Takılma KM girilmediği için Kullanım KM hesaplanamıyor.
    public bool IsMissingData => !InstalledKm.HasValue || (!RecommendedReplaceKm.HasValue && !RecommendedReplaceDate.HasValue);

    // 2. Değişim Gerekli (Kritik): Eşik girilmiş ve KM veya tavsiye tarihi aşılmış/dolmuş.
    public bool IsCritical => !IsMissingData && (
        (RemainingKm.HasValue && RemainingKm.Value <= 0) || 
        (RemainingDays.HasValue && RemainingDays.Value <= 0));

    // 3. Değişimi Yaklaşıyor: Eşik girilmiş, aşılmamış, ancak 30 gün içinde veya KM uyarı eşiğinde (<= 1000 KM).
    public bool IsApproaching => !IsMissingData && !IsCritical && (
        (RemainingKm.HasValue && RemainingKm.Value <= KmWarningThreshold) || 
        (RemainingDays.HasValue && RemainingDays.Value <= 30));

    // 4. Normal: Eşik girilmiş, aşınma ve süre problemi yok (güvenli sınırda).
    public bool IsNormal => !IsMissingData && !IsCritical && !IsApproaching;

    /// <summary>
    /// Tek ortak durum sınıflandırması (Normal, Approaching, Critical, MissingData).
    /// </summary>
    public TireStatus Status
    {
        get
        {
            if (IsMissingData) return TireStatus.MissingData;
            if (IsCritical) return TireStatus.Critical;
            if (IsApproaching) return TireStatus.Approaching;
            return TireStatus.Normal;
        }
    }

    public string StatusDisplayName => Status.ToTurkishString();
    public string StatusBadgeClass => Status.ToBadgeClass();

    // Uyarı Kontrolü (Dashboard ve zil bildirimleri için: YALNIZCA Değişim Gerekli veya Yaklaşanlar; Veri Eksik ve Normal asla alert üretmez)
    public bool IsAlert => IsCritical || IsApproaching;

    public bool IsKmAlert => !IsMissingData && RecommendedReplaceKm.HasValue && (RemainingKm <= KmWarningThreshold);
    public bool IsDateAlert => !IsMissingData && RecommendedReplaceDate.HasValue && (RemainingDays <= 30);

    public string AlertReason
    {
        get
        {
            if (IsMissingData)
            {
                if (!InstalledKm.HasValue && !RecommendedReplaceKm.HasValue && !RecommendedReplaceDate.HasValue)
                    return "Takılma KM ve değişim eşiği girilmemiş";
                if (!InstalledKm.HasValue)
                    return "Takılma KM girilmemiş (Kullanım KM bilinmiyor)";
                return "Değişim eşiği (KM/Tarih) girilmemiş";
            }
            if (IsCritical)
            {
                if (RemainingKm.HasValue && RemainingKm.Value <= 0 && RemainingDays.HasValue && RemainingDays.Value <= 0)
                    return $"Hem KM ({Math.Abs(RemainingKm.Value):N0} KM aşıldı) hem Tarih ({Math.Abs(RemainingDays.Value)} gün geçti) doldu!";
                if (RemainingKm.HasValue && RemainingKm.Value <= 0)
                    return $"Değişim KM'si aşıldı ({Math.Abs(RemainingKm.Value):N0} KM geçti)";
                if (RemainingDays.HasValue && RemainingDays.Value <= 0)
                    return $"Değişim tarihi geçti ({Math.Abs(RemainingDays.Value)} gün önce)";
            }
            if (IsApproaching)
            {
                if (RemainingKm.HasValue && RemainingKm.Value <= KmWarningThreshold && RemainingDays.HasValue && RemainingDays.Value <= 30)
                    return $"Hem KM ({RemainingKm:N0} KM kaldı) hem Süre ({RemainingDays} gün kaldı) yaklaştı";
                if (RemainingKm.HasValue && RemainingKm.Value <= KmWarningThreshold)
                    return $"Değişim KM'si yaklaştı ({RemainingKm:N0} KM kaldı)";
                if (RemainingDays.HasValue && RemainingDays.Value <= 30)
                    return $"Değişim süresi yaklaştı ({RemainingDays} gün kaldı)";
            }
            return string.Empty;
        }
    }
}
