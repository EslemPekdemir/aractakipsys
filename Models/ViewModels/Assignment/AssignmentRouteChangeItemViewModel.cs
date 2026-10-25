// File: Models/ViewModels/Assignment/AssignmentRouteChangeItemViewModel.cs
using System;

namespace aractakipsys.Models.ViewModels.Assignment;

public class AssignmentRouteChangeItemViewModel
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string? OldRoute { get; set; }
    public string NewRoute { get; set; } = string.Empty;
    public string? OldDestination { get; set; }
    public string? NewDestination { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ChangedByUserId { get; set; } = string.Empty;
    public string ChangedByFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
