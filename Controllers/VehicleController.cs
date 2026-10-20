// File: Controllers/VehicleController.cs
using System.Security.Claims;
using aractakipsys.Models.ViewModels.Vehicle;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

[Authorize(Roles = "Admin,Kullanici")]
[Route("Vehicle")]
public class VehicleController : Controller
{
    private readonly IVehicleService _vehicleService;
    private readonly ILogger<VehicleController> _logger;

    public VehicleController(IVehicleService vehicleService, ILogger<VehicleController> logger)
    {
        _vehicleService = vehicleService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
    }

    /// <summary>
    /// Araç Listesi (Filtreleme & Sayfalama)
    /// GET: /Vehicle
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] VehicleFilterViewModel filter)
    {
        var model = await _vehicleService.GetVehiclesPagedAsync(filter);
        return View(model);
    }

    /// <summary>
    /// Araç Detayı + Son 5 Görev + Bakım Geçmişi
    /// GET: /Vehicle/Details/{id}
    /// </summary>
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _vehicleService.GetVehicleDetailsAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "İstenen araç kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Yeni Araç Ekleme Formu
    /// GET: /Vehicle/Create
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new VehicleCreateViewModel());
    }

    /// <summary>
    /// Yeni Araç Kaydetme
    /// POST: /Vehicle/Create
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(" | ", ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}"));
            _logger.LogWarning("Araç ekleme ModelState doğrulama hatası: {Errors}", errors);
            return View(viewModel);
        }

        var userId = GetCurrentUserId();
        var (success, message, vehicleId) = await _vehicleService.CreateVehicleAsync(viewModel, userId);

        if (!success)
        {
            ModelState.AddModelError(nameof(viewModel.PlateNumber), message);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Araç Düzenleme Formu
    /// GET: /Vehicle/Edit/{id}
    /// </summary>
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _vehicleService.GetVehicleEditModelAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Düzenlenmek istenen araç bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Araç Güncelleme
    /// POST: /Vehicle/Edit/{id}
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            _logger.LogWarning("Araç düzenleme güvenlik uyuşmazlığı: URL ID={RouteId}, Model ID={ModelId}", id, viewModel.Id);
            TempData["ErrorMessage"] = "Geçersiz istek: Araç kimlik doğrulaması eşleşmedi.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join(" | ", ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}"));
            _logger.LogWarning("Araç düzenleme ModelState doğrulama hatası (Araç ID: {Id}): {Errors}", id, errors);
            return View(viewModel);
        }

        var userId = GetCurrentUserId();
        var (success, message) = await _vehicleService.UpdateVehicleAsync(viewModel, userId);

        if (!success)
        {
            _logger.LogWarning("Araç güncellenemedi (Araç ID: {Id}): {Message}", id, message);
            ModelState.AddModelError(string.Empty, message);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Araç Silme (Soft Delete)
    /// POST: /Vehicle/Delete/{id}
    /// </summary>
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var (success, message) = await _vehicleService.DeleteVehicleAsync(id, userId);

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
    /// Navbar Çanı Bildirimi: 30 gün içinde süresi dolacak ve süresi dolmuş araçların özeti
    /// GET: /Vehicle/ExpiringDocumentSummary
    /// </summary>
    [HttpGet("ExpiringDocumentSummary")]
    public async Task<IActionResult> ExpiringDocumentSummary()
    {
        var summary = await _vehicleService.GetExpiringDocumentsSummaryAsync();
        return Json(summary);
    }
}
