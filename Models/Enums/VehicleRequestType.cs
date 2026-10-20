// File: Models/Enums/VehicleRequestType.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Enums;

public enum VehicleRequestType
{
    [Display(Name = "Resmi Görev / Heyet")]
    OfficialTrip = 0,

    [Display(Name = "Arıza / Tamir & Bakım")]
    MaintenanceRepair = 1
}
