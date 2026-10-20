// File: Controllers/AssignmentController.cs
using System.Security.Claims;
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.Assignment;
using aractakipsys.Services.Implementations;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace aractakipsys.Controllers;

[Authorize(Roles = "Admin,Kullanici")]
[Route("Assignment")]
public class AssignmentController : Controller
{
    private readonly IAssignmentService _assignmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AssignmentController> _logger;

    public AssignmentController(
        IAssignmentService assignmentService,
        UserManager<ApplicationUser> userManager,
        ILogger<AssignmentController> logger)
    {
        _assignmentService = assignmentService;
        _userManager = userManager;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
    }

    /// <summary>
    /// Görev Listesi (Filtreleme & Sayfalama)
    /// GET: /Assignment
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] AssignmentFilterViewModel filter)
    {
        var model = await _assignmentService.GetAssignmentsPagedAsync(filter);
        return View(model);
    }

    /// <summary>
    /// Görev Detayı
    /// GET: /Assignment/Details/{id}
    /// </summary>
    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _assignmentService.GetAssignmentDetailsAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "İstenen görev kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Resmi Görevlendirme Belgesi PDF İndir
    /// GET: /Assignment/DownloadOrderDocument/{id}
    /// </summary>
    [HttpGet("DownloadOrderDocument/{id:int}")]
    public async Task<IActionResult> DownloadOrderDocument(int id)
    {
        var model = await _assignmentService.GetAssignmentDetailsAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Görevlendirme belgesi üretilecek kayıt bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var creatorName = currentUser?.FullName ?? User.Identity?.Name ?? "Sistem Yetkilisi";

        var document = new AssignmentOrderDocument(model, creatorName);
        var pdfBytes = document.GeneratePdf();

        var safePlate = (model.VehiclePlate ?? "Arac").Replace(" ", "_");
        var fileName = $"Gorevlendirme_Belgesi_{model.Id}_{safePlate}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Yeni Görev Atama Formu
    /// GET: /Assignment/Create
    /// </summary>
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var model = await _assignmentService.GetCreateViewModelAsync();
        return View(model);
    }

    /// <summary>
    /// Yeni Görev Kaydetme
    /// POST: /Assignment/Create
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignmentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var repopulatedModel = await _assignmentService.GetCreateViewModelAsync(model.PlannedStartTime, model.PlannedEndTime);
            repopulatedModel.VehicleId = model.VehicleId;
            repopulatedModel.DriverId = model.DriverId;
            repopulatedModel.RequestedByDepartment = model.RequestedByDepartment;
            repopulatedModel.Purpose = model.Purpose;
            repopulatedModel.AccommodationNights = model.AccommodationNights;
            repopulatedModel.AccommodationCost = model.AccommodationCost;
            repopulatedModel.PerDiemType = model.PerDiemType ?? "Hourly";
            repopulatedModel.PerDiemHours = model.PerDiemHours;
            repopulatedModel.PerDiemRate = model.PerDiemRate;
            repopulatedModel.PerDiemDays = model.PerDiemDays;
            repopulatedModel.PerDiemAmount = model.PerDiemAmount;
            repopulatedModel.ExpenseNotes = model.ExpenseNotes;
            return View(repopulatedModel);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message, assignmentId) = await _assignmentService.CreateAssignmentAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var repopulatedModel = await _assignmentService.GetCreateViewModelAsync(model.PlannedStartTime, model.PlannedEndTime);
            repopulatedModel.VehicleId = model.VehicleId;
            repopulatedModel.DriverId = model.DriverId;
            repopulatedModel.RequestedByDepartment = model.RequestedByDepartment;
            repopulatedModel.Purpose = model.Purpose;
            repopulatedModel.AccommodationNights = model.AccommodationNights;
            repopulatedModel.AccommodationCost = model.AccommodationCost;
            repopulatedModel.PerDiemType = model.PerDiemType ?? "Hourly";
            repopulatedModel.PerDiemHours = model.PerDiemHours;
            repopulatedModel.PerDiemRate = model.PerDiemRate;
            repopulatedModel.PerDiemDays = model.PerDiemDays;
            repopulatedModel.PerDiemAmount = model.PerDiemAmount;
            repopulatedModel.ExpenseNotes = model.ExpenseNotes;
            return View(repopulatedModel);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Görev Düzenleme Formu (Yalnızca Planned görevler)
    /// GET: /Assignment/Edit/{id}
    /// </summary>
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _assignmentService.GetAssignmentEditModelAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Düzenlenmek istenen görev bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    /// <summary>
    /// Görev Güncelleme
    /// POST: /Assignment/Edit/{id}
    /// </summary>
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssignmentEditViewModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Geçersiz istek.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model = await _assignmentService.PopulateEditDropdownsAsync(model);
            return View(model);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message) = await _assignmentService.UpdateAssignmentAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            model = await _assignmentService.PopulateEditDropdownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Seyir Halindeki Görevin Planlanan Süresini Uzatma
    /// POST: /Assignment/ExtendDuration/{id}
    /// </summary>
    [HttpPost("ExtendDuration/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtendDuration(int id, [FromForm] DateTime newPlannedEndTime, [FromForm] string? reason)
    {
        var adminUserId = GetCurrentUserId();
        var (success, message) = await _assignmentService.ExtendAssignmentDurationAsync(id, newPlannedEndTime, reason, adminUserId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Görev İptal Etme (Yalnızca Planned görevler)
    /// POST: /Assignment/Cancel/{id}
    /// </summary>
    [HttpPost("Cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, [FromForm] string cancellationReason, [FromQuery] string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(cancellationReason))
        {
            TempData["ErrorMessage"] = "Görev iptal gerekçesi belirtilmelidir.";
            return RedirectToAction(nameof(Index));
        }

        var adminUserId = GetCurrentUserId();
        var (success, message) = await _assignmentService.CancelAssignmentAsync(id, cancellationReason, adminUserId);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Şoförün bildirdiği görev başlangıcını onaylama
    /// POST: /Assignment/ApproveStart/{id}
    /// </summary>
    [HttpPost("ApproveStart/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveStart(int id, [FromQuery] string? returnUrl = null)
    {
        var approverUserId = GetCurrentUserId();
        var (success, message) = await _assignmentService.ApproveStartAsync(id, approverUserId);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Şoförün bildirdiği görev bitişini onaylama ve çalışma saati/ücret hesaplama
    /// POST: /Assignment/ApproveEnd/{id}
    /// </summary>
    [HttpPost("ApproveEnd/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveEnd(int id, [FromForm] AssignmentApproveEndViewModel model)
    {
        var approverUserId = GetCurrentUserId();
        model.AssignmentId = id;

        (bool success, string message) result;
        if (model.ConfirmedStartTime == default || model.ConfirmedEndTime == default)
        {
            result = await _assignmentService.ApproveEndAsync(id, approverUserId);
        }
        else
        {
            result = await _assignmentService.ApproveEndAsync(model, approverUserId);
        }

        if (!result.success)
        {
            TempData["ErrorMessage"] = result.message;
        }
        else
        {
            TempData["SuccessMessage"] = result.message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Tamamlanmış görev için fatura/harcırah revizyonu (Şef / Admin)
    /// POST: /Assignment/UpdateFinancialDetails/{id}
    /// </summary>
    [HttpPost("UpdateFinancialDetails/{id:int}")]
    [Authorize(Roles = "Admin,Kullanici")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFinancialDetails(int id, [FromForm] AssignmentFinancialRevisionViewModel model)
    {
        var userId = GetCurrentUserId();
        model.AssignmentId = id;

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            TempData["ErrorMessage"] = firstError ?? "Lütfen formu eksiksiz doldurunuz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var (success, message) = await _assignmentService.UpdateFinancialDetailsAsync(model, userId);
        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Yetkili tarafından görevi doğrudan başlatma (Şoför bildirimi olmaksızın)
    /// POST: /Assignment/StartDirect/{id}
    /// </summary>
    [HttpPost("StartDirect/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartDirect(int id, [FromForm] int? startKm, [FromQuery] string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        var (success, message) = await _assignmentService.StartAssignmentDirectAsync(id, userId, startKm);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Yetkili tarafından görevi doğrudan bitirme (Şoför bildirimi olmaksızın)
    /// POST: /Assignment/EndDirect/{id}
    /// </summary>
    [HttpPost("EndDirect/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndDirect(int id, [FromForm] int endKm, [FromForm] string? driverReturnNotes)
    {
        var userId = GetCurrentUserId();
        var (success, message) = await _assignmentService.EndAssignmentDirectAsync(id, userId, endKm, driverReturnNotes);

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
    /// Fiziki olarak imzalanmış görevlendirme belgesini sisteme yükler.
    /// POST: /Assignment/UploadSignedDocument/{id}
    /// </summary>
    [HttpPost("UploadSignedDocument/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // Kestrel düzeyinde 10 MB sınır
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)] // Form düzeyinde 10 MB sınır
    public async Task<IActionResult> UploadSignedDocument(int id, IFormFile? signedDocument, string? returnUrl = null)
    {
        if (signedDocument == null || signedDocument.Length == 0)
        {
            TempData["ErrorMessage"] = "Lütfen yüklenecek bir imzalı belge dosyası seçiniz.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = GetCurrentUserId();
        var result = await _assignmentService.UploadSignedDocumentAsync(id, signedDocument, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "İmzalı görevlendirme belgesi başarıyla sisteme yüklendi.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Güvenli dizinde saklanan imzalı görevlendirme belgesini indirir (attachment).
    /// GET: /Assignment/DownloadSignedDocument/{id}
    /// </summary>
    [HttpGet("DownloadSignedDocument/{id:int}")]
    public async Task<IActionResult> DownloadSignedDocument(int id)
    {
        var document = await _assignmentService.GetSignedDocumentFileAsync(id);

        if (document == null || document.Value.FileContents == null)
        {
            TempData["ErrorMessage"] = "İmzalı belge dosyası bulunamadı veya diskten silinmiş.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var (fileBytes, contentType, originalFileName) = document.Value;

        return File(fileBytes, contentType ?? "application/octet-stream", originalFileName);
    }

    /// <summary>
    /// İmzalı görevlendirme belgesini yeni sekmede doğrudan görüntüler (inline preview).
    /// GET: /Assignment/ViewSignedDocument/{id}
    /// </summary>
    [HttpGet("ViewSignedDocument/{id:int}")]
    public async Task<IActionResult> ViewSignedDocument(int id)
    {
        var document = await _assignmentService.GetSignedDocumentFileAsync(id);

        if (document == null || document.Value.FileContents == null)
        {
            TempData["ErrorMessage"] = "İmzalı belge dosyası bulunamadı veya diskten silinmiş.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var (fileBytes, contentType, _) = document.Value;

        return File(fileBytes, contentType ?? "application/octet-stream");
    }

    /// <summary>
    /// İmzalı Görevlendirme Evrak Arşivi ve Takip Ekranı
    /// GET: /Assignment/SignedDocuments veya /Assignment/Documents
    /// </summary>
    [HttpGet("SignedDocuments")]
    [HttpGet("Documents")]
    public async Task<IActionResult> SignedDocuments(SignedDocumentArchiveFilterViewModel filter)
    {
        var model = await _assignmentService.GetSignedDocumentArchiveAsync(filter);
        return View(model);
    }
}
