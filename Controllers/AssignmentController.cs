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

using aractakipsys.Common;
using aractakipsys.Common.Helpers;

namespace aractakipsys.Controllers;

[Authorize(Roles = AppRoles.AllStaffAndManagers)]
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
    /// Resmi Görevlendirme Belgesi PDF İndir (Şoför bazlı veya genel)
    /// GET: /Assignment/DownloadOrderDocument/{id}
    /// </summary>
    [HttpGet("DownloadOrderDocument/{id:int}")]
    public async Task<IActionResult> DownloadOrderDocument(int id, [FromQuery] bool emptyValues = false, [FromQuery] int? driverId = null, [FromQuery] bool preview = false)
    {
        var model = await _assignmentService.GetAssignmentDetailsAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Görevlendirme belgesi üretilecek kayıt bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var creatorName = model.CreatedByUserName;
        if (string.IsNullOrWhiteSpace(creatorName))
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var candidate = currentUser?.FullName;
            if (!string.IsNullOrWhiteSpace(candidate) && !Guid.TryParse(candidate.Trim(), out _))
            {
                creatorName = candidate.Trim();
            }
        }

        string? specificDriverName = null;
        if (driverId.HasValue && model.AssignedDrivers != null)
        {
            var targetDriver = model.AssignedDrivers.FirstOrDefault(d => d.DriverId == driverId.Value);
            if (targetDriver != null)
            {
                specificDriverName = targetDriver.DriverFullName;
            }
        }

        var document = new AssignmentOrderDocument(model, creatorName, emptyValues: emptyValues, specificDriverFullName: specificDriverName);
        var pdfBytes = document.GeneratePdf();

        var safePlate = (model.VehiclePlate ?? "Arac").Replace(" ", "_");
        var prefix = emptyValues ? "Gorevlendirme_Belgesi_Bos" : "Gorevlendirme_Belgesi";
        var driverSuffix = !string.IsNullOrWhiteSpace(specificDriverName) ? $"_{specificDriverName.Replace(" ", "_")}" : "";
        var fileName = $"{prefix}_{model.Id}_{safePlate}{driverSuffix}.pdf";

        if (preview)
        {
            FileDownloadHelper.SetContentDisposition(Response, fileName, preview: true);
            return File(pdfBytes, "application/pdf");
        }

        return File(pdfBytes, "application/pdf", FileDownloadHelper.ToSafeAsciiFileName(fileName));
    }

    /// <summary>
    /// Acil Güzergah Dışı Sevk Kaydı Ekleme (POST)
    /// POST: /Assignment/SaveOffRouteDispatch
    /// </summary>
    [HttpPost("SaveOffRouteDispatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOffRouteDispatch(OffRouteDispatchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Geçersiz veri gönderildi.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var (success, message) = await _assignmentService.SaveOffRouteDispatchAsync(model, userId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
    }

    /// <summary>
    /// Şoförün Bildirdiği Güzergah Değişikliğini Onaylama (Şef / Amir Onayı)
    /// POST: /Assignment/ApproveRouteChange/{id}
    /// </summary>
    [HttpPost("ApproveRouteChange/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.OperationalStaff)]
    public async Task<IActionResult> ApproveRouteChange(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var (success, message, assignmentId) = await _assignmentService.ApproveRouteChangeAsync(id, userId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id = assignmentId > 0 ? assignmentId : 0 });
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
            model.AvailableVehicles = repopulatedModel.AvailableVehicles;
            model.AvailableDrivers = repopulatedModel.AvailableDrivers;
            return View(model);
        }

        var adminUserId = GetCurrentUserId();
        var (success, message, assignmentId) = await _assignmentService.CreateAssignmentAsync(model, adminUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var repopulatedModel = await _assignmentService.GetCreateViewModelAsync(model.PlannedStartTime, model.PlannedEndTime);
            model.AvailableVehicles = repopulatedModel.AvailableVehicles;
            model.AvailableDrivers = repopulatedModel.AvailableDrivers;
            return View(model);
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
    public async Task<IActionResult> StartDirect(int id, [FromForm] int? startKm, [FromForm] bool confirmKmDifference = false, [FromQuery] string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        var (success, message) = await _assignmentService.StartAssignmentDirectAsync(id, userId, startKm, confirmKmDifference);

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
    public async Task<IActionResult> EndDirect(int id, [FromForm] int endKm, [FromForm] string? driverReturnNotes, [FromForm] bool confirmKmDifference = false, [FromQuery] string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        var (success, message) = await _assignmentService.EndAssignmentDirectAsync(id, userId, endKm, driverReturnNotes, confirmKmDifference);

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
    /// Fiziki olarak imzalanmış görevlendirme belgesini sisteme ekler (çoklu belge destekli).
    /// POST: /Assignment/UploadSignedDocument/{id}
    /// </summary>
    [HttpPost("UploadSignedDocument/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // Kestrel düzeyinde 10 MB sınır
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)] // Form düzeyinde 10 MB sınır
    public async Task<IActionResult> UploadSignedDocument(
        int id,
        IFormFile? signedDocument,
        [FromForm] int? driverId = null,
        [FromForm] string? description = null,
        string? returnUrl = null)
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
        var result = await _assignmentService.UploadSignedDocumentAsync(id, signedDocument, userId, driverId, description);

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
    /// Yüklenmiş bir imzalı görevlendirme belgesini yumuşak siler (Soft-Delete).
    /// POST: /Assignment/DeleteSignedDocument/{id}/{documentId}
    /// </summary>
    [HttpPost("DeleteSignedDocument/{id:int}/{documentId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSignedDocument(int id, int documentId, string? returnUrl = null)
    {
        var userId = GetCurrentUserId();
        var result = await _assignmentService.DeleteSignedDocumentAsync(id, documentId, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "İmzalı belge kaydı başarıyla silindi.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Güvenli dizinde saklanan imzalı görevlendirme belgesini indirir (attachment).
    /// GET: /Assignment/DownloadSignedDocument/{id}?documentId=...
    /// </summary>
    [HttpGet("DownloadSignedDocument/{id:int}")]
    public async Task<IActionResult> DownloadSignedDocument(int id, [FromQuery] int? documentId = null)
    {
        var document = await _assignmentService.GetSignedDocumentFileAsync(id, documentId);

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
    /// GET: /Assignment/ViewSignedDocument/{id}?documentId=...
    /// </summary>
    [HttpGet("ViewSignedDocument/{id:int}")]
    public async Task<IActionResult> ViewSignedDocument(int id, [FromQuery] int? documentId = null)
    {
        var document = await _assignmentService.GetSignedDocumentFileAsync(id, documentId);

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

    /// <summary>
    /// Üst Navbar Bildirim Çanı İçin Seferdeki Güzergah Değişiklikleri Özeti
    /// GET: /Assignment/PendingRouteChangeNotificationSummary
    /// </summary>
    [HttpGet("PendingRouteChangeNotificationSummary")]
    public async Task<IActionResult> PendingRouteChangeNotificationSummary()
    {
        var (count, items) = await _assignmentService.GetPendingRouteChangeNotificationSummaryAsync();

        return Json(new
        {
            pendingCount = count,
            latestChanges = items.Select(x => new
            {
                id = x.Id,
                assignmentId = x.AssignmentId,
                plate = x.VehiclePlate,
                driver = x.ChangedByFullName,
                newRoute = x.NewRoute,
                reason = x.Reason,
                formattedDate = x.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            })
        });
    }
}
