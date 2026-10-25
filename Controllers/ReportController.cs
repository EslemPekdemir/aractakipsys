// File: Controllers/ReportController.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.Report;
using aractakipsys.Services.Implementations;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

using aractakipsys.Common;

namespace aractakipsys.Controllers;

[Authorize(Roles = AppRoles.AllStaffAndManagers)]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportController(IReportService reportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReportFilterViewModel filter)
    {
        // 1. İlk sayfa açılışı kontrolü (Kullanıcı menüden henüz yeni geldiğinde)
        if (!filter.HasSearched)
        {
            await _reportService.PrepareFilterOptionsAsync(filter);
            return View(new UsageReportViewModel { Filter = filter });
        }

        // 2. Zorunlu Filtre Kuralı: En az bir Araç VEYA en az bir Şoför seçilmelidir
        if (!filter.VehicleId.HasValue && !filter.DriverId.HasValue)
        {
            ModelState.AddModelError("", "Lütfen rapor oluşturmak için en az bir Araç veya Şoför seçiniz.");
            await _reportService.PrepareFilterOptionsAsync(filter);
            return View(new UsageReportViewModel { Filter = filter });
        }

        // 3. Tarih Aralığı Mantık Kontrolü
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value > filter.EndDate.Value)
        {
            ModelState.AddModelError("", "Başlangıç tarihi bitiş tarihinden daha ileri bir tarih olamaz.");
            await _reportService.PrepareFilterOptionsAsync(filter);
            return View(new UsageReportViewModel { Filter = filter });
        }

        // 4. Rapor Verilerini Getir
        var report = await _reportService.GetUsageReportAsync(filter);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf([FromQuery] ReportFilterViewModel filter, [FromQuery] bool preview = false)
    {
        // En az bir araç veya şoför kontrolü
        if (!filter.VehicleId.HasValue && !filter.DriverId.HasValue)
        {
            TempData["ErrorMessage"] = "PDF raporu indirmek için lütfen en az bir Araç veya Şoför seçiniz.";
            return RedirectToAction(nameof(Index), filter);
        }

        // Rapor verilerini çek
        var report = await _reportService.GetUsageReportAsync(filter);

        if (report.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Seçilen kriterlere uygun tamamlanmış sefer bulunamadığı için PDF oluşturulamadı.";
            return RedirectToAction(nameof(Index), filter);
        }

        // Giriş yapan kullanıcının ad ve unvanını al
        var currentUser = await _userManager.GetUserAsync(User);
        var creatorName = currentUser?.FullName ?? User.Identity?.Name ?? "Sistem Yetkilisi";

        // QuestPDF ile PDF dokümanını oluştur
        var document = new UsageReportPdfDocument(report, creatorName);
        var pdfBytes = document.GeneratePdf();

        var fileName = $"Kullanim_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        if (preview)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(pdfBytes, "application/pdf");
        }

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Maintenance([FromQuery] MaintenanceReportFilterViewModel filter)
    {
        // 1. İlk sayfa açılışı kontrolü
        if (!filter.HasSearched)
        {
            await _reportService.PrepareMaintenanceFilterOptionsAsync(filter);
            return View(new MaintenanceReportViewModel { Filter = filter });
        }

        // 2. Tarih Aralığı Mantık Kontrolü
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value > filter.EndDate.Value)
        {
            ModelState.AddModelError("", "Başlangıç tarihi bitiş tarihinden daha ileri bir tarih olamaz.");
            await _reportService.PrepareMaintenanceFilterOptionsAsync(filter);
            return View(new MaintenanceReportViewModel { Filter = filter });
        }

        // 3. Rapor Verilerini Getir
        var report = await _reportService.GetMaintenanceReportAsync(filter);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportMaintenancePdf([FromQuery] MaintenanceReportFilterViewModel filter, [FromQuery] bool preview = false)
    {
        filter.HasSearched = true;
        var report = await _reportService.GetMaintenanceReportAsync(filter);

        if (report.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Seçilen kriterlere uygun bakım/onarım kaydı bulunamadığı için PDF oluşturulamadı.";
            return RedirectToAction(nameof(Maintenance), filter);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var creatorName = currentUser?.FullName ?? User.Identity?.Name ?? "Sistem Yetkilisi";

        var document = new MaintenanceReportPdfDocument(report, creatorName);
        var pdfBytes = document.GeneratePdf();

        var fileName = $"Arac_Bakim_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        if (preview)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(pdfBytes, "application/pdf");
        }

        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Sigorta ve Muayene Durum Raporu (Aşama E)
    /// GET: /Report/InsuranceInspection
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> InsuranceInspection([FromQuery] InsuranceInspectionReportFilterViewModel filter)
    {
        var report = await _reportService.GetInsuranceInspectionReportAsync(filter, paged: true);
        return View(report);
    }

    /// <summary>
    /// Sigorta ve Muayene Durum Raporu PDF İndirme (QuestPDF)
    /// GET: /Report/ExportInsuranceInspectionPdf
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportInsuranceInspectionPdf([FromQuery] InsuranceInspectionReportFilterViewModel filter, [FromQuery] bool preview = false)
    {
        var report = await _reportService.GetInsuranceInspectionReportAsync(filter, paged: false);

        if (report.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Seçilen filtre kriterine uygun araç kaydı bulunamadığı için PDF oluşturulamadı.";
            return RedirectToAction(nameof(InsuranceInspection), filter);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var creatorName = currentUser?.FullName ?? User.Identity?.Name ?? "Sistem Yetkilisi";

        var document = new InsuranceInspectionReportPdfDocument(report, creatorName);
        var pdfBytes = document.GeneratePdf();

        var scopePrefix = filter.Scope switch
        {
            ReportScopeType.Insurance => "Sigorta_Raporu",
            ReportScopeType.Inspection => "Muayene_Raporu",
            _ => "Sigorta_Muayene_Raporu"
        };

        var fileName = $"{scopePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        if (preview)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(pdfBytes, "application/pdf");
        }

        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Talep Eden Birimler Bazında Araç Talep İstatistiği Raporu (Faz 7-B)
    /// GET: /Report/VehicleRequests
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> VehicleRequests([FromQuery] VehicleRequestReportFilterViewModel filter)
    {
        // Tarih Aralığı Mantık Kontrolü
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value > filter.EndDate.Value)
        {
            ModelState.AddModelError("", "Başlangıç tarihi bitiş tarihinden daha ileri bir tarih olamaz.");
        }

        var report = await _reportService.GetVehicleRequestReportAsync(filter);
        return View(report);
    }

    /// <summary>
    /// Araç Talepleri İstatistiği PDF İndirme & Önizleme (QuestPDF)
    /// GET: /Report/ExportVehicleRequestsPdf
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportVehicleRequestsPdf([FromQuery] VehicleRequestReportFilterViewModel filter, [FromQuery] bool preview = false)
    {
        var report = await _reportService.GetVehicleRequestReportAsync(filter);

        if (report.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Seçilen tarih kriterine uygun araç talebi bulunamadığı için PDF oluşturulamadı.";
            return RedirectToAction(nameof(VehicleRequests), filter);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var creatorName = currentUser?.FullName ?? User.Identity?.Name ?? "Sistem Yetkilisi";

        var document = new VehicleRequestReportPdfDocument(report, creatorName);
        var pdfBytes = document.GeneratePdf();

        var fileName = $"Arac_Talepleri_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        if (preview)
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(pdfBytes, "application/pdf");
        }

        return File(pdfBytes, "application/pdf", fileName);
    }
}

