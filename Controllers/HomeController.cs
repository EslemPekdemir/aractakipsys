// File: Controllers/HomeController.cs
using System.Diagnostics;
using System.Security.Claims;
using aractakipsys.Models;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IDashboardService dashboardService,
        ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Admin Dashboard (Filo Özeti, KPI Kartları, Aktif Görevler, Bakımlar, Grafikler)
    /// GET: / or /Home or /Dashboard
    /// </summary>
    [HttpGet("")]
    [HttpGet("Home")]
    [HttpGet("Home/Index")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Index()
    {
        // Eğer giriş yapan kullanıcı Şoför ise ve Admin ya da yetkili rollerden biri değilse doğrudan şoför portalına yönlendir
        if (User.IsInRole("Driver") && !User.IsInRole("Admin") && !User.IsInRole("Kullanici") && !User.IsInRole("DaireBaskani") && !User.IsInRole("GenelSekreter"))
        {
            return RedirectToAction("Index", "Surucu");
        }

        var model = await _dashboardService.GetDashboardMetricsAsync();
        model.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return View(model);
    }

    /// <summary>
    /// Dashboard Hızlı Plaka Arama (AJAX)
    /// GET: /Home/SearchPlate?plate=...
    /// Rol: Admin, Kullanici (Şoförler erişemez)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = aractakipsys.Common.AppRoles.AllStaffAndManagers)]
    public async Task<IActionResult> SearchPlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            return Json(new { success = false, message = "Lütfen sorgulamak istediğiniz plakayı giriniz." });
        }

        var result = await _dashboardService.SearchPlateAsync(plate);
        if (result == null)
        {
            return Json(new { success = false, message = "Bu plakaya kayıtlı araç yok" });
        }

        return Json(new { success = true, data = result });
    }

    /// <summary>
    /// Dashboard Aylık Takvim Verisi (AJAX)
    /// GET: /Home/CalendarData?year=2026&month=10
    /// Rol: Admin, DaireBaskani, GenelSekreter, Kullanici (Şoförler erişemez)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = aractakipsys.Common.AppRoles.AllStaffAndManagers)]
    public async Task<IActionResult> CalendarData(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Today.Year;
        var targetMonth = month ?? DateTime.Today.Month;

        var data = await _dashboardService.GetCalendarMonthDataAsync(targetYear, targetMonth);
        return Json(new { success = true, data = data });
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
