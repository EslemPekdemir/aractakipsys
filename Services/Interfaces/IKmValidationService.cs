// File: Services/Interfaces/IKmValidationService.cs
namespace aractakipsys.Services.Interfaces;

public interface IKmValidationService
{
    int GetDiscrepancyThreshold();
    int GetTripDistanceThreshold();

    (bool IsValid, bool NeedsConfirmation, string? Message) ValidateStartKm(
        int startKm, 
        int vehicleCurrentKm, 
        bool isConfirmed = false);

    (bool IsValid, bool NeedsConfirmation, string? Message) ValidateEndKm(
        int endKm, 
        int startKm, 
        bool isConfirmed = false);

    (bool IsWarning, int Distance, string? WarningMessage) CheckTripDistanceWarning(
        int startKm, 
        int endKm);
}
