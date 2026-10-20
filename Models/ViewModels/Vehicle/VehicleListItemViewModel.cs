// File: Models/ViewModels/Vehicle/VehicleListItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleListItemViewModel
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public int CurrentKm { get; set; }
    public VehicleStatus Status { get; set; }

    public string StatusBadgeClass => Status switch
    {
        VehicleStatus.Idle => "bg-success",
        VehicleStatus.OnDuty => "bg-primary",
        VehicleStatus.Maintenance => "bg-warning",
        VehicleStatus.OutOfService => "bg-danger",
        VehicleStatus.DocumentExpired => "bg-danger",
        _ => "bg-secondary"
    };

    public string StatusDisplayName => Status switch
    {
        VehicleStatus.Idle => "Boşta",
        VehicleStatus.OnDuty => "Görevde",
        VehicleStatus.Maintenance => "Bakımda",
        VehicleStatus.OutOfService => "Hizmet Dışı",
        VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
        _ => Status.ToString()
    };
}
