// File: Models/ViewModels/Assignment/SignedDocumentArchiveViewModel.cs
using System;
using System.Collections.Generic;
using aractakipsys.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Assignment;

public enum SignedDocumentFilterType
{
    Uploaded = 1,   // Belgesi Yüklenenler (Varsayılan Arşiv)
    Missing = 2,    // Belgesi Eksik Olanlar (Evrak Takibi)
    All = 3         // Tüm Görevler
}

public class SignedDocumentArchiveFilterViewModel
{
    public SignedDocumentFilterType DocumentFilter { get; set; } = SignedDocumentFilterType.Uploaded;
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class SignedDocumentItemViewModel
{
    public int AssignmentId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string DriverFullName { get; set; } = string.Empty;
    public string RequestedByDepartment { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime PlannedStartTime { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public AssignmentStatus Status { get; set; }
    public bool HasDocument => !string.IsNullOrWhiteSpace(DocumentPath);
    public string? DocumentOriginalFileName { get; set; }
    public string? DocumentPath { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? UploadedByFullName { get; set; }
    public string? FileExtension => !string.IsNullOrEmpty(DocumentOriginalFileName) 
        ? System.IO.Path.GetExtension(DocumentOriginalFileName).ToLowerInvariant() 
        : null;

    public bool IsPdf => string.Equals(FileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);
    public bool IsImage => string.Equals(FileExtension, ".png", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(FileExtension, ".jpg", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(FileExtension, ".jpeg", StringComparison.OrdinalIgnoreCase);

    public string StatusBadgeClass => Status switch
    {
        AssignmentStatus.Planned => "badge-secondary",
        AssignmentStatus.InProgress => "badge-primary",
        AssignmentStatus.Completed => "badge-success",
        AssignmentStatus.Cancelled => "badge-danger",
        _ => "badge-secondary"
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

public class SignedDocumentArchiveViewModel
{
    public SignedDocumentArchiveFilterViewModel Filter { get; set; } = new();
    public List<SignedDocumentItemViewModel> Items { get; set; } = new();

    // KPI Sayaçları
    public int TotalUploadedCount { get; set; }
    public int TotalMissingCount { get; set; }
    public int TotalAssignmentsCount { get; set; }
    public int UploadedThisMonthCount { get; set; }

    public int UploadRatePercentage => TotalAssignmentsCount > 0 
        ? (int)Math.Round((double)TotalUploadedCount * 100 / TotalAssignmentsCount) 
        : 0;

    // Filtre Dropdown Listeleri
    public List<SelectListItem> VehicleList { get; set; } = new();
    public List<SelectListItem> DriverList { get; set; } = new();
}
