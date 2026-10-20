// File: Models/ViewModels/Driver/DriverDetailsViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Driver;

public class DriverDetailsViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool HasExpiredLicense => LicenseExpiryDate.Date < DateTime.UtcNow.Date;
    public int DaysUntilLicenseExpiry => (LicenseExpiryDate.Date - DateTime.UtcNow.Date).Days;

    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
    public string StatusDisplayName => IsActive ? "Aktif" : "Pasif";

    // Şoförün Şu Anki Aktif Görevi (varsa)
    public DriverActiveAssignmentViewModel? CurrentActiveAssignment { get; set; }

    // Son 5 Görev Geçmişi (ENDPOINT-SPESIFIKASYONU Bölüm 2)
    public List<DriverAssignmentHistoryViewModel> RecentAssignments { get; set; } = new();

    // Toplam İstatistikler
    public int TotalCompletedAssignments { get; set; }
    public int TotalDrivenKm { get; set; }
}

public class DriverActiveAssignmentViewModel
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public int? StartKm { get; set; }
}

public class DriverAssignmentHistoryViewModel
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
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
