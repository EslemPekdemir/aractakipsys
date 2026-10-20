// File: Models/ViewModels/VehicleRequest/PendingNotificationSummaryViewModel.cs
namespace aractakipsys.Models.ViewModels.VehicleRequest;

public class PendingNotificationSummaryViewModel
{
    public int PendingCount { get; set; }
    public List<PendingNotificationItemViewModel> LatestRequests { get; set; } = new();
}

public class PendingNotificationItemViewModel
{
    public int Id { get; set; }
    public string RequesterFullName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string FormattedDate { get; set; } = string.Empty;
}
