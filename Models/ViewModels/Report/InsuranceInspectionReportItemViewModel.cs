// File: Models/ViewModels/Report/InsuranceInspectionReportItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Report;

public class InsuranceInspectionReportItemViewModel
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BrandModel => $"{Brand} {Model}".Trim();
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;

    // Sigorta
    public DateTime InsuranceStartDate { get; set; }
    public DateTime InsuranceExpiryDate { get; set; }
    public int InsuranceDaysRemaining { get; set; }
    public decimal? InsuranceCost { get; set; }
    public string? InsuranceCompanyName { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public bool IsInsuranceExpired => InsuranceDaysRemaining < 0;
    public bool IsInsuranceExpiringSoon => InsuranceDaysRemaining >= 0 && InsuranceDaysRemaining <= 30;

    // Muayene
    public DateTime InspectionStartDate { get; set; }
    public DateTime InspectionExpiryDate { get; set; }
    public int InspectionDaysRemaining { get; set; }
    public decimal? InspectionCost { get; set; }
    public string? InspectionStationName { get; set; }
    public string? InspectionPolicyNumber { get; set; }
    public bool IsInspectionExpired => InspectionDaysRemaining < 0;
    public bool IsInspectionExpiringSoon => InspectionDaysRemaining >= 0 && InspectionDaysRemaining <= 30;

    // Kasko (Faz 5-A / 7-A: Kasko dahil)
    public DateTime? KaskoStartDate { get; set; }
    public DateTime? KaskoExpiryDate { get; set; }
    public int? KaskoDaysRemaining { get; set; }
    public decimal? KaskoCost { get; set; }
    public string? KaskoCompanyName { get; set; }
    public string? KaskoPolicyNumber { get; set; }
    public bool IsKaskoExpired => KaskoDaysRemaining.HasValue && KaskoDaysRemaining.Value < 0;
    public bool IsKaskoExpiringSoon => KaskoDaysRemaining.HasValue && KaskoDaysRemaining.Value >= 0 && KaskoDaysRemaining.Value <= 30;

    // Araç Toplam Belge Maliyeti
    public decimal TotalDocumentCost => (InsuranceCost ?? 0) + (InspectionCost ?? 0) + (KaskoCost ?? 0);

    // Genel Belge Durumu
    public bool HasAnyExpired => IsInsuranceExpired || IsInspectionExpired || Status == VehicleStatus.DocumentExpired;
    public bool HasAnyExpiringSoon => !HasAnyExpired && (IsInsuranceExpiringSoon || IsInspectionExpiringSoon);
}
