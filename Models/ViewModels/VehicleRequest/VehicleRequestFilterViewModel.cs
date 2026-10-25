// File: Models/ViewModels/VehicleRequest/VehicleRequestFilterViewModel.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.VehicleRequest;

public class VehicleRequestFilterViewModel
{
    public VehicleRequestStatus? Status { get; set; } = VehicleRequestStatus.Pending; // Varsayılan bekleyenler
    public VehicleRequestType? RequestType { get; set; } // null: Tümü (geriye uyumluluk için)
    public string? Department { get; set; } // null: Tüm Birimler
    public DateTime? StartDate { get; set; } // Başlangıç Tarihi Filtresi
    public DateTime? EndDate { get; set; } // Bitiş Tarihi Filtresi
    public string? SearchTerm { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public int PendingCount { get; set; }
    public int FulfilledCount { get; set; }
    public int RejectedCount { get; set; }

    public List<string> AvailableDepartments { get; set; } = new();
    public List<VehicleRequestListItemViewModel> Items { get; set; } = new();
}

public class VehicleRequestListItemViewModel
{
    public int Id { get; set; }
    public VehicleRequestType RequestType { get; set; } = VehicleRequestType.OfficialTrip;
    public string RequesterFullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string RequesterEmail { get; set; } = string.Empty;
    public string RequesterPhone { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public VehicleRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? RequestedVehicleType { get; set; }
    public int? PassengerCount { get; set; }
    public DateTime? RequestedStartTime { get; set; }
    public DateTime? RequestedEndTime { get; set; }

    public int? FulfilledAssignmentId { get; set; }
    public string? FulfilledByUserId { get; set; }
    public string? FulfilledByUserName { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ContactFullName { get; set; }
    public string? ContactPhone { get; set; }
}
