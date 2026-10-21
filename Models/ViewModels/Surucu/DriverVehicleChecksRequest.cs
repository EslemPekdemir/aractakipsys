// File: Models/ViewModels/Surucu/DriverVehicleChecksRequest.cs
namespace aractakipsys.Models.ViewModels.Surucu;

public class DriverVehicleChecksRequest
{
    public int AssignmentId { get; set; }
    public bool CheckOil { get; set; }
    public bool CheckWater { get; set; }
    public bool CheckFuel { get; set; }
    public bool CheckHydraulic { get; set; }
    public bool CheckTires { get; set; }
}
