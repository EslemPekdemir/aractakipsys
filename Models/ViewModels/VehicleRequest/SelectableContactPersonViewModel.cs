// File: Models/ViewModels/VehicleRequest/SelectableContactPersonViewModel.cs
namespace aractakipsys.Models.ViewModels.VehicleRequest;

public class SelectableContactPersonViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    public string DisplayText => string.IsNullOrWhiteSpace(PhoneNumber)
        ? $"{FullName} ({RoleName ?? "Personel"}) - {Email}"
        : $"{FullName} ({RoleName ?? "Personel"}) - Tel: {PhoneNumber}";
}
