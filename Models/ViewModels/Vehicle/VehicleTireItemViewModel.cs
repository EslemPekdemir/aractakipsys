// File: Models/ViewModels/Vehicle/VehicleTireItemViewModel.cs
using System;

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

    // Uyarı Kontrolü (Değişim tarihine 30 gün kala veya KM eşiği yaklaşınca / geçince)
    // Eşik: Kalan KM <= KmWarningThreshold veya eksiye düşmüşse; Tarih: <= 30 gün veya geçmişse
    public bool IsKmAlert => RecommendedReplaceKm.HasValue && (RecommendedReplaceKm.Value - VehicleCurrentKm <= KmWarningThreshold);
    public bool IsDateAlert => RecommendedReplaceDate.HasValue && ((RecommendedReplaceDate.Value.Date - DateTime.Today).TotalDays <= 30);
    public bool IsAlert => IsKmAlert || IsDateAlert;

    public int? RemainingKm => RecommendedReplaceKm.HasValue ? (RecommendedReplaceKm.Value - VehicleCurrentKm) : null;
    public int? RemainingDays => RecommendedReplaceDate.HasValue ? (int)(RecommendedReplaceDate.Value.Date - DateTime.Today).TotalDays : null;

    public string AlertReason
    {
        get
        {
            if (IsKmAlert && IsDateAlert)
            {
                // Hangisi daha acilse veya ikisi birden
                if (RemainingKm <= 0 && RemainingDays <= 0)
                    return $"Hem KM ({Math.Abs(RemainingKm.Value):N0} KM aşıldı) hem Tarih ({Math.Abs(RemainingDays.Value)} gün geçti) doldu!";
                if (RemainingKm <= 0)
                    return $"Değişim KM'si aşıldı ({Math.Abs(RemainingKm.Value):N0} KM geçti)";
                if (RemainingDays <= 0)
                    return $"Değişim tarihi geçti ({Math.Abs(RemainingDays.Value)} gün önce)";
                return $"Hem KM ({RemainingKm:N0} KM kaldı) hem Süre ({RemainingDays} gün kaldı) yaklaştı";
            }
            if (IsKmAlert)
            {
                return RemainingKm <= 0 
                    ? $"Değişim KM'si aşıldı ({Math.Abs(RemainingKm.Value):N0} KM geçti)" 
                    : $"Değişim KM'si yaklaştı ({RemainingKm:N0} KM kaldı)";
            }
            if (IsDateAlert)
            {
                return RemainingDays <= 0 
                    ? $"Değişim tarihi geçti ({Math.Abs(RemainingDays.Value)} gün önce)" 
                    : $"Değişim süresi yaklaştı ({RemainingDays} gün kaldı)";
            }
            return string.Empty;
        }
    }
}
