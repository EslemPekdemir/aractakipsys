// File: Models/ViewModels/Report/TireReportViewModel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using aractakipsys.Models.ViewModels.Vehicle;

namespace aractakipsys.Models.ViewModels.Report;

public class TireReportViewModel
{
    public TireReportFilterViewModel Filter { get; set; } = new();
    public List<VehicleTireItemViewModel> Items { get; set; } = new();

    // 4 Özet KPI Kartı Sayaçları
    public int TotalCount => Items.Count;
    public int TotalVehiclesCount => Items.Select(x => x.VehicleId).Distinct().Count();
    
    // Süresi veya KM'si dolmuş kritik lastikler
    public int ExpiredCount => Items.Count(x => 
        (x.RemainingKm.HasValue && x.RemainingKm.Value <= 0) || 
        (x.RemainingDays.HasValue && x.RemainingDays.Value <= 0));

    // 30 gün içinde dolacak veya KM uyarı eşiğine girmiş yaklaşan lastikler
    public int ExpiringSoonCount => Items.Count(x => 
        x.IsAlert && 
        !((x.RemainingKm.HasValue && x.RemainingKm.Value <= 0) || (x.RemainingDays.HasValue && x.RemainingDays.Value <= 0)));

    // Sorunsuz geçerli lastikler
    public int ValidCount => Items.Count(x => !x.IsAlert);

    public string FilterOptionDisplayName => Filter.FilterOption switch
    {
        "Expired" => "Süresi / KM'si Geçenler (Kritik)",
        "ExpiringSoon" => "Değişimi Yaklaşanlar (30 Gün / KM Eşiği)",
        "Valid" => "Sorunsuz / Geçerli Lastikler",
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
