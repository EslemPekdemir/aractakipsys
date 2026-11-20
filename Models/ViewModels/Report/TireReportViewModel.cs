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

    private int? _totalCount;
    private int? _totalVehiclesCount;
    private int? _criticalCount;
    private int? _approachingCount;
    private int? _normalCount;
    private int? _missingDataCount;

    // Özet KPI Kartı Sayaçları (Sayfalamadan bağımsız tüm filtrelenmiş kayıtların toplamları)
    public int TotalCount
    {
        get => _totalCount ?? Items.Count;
        set => _totalCount = value;
    }

    public int TotalVehiclesCount
    {
        get => _totalVehiclesCount ?? Items.Select(x => x.VehicleId).Distinct().Count();
        set => _totalVehiclesCount = value;
    }
    
    // Değişim gerekli kritik lastikler (KM veya tavsiye tarihi aşılmış)
    public int CriticalCount
    {
        get => _criticalCount ?? Items.Count(x => x.Status == TireStatus.Critical);
        set => _criticalCount = value;
    }
    public int ExpiredCount => CriticalCount;

    // 30 gün içinde dolacak veya KM uyarı eşiğinde olan yaklaşan lastikler
    public int ApproachingCount
    {
        get => _approachingCount ?? Items.Count(x => x.Status == TireStatus.Approaching);
        set => _approachingCount = value;
    }
    public int ExpiringSoonCount => ApproachingCount;

    // Normal (Eşik girilmiş, aşınma ve süre problemi olmayanlar)
    public int NormalCount
    {
        get => _normalCount ?? Items.Count(x => x.Status == TireStatus.Normal);
        set => _normalCount = value;
    }
    public int ValidCount => NormalCount;

    // Veri Eksik (Eşik girilmemiş veya kullanım KM hesaplanamıyor)
    public int MissingDataCount
    {
        get => _missingDataCount ?? Items.Count(x => x.Status == TireStatus.MissingData);
        set => _missingDataCount = value;
    }

    // Sayfalama Özellikleri
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

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
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public List<SelectListItem> VehicleList { get; set; } = new();
}
