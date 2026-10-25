// File: Models/ViewModels/Vehicle/VehicleDocumentItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleDocumentItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public VehicleDocumentType DocumentType { get; set; }
    public string DocumentTypeName => DocumentType switch
    {
        VehicleDocumentType.Inspection => "Fenni Muayene",
        VehicleDocumentType.Insurance => "Trafik Sigortası",
        VehicleDocumentType.Kasko => "Kasko",
        _ => DocumentType.ToString()
    };

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Cost { get; set; }
    public string? PolicyOrStationNumber { get; set; }
    public string? CompanyName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsExpired => EndDate.Date < DateTime.Today;
    public int DaysRemaining => (int)(EndDate.Date - DateTime.Today).TotalDays;
    public bool IsExpiringSoon => !IsExpired && DaysRemaining <= 30;

    public bool IsCurrentValid { get; set; }
}
