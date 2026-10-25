// File: Common/AppRoles.cs
namespace aractakipsys.Common;

/// <summary>
/// Sistem genelinde kullanılan roller ve rol gruplamaları sabitleri.
/// Merkezi yetkilendirme ve rol kontrolleri buradan yönetilir.
/// </summary>
public static class AppRoles
{
    // Temel Roller
    public const string Admin = "Admin";
    public const string DaireBaskani = "DaireBaskani";
    public const string GenelSekreter = "GenelSekreter";
    public const string Kullanici = "Kullanici"; // Ulaşım Şefi
    public const string Driver = "Driver";       // Şoför

    // Yetki Gruplamaları (Authorization Policy ve Authorize attribute string'leri)
    /// <summary>
    /// Tam yetkili yöneticiler (Admin ve Daire Başkanı)
    /// </summary>
    public const string AdminOrDaireBaskani = "Admin,DaireBaskani";

    /// <summary>
    /// Kullanıcı yönetimi (Daire Başkanı ve Genel Sekreter hesaplarını yalnızca saf Admin oluşturabilir)
    /// </summary>
    public const string SuperAdminOnly = "Admin";

    /// <summary>
    /// Tüm yönetim ve denetim panellerini görebilenler (Genel Sekreter dahil)
    /// </summary>
    public const string ManagementAndAudit = "Admin,DaireBaskani,GenelSekreter";

    /// <summary>
    /// Tüm operasyonel personel ve yöneticiler (Genel Sekreter izleme dahil)
    /// </summary>
    public const string AllStaffAndManagers = "Admin,DaireBaskani,GenelSekreter,Kullanici";

    /// <summary>
    /// Operasyonel değişiklik yapabilen personel (Genel Sekreter hariç)
    /// </summary>
    public const string OperationalStaff = "Admin,DaireBaskani,Kullanici";
}
