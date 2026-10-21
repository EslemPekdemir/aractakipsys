// File: Models/ViewModels/Dashboard/DashboardPlateSearchResultViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Dashboard;

public class DashboardPlateSearchResultViewModel
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BrandModel => $"{Brand} {Model}".Trim();
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public int CurrentKm { get; set; }
    public VehicleStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;

    // Sigorta & Muayene Bitiş Tarihleri
    public DateTime InsuranceExpiryDate { get; set; }
    public int InsuranceDaysRemaining { get; set; }
    public bool IsInsuranceExpired => InsuranceDaysRemaining < 0;

    public DateTime InspectionExpiryDate { get; set; }
    public int InspectionDaysRemaining { get; set; }
    public bool IsInspectionExpired => InspectionDaysRemaining < 0;

    // Ceza Notu (FineNotes)
    public string? FineNotes { get; set; }
    public bool HasFineNotes => !string.IsNullOrWhiteSpace(FineNotes);

    // O aracı kullanmış şoförler (Completed kayıtlar, en son kullanan üstte)
    public List<DashboardVehicleDriverHistoryViewModel> DriverHistory { get; set; } = new();
}

public class DashboardVehicleDriverHistoryViewModel
{
    public int AssignmentId { get; set; }
    public int? DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string? DriverPhone { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string FormattedCompletedDate => CompletedDate?.ToString("dd.MM.yyyy HH:mm") ?? "-";
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public int? StartKm { get; set; }
    public int? EndKm { get; set; }
    public int? TotalKm => (StartKm.HasValue && EndKm.HasValue && EndKm >= StartKm) ? (EndKm.Value - StartKm.Value) : null;
    public string Purpose { get; set; } = string.Empty;
    public string? RequestedByDepartment { get; set; }
}
