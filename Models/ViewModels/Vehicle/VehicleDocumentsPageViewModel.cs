// File: Models/ViewModels/Vehicle/VehicleDocumentsPageViewModel.cs
using aractakipsys.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleDocumentsPageViewModel
{
    public int? SelectedVehicleId { get; set; }
    public bool IsAllVehicles { get; set; }
    public int TotalVehicleCount { get; set; }
    public string ActiveTab { get; set; } = "Inspection"; // Inspection, Insurance, Kasko, Tire

    public List<SelectListItem> VehicleSelectList { get; set; } = new();

    public aractakipsys.Models.Entities.Vehicle? SelectedVehicle { get; set; }

    public List<VehicleDocumentItemViewModel> InspectionDocuments { get; set; } = new();
    public List<VehicleDocumentItemViewModel> InsuranceDocuments { get; set; } = new();
    public List<VehicleDocumentItemViewModel> KaskoDocuments { get; set; } = new();
    public List<VehicleTireItemViewModel> TireDocuments { get; set; } = new();

    // Üst özet kutularında aracın en son kaydı gösterilir (süresi dolmuşsa da kırmızı etiketle görünür)
    public VehicleDocumentItemViewModel? CurrentInspection => InspectionDocuments.FirstOrDefault(x => x.IsLatest) ?? InspectionDocuments.FirstOrDefault();
    public VehicleDocumentItemViewModel? CurrentInsurance => InsuranceDocuments.FirstOrDefault(x => x.IsLatest) ?? InsuranceDocuments.FirstOrDefault();
    public VehicleDocumentItemViewModel? CurrentKasko => KaskoDocuments.FirstOrDefault(x => x.IsLatest) ?? KaskoDocuments.FirstOrDefault();
    public VehicleTireItemViewModel? CurrentTire => TireDocuments.FirstOrDefault(x => x.IsCurrentTire);
}
