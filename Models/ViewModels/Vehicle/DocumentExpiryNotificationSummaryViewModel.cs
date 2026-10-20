// File: Models/ViewModels/Vehicle/DocumentExpiryNotificationSummaryViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class DocumentExpiryNotificationSummaryViewModel
{
    public int TotalCount { get; set; }
    public List<DocumentExpiryNotificationItemViewModel> ExpiringVehicles { get; set; } = new();
}

public class DocumentExpiryNotificationItemViewModel
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentDescription { get; set; } = string.Empty;
    public int DaysDiff { get; set; }
    public bool IsExpired => DaysDiff < 0 || Status == VehicleStatus.DocumentExpired;
    public string BadgeText => DaysDiff < 0 
        ? $"{Math.Abs(DaysDiff)} gün geçti" 
        : (DaysDiff == 0 ? "Bugün doluyor!" : $"{DaysDiff} gün kaldı");
    public string BadgeClass => IsExpired ? "badge-danger" : "badge-warning";
    public string IconClass => IsExpired ? "fa-times-circle text-danger" : "fa-clock-o text-warning";
}
