// File: Controllers/MaintenanceController.cs
using System.Security.Claims;
using aractakipsys.Models.ViewModels.Maintenance;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

[Authorize(Roles = "Admin,Kullanici")]
[Route("Maintenance")]
public class MaintenanceController : Controller
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceService = maintenanceService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
    }

    /// <summary>
    /// Bakım Listesi (Filtreleme & Sayfalama)
    /// GET: /Maintenance
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] MaintenanceFilterViewModel filter)
    {
        var model = await _maintenanceService.GetMaintenancesPagedAsync(filter);
        return View(model);
    }

    /// <summary>
    /// Bakım Detayı
    /// GET: /Maintenance/Details/{id}
    /// </summary>
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _maintenanceService.GetMaintenanceDetailsAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "İstenen bakım kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Yeni Bakım Kaydı Formu
    /// GET: /Maintenance/Create
    /// </summary>
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var model = await _maintenanceService.GetCreateViewModelAsync();
        return View(model);
    }

    /// <summary>
    /// Yeni Bakım Kaydı Kaydetme
    /// POST: /Maintenance/Create
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var createModel = await _maintenanceService.GetCreateViewModelAsync();
            createModel.VehicleId = model.VehicleId;
            createModel.MaintenanceType = model.MaintenanceType;
            createModel.Description = model.Description;
            createModel.StartDate = model.StartDate;
            createModel.EndDate = model.EndDate;
            createModel.PerformedBy = model.PerformedBy;
            return View(createModel);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message, maintenanceId) = await _maintenanceService.CreateMaintenanceAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var createModel = await _maintenanceService.GetCreateViewModelAsync();
            createModel.VehicleId = model.VehicleId;
            createModel.MaintenanceType = model.MaintenanceType;
            createModel.Description = model.Description;
            createModel.StartDate = model.StartDate;
            createModel.EndDate = model.EndDate;
            createModel.PerformedBy = model.PerformedBy;
            return View(createModel);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Bakım Düzenleme Formu
    /// GET: /Maintenance/Edit/{id}
    /// </summary>
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _maintenanceService.GetEditViewModelAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Düzenlenmek istenen bakım kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Bakım Güncelleme
    /// POST: /Maintenance/Edit/{id}
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MaintenanceEditViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Geçersiz istek.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var editModel = await _maintenanceService.GetEditViewModelAsync(id);
            if (editModel != null)
            {
                model.AvailableVehicles = editModel.AvailableVehicles;
                model.VehiclePlate = editModel.VehiclePlate;
                model.VehicleBrandModel = editModel.VehicleBrandModel;
            }
            return View(model);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message) = await _maintenanceService.UpdateMaintenanceAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var editModel = await _maintenanceService.GetEditViewModelAsync(id);
            if (editModel != null)
            {
                model.AvailableVehicles = editModel.AvailableVehicles;
                model.VehiclePlate = editModel.VehiclePlate;
                model.VehicleBrandModel = editModel.VehicleBrandModel;
            }
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Devam Eden Bakımı Tamamlama
    /// POST: /Maintenance/Complete/{id}
    /// </summary>
    [HttpPost("Complete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, [FromForm] DateTime endDate, [FromForm] string? performedBy)
    {
        var adminUserId = GetCurrentUserId();
        var (success, message) = await _maintenanceService.CompleteMaintenanceAsync(id, endDate, performedBy, adminUserId);

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
    /// Bakım Kaydı Silme (Soft Delete)
    /// POST: /Maintenance/Delete/{id}
    /// </summary>
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var adminUserId = GetCurrentUserId();
        var (success, message) = await _maintenanceService.DeleteMaintenanceAsync(id, adminUserId);

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
}
