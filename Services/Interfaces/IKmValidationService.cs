// File: Services/Interfaces/IKmValidationService.cs
namespace aractakipsys.Services.Interfaces;

public interface IKmValidationService
{
    int GetDiscrepancyThreshold();

    (bool IsValid, bool NeedsConfirmation, string? Message) ValidateStartKm(
        int startKm, 
        int vehicleCurrentKm, 
        bool isConfirmed = false);

    (bool IsValid, bool NeedsConfirmation, string? Message) ValidateEndKm(
        int endKm, 
        int startKm, 
        bool isConfirmed = false);
}
