// File: Filters/ReadOnlyRoleFilter.cs
using aractakipsys.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace aractakipsys.Filters;

/// <summary>
/// GenelSekreter rolündeki kullanıcılar için salt okunur kalkanı.
/// POST, PUT, DELETE, PATCH isteklerini merkezi olarak denetler ve engeller.
/// Giriş, çıkış ve şifre değiştirme gibi zorunlu kimlik eylemleri istisnadır.
/// </summary>
public class ReadOnlyRoleFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> AllowedActionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Login",
        "Logout",
        "Profile",
        "ChangePassword",
        "ForgotPassword",
        "ResetPassword"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        // Kullanıcı giriş yapmamışsa veya GenelSekreter değilse normal akışa devam et
        if (user.Identity?.IsAuthenticated != true || !user.IsReadOnlyUser())
        {
            await next();
            return;
        }

        var method = httpContext.Request.Method.ToUpperInvariant();

        // GET ve HEAD istekleri her zaman serbesttir (okuma)
        if (method == "GET" || method == "HEAD" || method == "OPTIONS")
        {
            await next();
            return;
        }

        // İstisna Action kontrolü (Örn: /Account/Logout, /Account/ChangePassword)
        var actionName = context.ActionDescriptor.RouteValues["action"] ?? string.Empty;
        var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? string.Empty;

        if (string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase) &&
            AllowedActionNames.Contains(actionName))
        {
            await next();
            return;
        }

        // POST/PUT/PATCH/DELETE istekleri için engelleme:
        var isAjaxOrJson = httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                           httpContext.Request.Headers.Accept.ToString().Contains("application/json") ||
                           httpContext.Request.ContentType?.Contains("application/json") == true;

        if (isAjaxOrJson)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                isReadOnly = true,
                message = "Bu hesap 'Genel Sekreter' (Salt Okunur) yetkisine sahiptir. Sistem üzerinde herhangi bir ekleme, düzenleme, onay veya silme işlemi yapılamaz."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Klasik form submit ise kullanıcıya anlaşılır hata mesajıyla geriye veya yönlendirilen sayfaya dön
        var controller = context.Controller as Controller;
        if (controller != null)
        {
            controller.TempData["ErrorMessage"] = "Yetkisiz İşlem: 'Genel Sekreter' rolü salt okunur yetkiye sahiptir. Değişiklik yapma, onaylama veya silme yetkiniz bulunmamaktadır.";
        }

        var referer = httpContext.Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            context.Result = new RedirectResult(referer);
        }
        else
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
