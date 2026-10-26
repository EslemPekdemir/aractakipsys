// File: Common/SystemDefinitionCategories.cs
using System.Globalization;

namespace aractakipsys.Common;

/// <summary>
/// Sistem Tanımları (Dinamik Seçenek Listeleri) kategorilerini tanımlayan sabit beyaz liste.
/// Arayüzden yeni kategori eklenemez; sadece bu kategoriler altındaki seçenekler yönetilir.
/// </summary>
public static class SystemDefinitionCategories
{
    public const string TireSeason = "TireSeason";
    public const string FuelType = "FuelType";
    public const string MaintenanceType = "MaintenanceType";
    public const string VehicleType = "VehicleType";
    public const string LicenseClass = "LicenseClass";

    public static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR");

    public record CategoryMeta(
        string Key, 
        string DisplayName, 
        string Description, 
        string Icon, 
        string TargetTable, 
        string TargetColumn);

    public static readonly IReadOnlyList<CategoryMeta> All = new List<CategoryMeta>
    {
        new(TireSeason, "Lastik Mevsimi", "Araç ve lastik kayıtlarında seçilen mevsim tanımları", "fa-circle-o-notch", "VehicleTires", "Season"),
        new(FuelType, "Yakıt Tipi", "Araç filosu tanımında kullanılan yakıt türleri", "fa-tint", "Vehicles", "FuelType"),
        new(MaintenanceType, "Bakım / Servis Türü", "Periyodik bakım ve arıza onarım işlemlerinde seçilen işlem türleri", "fa-wrench", "Maintenances", "MaintenanceType"),
        new(VehicleType, "Araç Türü (Filo)", "Filonun gövde/tür sınıflandırması (Sedan, Minibüs, vb.)", "fa-car", "Vehicles", "VehicleType"),
        new(LicenseClass, "Ehliyet Sınıfı", "Şoför ehliyet kartlarında tanımlanan sınıflar (B, C, D, vb.)", "fa-id-card", "DriverLicenses", "LicenseClass")
    };

    public static bool IsValidCategory(string categoryKey)
    {
        return All.Any(c => string.Equals(c.Key, categoryKey, StringComparison.OrdinalIgnoreCase));
    }

    public static CategoryMeta? GetMeta(string categoryKey)
    {
        return All.FirstOrDefault(c => string.Equals(c.Key, categoryKey, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Türkçe karakter duyarlı güvenli normalizasyon (boşlukları kırpar, tr-TR ToUpper uygular).
    /// Tekillik kontrolleri ve aramalarda kullanılır.
    /// </summary>
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.Trim().ToUpper(TurkishCulture);
    }
}
