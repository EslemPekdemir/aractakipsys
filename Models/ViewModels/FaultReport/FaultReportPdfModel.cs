// File: Models/ViewModels/FaultReport/FaultReportPdfModel.cs
namespace aractakipsys.Models.ViewModels.FaultReport;

public class FaultReportPdfModel
{
    public int Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public string DriverFullName { get; set; } = string.Empty;
    public DateTime? ReportDate { get; set; }
    public int? CurrentKm { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReviewerFullName { get; set; } = string.Empty;
    public bool IsBlank { get; set; } = false;
}
