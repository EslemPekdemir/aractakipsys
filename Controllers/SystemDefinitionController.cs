// File: Controllers/SystemDefinitionController.cs
using System.Security.Claims;
using aractakipsys.Common;
using aractakipsys.Models.ViewModels.SystemDefinition;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

/// <summary>
/// Sistem Tanımları (Dinamik Seçenek Listeleri) Yönetim Denetleyicisi
/// Admin ve Daire Başkanı düzenleyebilir; Genel Sekreter salt okunur izler; Şef ve Şoför erişemez.
/// </summary>
[Authorize(Roles = AppRoles.ManagementAndAudit)]
[Route("SystemDefinition")]
public class SystemDefinitionController : Controller
{
    private readonly ISystemDefinitionService _systemDefinitionService;
    private readonly ILogger<SystemDefinitionController> _logger;

    public SystemDefinitionController(
        ISystemDefinitionService systemDefinitionService,
        ILogger<SystemDefinitionController> logger)
    {
        _systemDefinitionService = systemDefinitionService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
    }

    /// <summary>
    /// Sistem Tanımları Ana Sayfası (Kategori Bazlı Seçenek Listesi)
    /// GET: /SystemDefinition?category=TireSeason veya ?categoryKey=TireSeason
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? category, string? categoryKey)
    {
        var targetCategory = categoryKey ?? category;
        var canManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.DaireBaskani);
        var isReadOnly = User.IsInRole(AppRoles.GenelSekreter);
        var model = await _systemDefinitionService.GetPageDataAsync(targetCategory, canManage, isReadOnly);
        return View(model);
    }

    /// <summary>
    /// Yeni Seçenek Ekleme
    /// POST: /SystemDefinition/Create
    /// </summary>
    [Authorize(Roles = AppRoles.AdminOrDaireBaskani)]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SystemDefinitionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errors) ? "Lütfen form alanlarını doğru doldurunuz." : errors;
            return RedirectToAction(nameof(Index), new { category = model.CategoryKey });
        }

        var userId = GetCurrentUserId();
        var (success, message, _) = await _systemDefinitionService.CreateAsync(model.CategoryKey, model.Name, model.DisplayOrder, userId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index), new { category = model.CategoryKey });
    }

    /// <summary>
    /// Seçenek Adı Düzenleme (Yalnızca kullanımda olmayan ve sistem varsayılanı olmayan seçenekler için)
    /// POST: /SystemDefinition/EditName
    /// </summary>
    [Authorize(Roles = AppRoles.AdminOrDaireBaskani)]
    [HttpPost("EditName")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditName(SystemDefinitionEditNameViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errors) ? "Lütfen form alanlarını doğru doldurunuz." : errors;
            return RedirectToAction(nameof(Index), new { category = model.CategoryKey });
        }

        var userId = GetCurrentUserId();
        var (success, message) = await _systemDefinitionService.UpdateNameAsync(model.Id, model.NewName, userId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index), new { category = model.CategoryKey });
    }

    /// <summary>
    /// Seçenek Aktif/Pasif Durumu Değiştirme
    /// POST: /SystemDefinition/ToggleStatus veya /SystemDefinition/ToggleStatus/{id}
    /// </summary>
    [Authorize(Roles = AppRoles.AdminOrDaireBaskani)]
    [HttpPost("ToggleStatus")]
    [HttpPost("ToggleStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, string? categoryKey, string? category)
    {
        var targetCategory = categoryKey ?? category;
        var userId = GetCurrentUserId();
        var (success, message) = await _systemDefinitionService.ToggleStatusAsync(id, userId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index), new { category = targetCategory });
    }

    /// <summary>
    /// Seçenek Silme (Yalnızca kullanımda olmayan ve sistem varsayılanı olmayan seçenekler için)
    /// POST: /SystemDefinition/Delete veya /SystemDefinition/Delete/{id}
    /// </summary>
    [Authorize(Roles = AppRoles.AdminOrDaireBaskani)]
    [HttpPost("Delete")]
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? categoryKey, string? category)
    {
        var targetCategory = categoryKey ?? category;
        var userId = GetCurrentUserId();
        var (success, message) = await _systemDefinitionService.DeleteAsync(id, userId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index), new { category = targetCategory });
    }

    /// <summary>
    /// Dinamik Dropdown Verisi İçin Aktif Seçenek Listesi
    /// GET: /SystemDefinition/GetActiveOptions?category=TireSeason veya ?categoryKey=TireSeason
    /// </summary>
    [HttpGet("GetActiveOptions")]
    public async Task<IActionResult> GetActiveOptions(string? category, string? categoryKey)
    {
        var target = categoryKey ?? category ?? string.Empty;
        var options = await _systemDefinitionService.GetActiveOptionNamesAsync(target);
        return Json(options);
    }
}
