// File: Controllers/VehicleRequestController.cs
using System.Security.Claims;
using System.Text.RegularExpressions;
using aractakipsys.Models.ViewModels.VehicleRequest;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace aractakipsys.Controllers;

/// <summary>
/// Araç Talebi Kontrolcüsü.
/// Talep oluşturma formu public (AllowAnonymous), yönetim işlemleri (Index, Fulfill, Reject) Admin ve Kullanici rollerine açıktır.
/// </summary>
[Authorize(Roles = "Admin,Kullanici")]
[Route("VehicleRequest")]
public class VehicleRequestController : Controller
{
    private readonly IVehicleRequestService _vehicleRequestService;
    private readonly ILogger<VehicleRequestController> _logger;

    private static readonly Regex DuzceEmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@duzce\.edu\.tr$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public VehicleRequestController(
        IVehicleRequestService vehicleRequestService,
        ILogger<VehicleRequestController> logger)
    {
        _vehicleRequestService = vehicleRequestService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
    }

    #region Public Talep Formu (AllowAnonymous)

    /// <summary>
    /// Araç Talep Formu (GET: /AracTalebi veya /VehicleRequest/Create)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/AracTalebi")]
    [HttpGet("/AracTalebi/Create")]
    [HttpGet("Create")]
    [HttpGet("/Talep")]
    public IActionResult Create()
    {
        return View(new VehicleRequestCreateViewModel());
    }

    /// <summary>
    /// Araç Talep Gönderimi (POST: /AracTalebi veya /VehicleRequest/Create)
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("vehicle-request-limit")]
    [HttpPost("/AracTalebi")]
    [HttpPost("/AracTalebi/Create")]
    [HttpPost("Create")]
    [HttpPost("/Talep")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleRequestCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Server-side @duzce.edu.tr domain kontrolü (hem EndsWith hem Regex ile iki aşamalı doğrulama)
        var email = model.RequesterEmail?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || 
            !email.EndsWith("@duzce.edu.tr", StringComparison.OrdinalIgnoreCase) || 
            !DuzceEmailRegex.IsMatch(email))
        {
            ModelState.AddModelError(nameof(model.RequesterEmail), "E-posta adresi @duzce.edu.tr uzantılı geçerli bir kurumsal adres olmalıdır.");
            return View(model);
        }

        var result = await _vehicleRequestService.CreateRequestAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Talebiniz alındı, Şef tarafından değerlendirilecek";
        return RedirectToAction(nameof(Success), new { id = result.RequestId });
    }

    /// <summary>
    /// Talep Başarı / Onay Bildirim Sayfası
    /// GET: /AracTalebi/Success/{id} veya /VehicleRequest/Success/{id}
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/AracTalebi/Success/{id:int}")]
    [HttpGet("Success/{id:int}")]
    public async Task<IActionResult> Success(int id)
    {
        var request = await _vehicleRequestService.GetRequestByIdAsync(id);
        if (request == null)
        {
            return RedirectToAction(nameof(Create));
        }

        ViewBag.SuccessMessage = TempData["SuccessMessage"] as string 
            ?? "Talebiniz alındı, Şef tarafından değerlendirilecek";

        return View(request);
    }

    #endregion

    #region Admin & Kullanici Yönetim İşlemleri

    /// <summary>
    /// Bekleyen ve Diğer Araç Talepleri Listesi
    /// GET: /VehicleRequest veya /VehicleRequest/Index
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] VehicleRequestFilterViewModel filter)
    {
        var model = await _vehicleRequestService.GetRequestsPagedAsync(filter);
        return View(model);
    }

    /// <summary>
    /// Talebi Karşılama Mini-Formu (Araç, Şoför, Tarih Seçimi)
    /// GET: /VehicleRequest/Fulfill/{id}
    /// </summary>
    [HttpGet("Fulfill/{id:int}")]
    public async Task<IActionResult> Fulfill(int id)
    {
        var model = await _vehicleRequestService.GetFulfillViewModelAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "İşleme alınacak araç talebi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Talebi Karşılama ve Görevlendirme Oluşturma
    /// POST: /VehicleRequest/Fulfill/{id}
    /// </summary>
    [HttpPost("Fulfill/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fulfill(int id, VehicleRequestFulfillViewModel model)
    {
        if (id != model.RequestId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var repopulated = await _vehicleRequestService.GetFulfillViewModelAsync(
                id, model.PlannedStartTime, model.PlannedEndTime);

            if (repopulated != null)
            {
                repopulated.VehicleId = model.VehicleId;
                repopulated.DriverId = model.DriverId;
                repopulated.Purpose = model.Purpose;
                repopulated.IncludeDriverInfoInEmail = model.IncludeDriverInfoInEmail;
                repopulated.DepartmentTitle = model.DepartmentTitle;
                repopulated.ChiefNote = model.ChiefNote;
                return View(repopulated);
            }
            return View(model);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message, assignmentId) = await _vehicleRequestService.FulfillRequestAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var repopulated = await _vehicleRequestService.GetFulfillViewModelAsync(
                id, model.PlannedStartTime, model.PlannedEndTime);

            if (repopulated != null)
            {
                repopulated.VehicleId = model.VehicleId;
                repopulated.DriverId = model.DriverId;
                repopulated.Purpose = model.Purpose;
                repopulated.IncludeDriverInfoInEmail = model.IncludeDriverInfoInEmail;
                repopulated.DepartmentTitle = model.DepartmentTitle;
                repopulated.ChiefNote = model.ChiefNote;
                return View(repopulated);
            }
            return View(model);
        }

        TempData["SuccessMessage"] = $"Talep #{id} başarıyla karşılandı ve Görev #{assignmentId} oluşturuldu. Talep sahibine bildirim e-postası gönderildi.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Talebi Gerekçe ile Reddetme
    /// POST: /VehicleRequest/Reject/{id}
    /// </summary>
    [HttpPost("Reject/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, [FromForm] string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["ErrorMessage"] = "Lütfen talep ret gerekçesini belirtiniz.";
            return RedirectToAction(nameof(Index));
        }

        var adminUserId = GetCurrentUserId();
        var (success, message) = await _vehicleRequestService.RejectRequestAsync(id, rejectionReason, adminUserId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Üst navbar bildirim çanı ve sidebar için bekleyen araç talepleri bildirim özeti (AJAX)
    /// GET: /VehicleRequest/PendingNotificationSummary
    /// </summary>
    [HttpGet("PendingNotificationSummary")]
    [Produces("application/json")]
    public async Task<IActionResult> PendingNotificationSummary()
    {
        var result = await _vehicleRequestService.GetPendingNotificationSummaryAsync();
        return Json(result);
    }

    #endregion
}
