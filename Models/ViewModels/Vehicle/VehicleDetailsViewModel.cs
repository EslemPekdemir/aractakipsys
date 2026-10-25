// File: Models/ViewModels/Vehicle/VehicleDetailsViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleDetailsViewModel
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public int CurrentKm { get; set; }
    public VehicleStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime InspectionStartDate { get; set; }
    public DateTime InspectionExpiryDate { get; set; }
    public DateTime InsuranceStartDate { get; set; }
    public DateTime InsuranceExpiryDate { get; set; }
    public string? TireBrand { get; set; }
    public int? TireYear { get; set; }
    public string? TireSeason { get; set; }
    public string? FineNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Güncel Lastik Bilgisi (Faz 5-B)
    public VehicleTireItemViewModel? CurrentTire { get; set; }

    public string StatusBadgeClass => Status switch
    {
        VehicleStatus.Idle => "bg-success",
        VehicleStatus.OnDuty => "bg-primary",
        VehicleStatus.Maintenance => "bg-warning",
        VehicleStatus.OutOfService => "bg-danger",
        VehicleStatus.DocumentExpired => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusDisplayName => Status switch
    {
        VehicleStatus.Idle => "Boşta",
        VehicleStatus.OnDuty => "Görevde",
        VehicleStatus.Maintenance => "Bakımda",
        VehicleStatus.OutOfService => "Hizmet Dışı",
        VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
        _ => Status.ToString()
    };

    // Son 5 Görevlendirme Özeti (ENDPOINT-SPESIFIKASYONU Bölüm 1)
    public List<VehicleAssignmentSummaryViewModel> RecentAssignments { get; set; } = new();

    // Bakım Geçmişi Özeti
    public List<VehicleMaintenanceSummaryViewModel> MaintenanceHistory { get; set; } = new();
}

public class VehicleAssignmentSummaryViewModel
{
    public int Id { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public int? StartKm { get; set; }
    public int? EndKm { get; set; }
    public AssignmentStatus Status { get; set; }

    public string StatusBadgeClass => Status switch
    {
        AssignmentStatus.Planned => "bg-secondary",
        AssignmentStatus.InProgress => "bg-primary",
        AssignmentStatus.Completed => "bg-success",
        AssignmentStatus.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusDisplayName => Status switch
    {
        AssignmentStatus.Planned => "Planlandı",
        AssignmentStatus.InProgress => "Devam Ediyor",
        AssignmentStatus.Completed => "Tamamlandı",
        AssignmentStatus.Cancelled => "İptal Edildi",
        _ => Status.ToString()
    };
}

public class VehicleMaintenanceSummaryViewModel
{
    public int Id { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PerformedBy { get; set; }
    public bool IsOngoing => EndDate == null;
}
