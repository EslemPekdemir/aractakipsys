// File: Controllers/SurucuController.cs
using System.Security.Claims;
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels;
using aractakipsys.Models.ViewModels.Surucu;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

[Authorize(Roles = "Driver")]
[Route("Surucu")]
public class SurucuController : Controller
{
    private readonly IDriverService _driverService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SurucuController> _logger;

    public SurucuController(
        IDriverService driverService,
        UserManager<ApplicationUser> userManager,
        ILogger<SurucuController> logger)
    {
        _driverService = driverService;
        _userManager = userManager;
        _logger = logger;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Şoför Ana Ekranı - Görev Kartları Listesi
    /// GET: /Surucu
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    [HttpGet("MyAssignments")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var model = await _driverService.GetDriverAssignmentsAsync(userId);
        return View(model);
    }

    /// <summary>
    /// Şoför Görev Detay Ekranı
    /// GET: /Surucu/Detay/{id}
    /// </summary>
    [HttpGet("Detay/{id:int}")]
    public async Task<IActionResult> Detay(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var model = await _driverService.GetAssignmentDetailAsync(id, userId);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Görev kaydı bulunamadı veya bu göreve erişim yetkiniz bulunmamaktadır.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Görevi Başlat (StartAssignment) AJAX Uç Noktası
    /// POST: /Surucu/Baslat/{id}
    /// </summary>
    [HttpPost("Baslat/{id:int}")]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> StartAssignment(int id, [FromBody] StartAssignmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Geçersiz veri gönderildi.";

            return Json(AjaxResultViewModel.Fail(firstError));
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(AjaxResultViewModel.Fail("Oturum süreniz dolmuş. Lütfen tekrar giriş yapınız."));
        }

        var result = await _driverService.StartAssignmentAsync(id, userId, request);
        return Json(result);
    }

    /// <summary>
    /// Görevi Bitir (EndAssignment) AJAX Uç Noktası
    /// POST: /Surucu/Bitir/{id}
    /// </summary>
    [HttpPost("Bitir/{id:int}")]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> EndAssignment(int id, [FromBody] EndAssignmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Geçersiz veri gönderildi.";

            return Json(AjaxResultViewModel.Fail(firstError));
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(AjaxResultViewModel.Fail("Oturum süreniz dolmuş. Lütfen tekrar giriş yapınız."));
        }

        var result = await _driverService.EndAssignmentAsync(id, userId, request);
        return Json(result);
    }

    /// <summary>
    /// Şoförün Gece Vardiyası / Acil Durumda Kendine Görev Atayıp Başlatması (AJAX)
    /// POST: /Surucu/AcilGorevBaslat
    /// </summary>
    [HttpPost("AcilGorevBaslat")]
    [ValidateAntiForgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> EmergencySelfAssignment([FromBody] EmergencySelfAssignmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Lütfen formu eksiksiz doldurunuz.";

            return Json(AjaxResultViewModel.Fail(firstError));
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(AjaxResultViewModel.Fail("Oturum süreniz dolmuş. Lütfen tekrar giriş yapınız."));
        }

        var result = await _driverService.CreateSelfAssignmentAsync(userId, request);
        return Json(result);
    }

    /// <summary>
    /// Şoför Şifre Değiştirme Ekranı
    /// GET: /Surucu/ChangePassword
    /// </summary>
    [HttpGet("ChangePassword")]
    public IActionResult ChangePassword()
    {
        return View(new DriverChangePasswordViewModel());
    }

    /// <summary>
    /// Şoför Şifre Değiştirme Eylemi
    /// POST: /Surucu/ChangePassword
    /// </summary>
    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(DriverChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        _logger.LogInformation("Şoför şifresini başarıyla güncelledi: {UserName}", user.UserName);
        TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi. Yeni şifreniz kaydedilmiştir.";
        return RedirectToAction(nameof(Index));
    }
}
