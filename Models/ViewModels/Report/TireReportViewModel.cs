// File: Models/ViewModels/Report/TireReportViewModel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Vehicle;

namespace aractakipsys.Models.ViewModels.Report;

public class TireReportViewModel
{
    public TireReportFilterViewModel Filter { get; set; } = new();
    public List<VehicleTireItemViewModel> Items { get; set; } = new();

    // Özet KPI Kartı Sayaçları
    public int TotalCount => Items.Count;
    public int TotalVehiclesCount => Items.Select(x => x.VehicleId).Distinct().Count();
    
    // Değişim gerekli kritik lastikler (KM veya tavsiye tarihi aşılmış)
    public int CriticalCount => Items.Count(x => x.Status == TireStatus.Critical);
    public int ExpiredCount => CriticalCount;

    // 30 gün içinde dolacak veya KM uyarı eşiğinde olan yaklaşan lastikler
    public int ApproachingCount => Items.Count(x => x.Status == TireStatus.Approaching);
    public int ExpiringSoonCount => ApproachingCount;

    // Normal (Eşik girilmiş, aşınma ve süre problemi olmayanlar)
    public int NormalCount => Items.Count(x => x.Status == TireStatus.Normal);
    public int ValidCount => NormalCount;

    // Veri Eksik (Eşik girilmemiş veya kullanım KM hesaplanamıyor)
    public int MissingDataCount => Items.Count(x => x.Status == TireStatus.MissingData);

    public string FilterOptionDisplayName => Filter.FilterOption switch
    {
        "Critical" or "Expired" => "Değişim Gerekli (Kritik / Aşılmış)",
        "Approaching" or "ExpiringSoon" => "Değişimi Yaklaşanlar (30 Gün / KM Eşiği)",
        "Normal" or "Valid" => "Normal / Güvenli Lastikler",
        "MissingData" => "Veri Eksik Olanlar (Eşik / KM Bilgisi Yok)",
        _ => "Tüm Lastikler"
    };
}

public class TireReportFilterViewModel
{
    public int? VehicleId { get; set; }
    public string FilterOption { get; set; } = "All"; // All, Expired, ExpiringSoon, Valid
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public List<SelectListItem> VehicleList { get; set; } = new();
}
