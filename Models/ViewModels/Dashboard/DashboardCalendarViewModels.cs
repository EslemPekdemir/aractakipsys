// File: Models/ViewModels/Dashboard/DashboardCalendarViewModels.cs
using System;
using System.Collections.Generic;

namespace aractakipsys.Models.ViewModels.Dashboard;

public class DashboardCalendarDaySummaryDto
{
    public string Date { get; set; } = string.Empty; // "yyyy-MM-dd"
    public int Day { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public int ActiveVehicleCount { get; set; } // O gün göreve çıkan / planlı araç sayısı
    public int MaintenanceVehicleCount { get; set; } // O gün bakımda olan araç sayısı
    public int ExpiringDocumentCount { get; set; } // O gün süresi dolan muayene/sigorta/kasko/lastik sayısı
    public int ExpiringLicenseCount { get; set; } // O gün süresi dolan ehliyet sayısı
}

public class DashboardCalendarAssignmentItemDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string DriverNames { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class DashboardCalendarMaintenanceItemDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}

public class DashboardCalendarDocumentItemDto
{
    public int VehicleId { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string VehicleBrandModel { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty; // Muayene, Trafik Sigortası, Kasko, Lastik
    public string ExpiryDate { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class DashboardCalendarLicenseItemDto
{
    public int DriverId { get; set; }
    public string DriverFullName { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class DashboardCalendarDayDetailDto
{
    public string Date { get; set; } = string.Empty; // "yyyy-MM-dd"
    public string FormattedDate { get; set; } = string.Empty; // "25 Ekim 2026, Pazar"
    public List<DashboardCalendarAssignmentItemDto> Assignments { get; set; } = new();
    public List<DashboardCalendarMaintenanceItemDto> Maintenances { get; set; } = new();
    public List<DashboardCalendarDocumentItemDto> ExpiringDocuments { get; set; } = new();
    public List<DashboardCalendarLicenseItemDto> ExpiringLicenses { get; set; } = new();
}

public class DashboardCalendarMonthResponseDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty; // "Ekim 2026"
    public List<DashboardCalendarDaySummaryDto> Days { get; set; } = new();
    public Dictionary<string, DashboardCalendarDayDetailDto> DetailsByDate { get; set; } = new();
}
