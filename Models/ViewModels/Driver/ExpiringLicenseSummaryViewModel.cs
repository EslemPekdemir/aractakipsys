// File: Models/ViewModels/Driver/ExpiringLicenseSummaryViewModel.cs
namespace aractakipsys.Models.ViewModels.Driver;

public class ExpiringLicenseSummaryViewModel
{
    public int TotalCount { get; set; }
    public List<ExpiringLicenseItemViewModel> ExpiringLicenses { get; set; } = new();
}

public class ExpiringLicenseItemViewModel
{
    public int DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string LicenseClass { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired => DaysRemaining < 0;
    public string BadgeText => IsExpired ? $"{Math.Abs(DaysRemaining)} gün geçti" : (DaysRemaining == 0 ? "Bugün doluyor!" : $"{DaysRemaining} gün kaldı");
    public string BadgeClass => IsExpired ? "badge-danger" : "badge-warning";
}
