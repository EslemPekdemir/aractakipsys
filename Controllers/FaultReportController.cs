// File: Controllers/FaultReportController.cs
using System.Security.Claims;
using aractakipsys.Data;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.FaultReport;
using aractakipsys.Services.Implementations;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

using aractakipsys.Common;

namespace aractakipsys.Controllers;

[Authorize(Roles = AppRoles.AllStaffAndManagers)]
[Route("FaultReport")]
public class FaultReportController : Controller
{
    private readonly IFaultReportService _faultReportService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FaultReportController> _logger;

    public FaultReportController(
        IFaultReportService faultReportService,
        ApplicationDbContext context,
        ILogger<FaultReportController> logger)
    {
        _faultReportService = faultReportService;
        _context = context;
        _logger = logger;
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Arıza Bildirimleri Listesi
    /// GET: /FaultReport
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] FaultReportStatus? status = null, [FromQuery] int? vehicleId = null)
    {
        ViewBag.SelectedStatus = status;
        ViewBag.SelectedVehicleId = vehicleId;
        ViewBag.Vehicles = await _context.Vehicles
            .OrderBy(v => v.PlateNumber)
            .Select(v => new { v.Id, PlateAndBrand = $"{v.PlateNumber} - {v.Brand} {v.Model}" })
            .ToListAsync();

        var reports = await _faultReportService.GetFaultReportsAsync(status, vehicleId);
        return View(reports);
    }

    /// <summary>
    /// Arıza Bildirimi Detay ve Düzenleme / Onaylama Ekranı
    /// GET: /FaultReport/Detail/{id}
    /// </summary>
    [HttpGet("Detail/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var item = await _faultReportService.GetFaultReportByIdAsync(id);
        if (item == null)
        {
            TempData["ErrorMessage"] = "Arıza bildirimi kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(item);
    }

    /// <summary>
    /// Şefin Arıza Bildirimini Onaylaması (Form Alanlarını Düzenleyebilme Özelliği ile)
    /// POST: /FaultReport/Approve
    /// </summary>
    [HttpPost("Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(FaultReportApproveRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Lütfen formu eksiksiz doldurunuz.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction(nameof(Detail), new { id = request.Id });
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _faultReportService.ApproveFaultReportAsync(request, userId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = result.Success, message = result.Message });
        }

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = request.Id });
    }

    /// <summary>
    /// Arıza Durumu Güncelleme (Çözüldü / Reddedildi)
    /// POST: /FaultReport/UpdateStatus
    /// </summary>
    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromForm] FaultReportStatus status, [FromForm] string? notes)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _faultReportService.UpdateStatusAsync(id, status, notes, userId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = result.Success, message = result.Message });
        }

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>
    /// DÜZCE ÜNİVERSİTESİ ARAÇ ARIZA BİLDİRİM FORMU PDF İndirme
    /// GET: /FaultReport/DownloadPdf/{id}?isBlank=false
    /// </summary>
    [HttpGet("DownloadPdf/{id:int}")]
    public async Task<IActionResult> DownloadPdf(int id, [FromQuery] bool isBlank = false, [FromQuery] bool preview = false)
    {
        FaultReportPdfModel? model;
        if (isBlank)
        {
            model = await _faultReportService.GetBlankFaultReportPdfModelAsync(string.Empty);
        }
        else
        {
            model = await _faultReportService.GetFaultReportPdfModelAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Arıza bildirimi belgesi üretilecek kayıt bulunamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        var doc = new FaultReportDocument(model);
        var bytes = doc.GeneratePdf();

        var safePlate = (model.VehiclePlate ?? "Arac").Replace(" ", "_");
        var fileName = isBlank ? "Arac_Ariza_Bildirim_Formu_Bos.pdf" : $"Ariza_Bildirim_Formu_{model.Id}_{safePlate}.pdf";

        if (preview)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(bytes, "application/pdf");
        }

        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Boş Matbu Arıza Bildirim Formu PDF İndirme
    /// GET: /FaultReport/DownloadBlankPdf
    /// </summary>
    [HttpGet("DownloadBlankPdf")]
    public async Task<IActionResult> DownloadBlankPdf([FromQuery] bool preview = false)
    {
        var model = await _faultReportService.GetBlankFaultReportPdfModelAsync(string.Empty);
        var doc = new FaultReportDocument(model);
        var bytes = doc.GeneratePdf();
        var fileName = "Arac_Ariza_Bildirim_Formu_Bos.pdf";

        if (preview)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(bytes, "application/pdf");
        }

        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Üst Navbar Bildirim Çanı İçin Bekleyen Arıza Sayısı ve Son Kayıtlar
    /// GET: /FaultReport/PendingNotificationSummary
    /// </summary>
    [HttpGet("PendingNotificationSummary")]
    public async Task<IActionResult> PendingNotificationSummary()
    {
        var count = await _faultReportService.GetPendingFaultCountAsync();
        var latest = await _faultReportService.GetLatestPendingFaultsAsync(5);

        return Json(new
        {
            pendingCount = count,
            latestFaults = latest.Select(x => new
            {
                id = x.Id,
                plate = x.VehiclePlate,
                driver = x.DriverFullName,
                description = x.Description,
                formattedDate = x.ReportDate.ToString("dd.MM.yyyy HH:mm")
            })
        });
    }
}
