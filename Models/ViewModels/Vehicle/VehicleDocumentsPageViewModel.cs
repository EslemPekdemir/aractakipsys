// File: Models/ViewModels/Vehicle/VehicleDocumentsPageViewModel.cs
using aractakipsys.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleDocumentsPageViewModel
{
    public int? SelectedVehicleId { get; set; }
    public string ActiveTab { get; set; } = "Inspection"; // Inspection, Insurance, Kasko

    public List<SelectListItem> VehicleSelectList { get; set; } = new();

    public aractakipsys.Models.Entities.Vehicle? SelectedVehicle { get; set; }

    public List<VehicleDocumentItemViewModel> InspectionDocuments { get; set; } = new();
    public List<VehicleDocumentItemViewModel> InsuranceDocuments { get; set; } = new();
    public List<VehicleDocumentItemViewModel> KaskoDocuments { get; set; } = new();
    public List<VehicleTireItemViewModel> TireDocuments { get; set; } = new();

    public VehicleDocumentItemViewModel? CurrentInspection => InspectionDocuments.FirstOrDefault(x => x.IsCurrentValid);
    public VehicleDocumentItemViewModel? CurrentInsurance => InsuranceDocuments.FirstOrDefault(x => x.IsCurrentValid);
    public VehicleDocumentItemViewModel? CurrentKasko => KaskoDocuments.FirstOrDefault(x => x.IsCurrentValid);
    public VehicleTireItemViewModel? CurrentTire => TireDocuments.FirstOrDefault(x => x.IsCurrentTire);
}
