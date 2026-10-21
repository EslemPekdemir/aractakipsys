// File: Services/Implementations/KmValidationService.cs
using aractakipsys.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace aractakipsys.Services.Implementations;

public class KmValidationService : IKmValidationService
{
    private readonly int _discrepancyThreshold;

    public KmValidationService(IConfiguration configuration)
    {
        // appsettings.json'dan eşik değerini oku; varsayılan 1000 KM
        _discrepancyThreshold = configuration.GetValue<int>("VehicleSettings:KmDiscrepancyThreshold", 1000);
    }

    public int GetDiscrepancyThreshold() => _discrepancyThreshold;

    public (bool IsValid, bool NeedsConfirmation, string? Message) ValidateStartKm(
        int startKm, 
        int vehicleCurrentKm, 
        bool isConfirmed = false)
    {
        if (startKm < vehicleCurrentKm)
        {
            return (false, false, $"Başlangıç kilometresi ({startKm:N0} KM), aracın güncel kilometresinden ({vehicleCurrentKm:N0} KM) küçük olamaz.");
        }

        var diff = startKm - vehicleCurrentKm;
        if (diff > _discrepancyThreshold && !isConfirmed)
        {
            return (false, true, $"Girilen başlangıç kilometresi ({startKm:N0} KM), aracın mevcut kilometresinden ({vehicleCurrentKm:N0} KM) belirgin biçimde ({diff:N0} KM) büyüktür. Bu işlemi gerçekleştirmek için açık onay vermeniz gerekmektedir.");
        }

        return (true, false, null);
    }

    public (bool IsValid, bool NeedsConfirmation, string? Message) ValidateEndKm(
        int endKm, 
        int startKm, 
        bool isConfirmed = false)
    {
        if (endKm < startKm)
        {
            return (false, false, $"Bitiş kilometresi ({endKm:N0} KM), başlangıç kilometresinden ({startKm:N0} KM) küçük olamaz.");
        }

        var diff = endKm - startKm;
        if (diff > _discrepancyThreshold && !isConfirmed)
        {
            return (false, true, $"Girilen bitiş kilometresi ({endKm:N0} KM), başlangıç kilometresinden ({startKm:N0} KM) belirgin biçimde ({diff:N0} KM) büyüktür. Bu işlemi gerçekleştirmek için açık onay vermeniz gerekmektedir.");
        }

        return (true, false, null);
    }
}
