// File: Controllers/DriverController.cs
using System.Security.Claims;
using aractakipsys.Models.ViewModels.Driver;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using aractakipsys.Common;

namespace aractakipsys.Controllers;

[Authorize(Roles = AppRoles.AllStaffAndManagers)]
[Route("Driver")]
public class DriverController : Controller
{
    private readonly IDriverService _driverService;
    private readonly ILogger<DriverController> _logger;

    public DriverController(IDriverService driverService, ILogger<DriverController> logger)
    {
        _driverService = driverService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
    }

    /// <summary>
    /// Şoför Listesi (Filtreleme & Sayfalama)
    /// GET: /Driver
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] DriverFilterViewModel filter)
    {
        var model = await _driverService.GetDriversPagedAsync(filter);
        return View(model);
    }

    /// <summary>
    /// Şoför Detayı + Son 5 Görev
    /// GET: /Driver/Details/{id}
    /// </summary>
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _driverService.GetDriverDetailsAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "İstenen şoför kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Yeni Şoför Ekleme Formu
    /// GET: /Driver/Create
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new DriverCreateViewModel());
    }

    /// <summary>
    /// Yeni Şoför Kaydetme (Transaction ile Identity + Driver)
    /// POST: /Driver/Create
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DriverCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message, driverId) = await _driverService.CreateDriverAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Şoför Düzenleme Formu
    /// GET: /Driver/Edit/{id}
    /// </summary>
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _driverService.GetDriverEditModelAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Düzenlenmek istenen şoför bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        model.CanEditDepartment = User.IsInRole("Admin") || User.IsInRole("DaireBaskani");
        return View(model);
    }

    /// <summary>
    /// Şoför Güncelleme
    /// POST: /Driver/Edit/{id}
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DriverEditViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Geçersiz istek.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model.CanEditDepartment = User.IsInRole("Admin") || User.IsInRole("DaireBaskani");
            return View(model);
        }

        var adminUserId = GetCurrentUserId();
        bool isAdmin = User.IsInRole("Admin") || User.IsInRole("DaireBaskani");
        var (success, message) = await _driverService.UpdateDriverAsync(model, adminUserId, isAdmin);

        if (!success)
        {
            model.CanEditDepartment = isAdmin;
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Şoför Durumu Değiştirme (Aktif / Pasif)
    /// POST: /Driver/ToggleStatus/{id}
    /// </summary>
    [HttpPost("ToggleStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var adminUserId = GetCurrentUserId();
        var (success, message) = await _driverService.ToggleDriverStatusAsync(id, adminUserId);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Şoför Silme (Soft Delete)
    /// POST: /Driver/Delete/{id}
    /// </summary>
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var adminUserId = GetCurrentUserId();
        var (success, message) = await _driverService.DeleteDriverAsync(id, adminUserId);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Şoför Şifresini Yönetici Tarafından Sıfırlama
    /// POST: /Driver/ResetPassword/{id}
    /// </summary>
    [HttpPost("ResetPassword/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 10)
        {
            TempData["ErrorMessage"] = "Yeni şifre en az 10 karakter olmalı ve boş bırakılamaz.";
            return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Details), new { id });
        }

        var adminUserId = GetCurrentUserId();
        var (success, message) = await _driverService.ResetDriverPasswordAsync(id, newPassword, adminUserId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Navbar Çanı / Bildirim Özeti: 30 gün içinde süresi dolacak ve süresi dolmuş ehliyetler
    /// GET: /Driver/ExpiringLicenseSummary
    /// </summary>
    [HttpGet("ExpiringLicenseSummary")]
    public async Task<IActionResult> ExpiringLicenseSummary()
    {
        var summary = await _driverService.GetExpiringLicensesSummaryAsync();
        return Json(summary);
    }
}
