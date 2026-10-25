// File: Common/ClaimsPrincipalExtensions.cs
using System.Security.Claims;

namespace aractakipsys.Common;

/// <summary>
/// ClaimsPrincipal (User nesnesi) için merkezi rol ve yetki genişletme metotları.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin);

    public static bool IsDaireBaskani(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.DaireBaskani);

    public static bool IsGenelSekreter(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.GenelSekreter);

    public static bool IsKullanici(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Kullanici);

    public static bool IsDriver(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Driver);

    /// <summary>
    /// Admin veya Daire Başkanı (Tam yönetici yetkisi)
    /// </summary>
    public static bool IsAdminOrDaireBaskani(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.DaireBaskani);

    /// <summary>
    /// Genel Sekreter veya salt okunur rol kontrolü
    /// </summary>
    public static bool IsReadOnlyUser(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.GenelSekreter);

    /// <summary>
    /// Veri değiştirme yetkisi var mı? (Genel Sekreter için false döner)
    /// </summary>
    public static bool CanWriteData(this ClaimsPrincipal user) =>
        !user.IsReadOnlyUser();
}
