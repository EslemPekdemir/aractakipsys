// File: Models/ViewModels/Driver/DriverListItemViewModel.cs
namespace aractakipsys.Models.ViewModels.Driver;

public class DriverListItemViewModel
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

    public bool HasExpiredLicense => LicenseExpiryDate.Date < DateTime.UtcNow.Date;
    public int DaysUntilLicenseExpiry => (LicenseExpiryDate.Date - DateTime.UtcNow.Date).Days;

    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
    public string StatusDisplayName => IsActive ? "Aktif" : "Pasif";
}
