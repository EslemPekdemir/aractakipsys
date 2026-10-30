// File: Controllers/RoleDelegationController.cs
using System.Security.Claims;
using aractakipsys.Common;
using aractakipsys.Common.Helpers;
using aractakipsys.Models.ViewModels;
using aractakipsys.Models.ViewModels.RoleDelegation;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

[Authorize(Roles = AppRoles.AllStaffAndManagers)]
[Route("RoleDelegation")]
public class RoleDelegationController : Controller
{
    private readonly IRoleDelegationService _roleDelegationService;
    private readonly ILogger<RoleDelegationController> _logger;

    public RoleDelegationController(
        IRoleDelegationService roleDelegationService,
        ILogger<RoleDelegationController> logger)
    {
        _roleDelegationService = roleDelegationService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    private bool IsElevatedAdmin()
    {
        return User.IsInRole(AppRoles.Admin) || 
               User.IsInRole(AppRoles.DaireBaskani) || 
               User.IsInRole(AppRoles.GenelSekreter);
    }

    private bool CanManageDelegation()
    {
        // Genel Sekreter yalnızca salt okunur görebilir (ReadOnlyRoleFilter zaten POST'u engeller)
        return User.IsInRole(AppRoles.Admin) || 
               User.IsInRole(AppRoles.DaireBaskani) || 
               User.IsInRole(AppRoles.Kullanici);
    }

    /// <summary>
    /// GET: /RoleDelegation
    /// Vekâlet Yönetimi ve Geçmiş Listesi
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var isElevated = IsElevatedAdmin();

        var model = await _roleDelegationService.GetDelegationListAsync(userId, isElevated);
        return View(model);
    }

    /// <summary>
    /// GET: /RoleDelegation/GetCreateModal
    /// Yetki Devri Oluşturma Modal İçeriği (AJAX)
    /// </summary>
    [HttpGet("GetCreateModal")]
    public async Task<IActionResult> GetCreateModal()
    {
        if (!CanManageDelegation())
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();
        var isElevated = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.DaireBaskani);
        var model = await _roleDelegationService.GetCreateModalViewModelAsync(userId, isElevated);

        return Json(new
        {
            success = true,
            isElevatedAdmin = model.IsElevatedAdmin,
            currentUserId = model.CurrentUserId,
            currentUserFullName = model.CurrentUserFullName,
            eligibleDelegators = model.EligibleDelegators,
            eligibleDelegatees = model.EligibleDelegatees
        });
    }

    /// <summary>
    /// POST: /RoleDelegation/Create
    /// Yetki Devri Kaydı Oluşturma (AJAX / POST)
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] RoleDelegationCreateInputModel input)
    {
        if (!CanManageDelegation())
        {
            return Json(AjaxResultViewModel.Fail("Yetki devri oluşturma yetkiniz bulunmamaktadır."));
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Lütfen tüm zorunlu alanları eksiksiz doldurunuz.";

            return Json(AjaxResultViewModel.Fail(firstError));
        }

        var userId = GetCurrentUserId();
        var isElevated = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.DaireBaskani);

        var (success, message, delegationId, conflictList) = await _roleDelegationService.CreateDelegationAsync(
            input, userId, isElevated);

        if (!success)
        {
            return Json(new
            {
                success = false,
                message,
                conflictingAssignments = conflictList
            });
        }

        return Json(new
        {
            success = true,
            message,
            delegationId
        });
    }

    /// <summary>
    /// POST: /RoleDelegation/Revoke
    /// Yetki Devrini Derhal Sonlandır (AJAX / POST)
    /// </summary>
    [HttpPost("Revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke([FromBody] RoleDelegationRevokeInputModel input)
    {
        if (!CanManageDelegation())
        {
            return Json(AjaxResultViewModel.Fail("Yetki devrini sonlandırma yetkiniz bulunmamaktadır."));
        }

        var userId = GetCurrentUserId();
        var isElevated = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.DaireBaskani);

        var (success, message) = await _roleDelegationService.RevokeDelegationAsync(
            input.DelegationId, userId, input.RevokeReason, isElevated);

        if (!success)
        {
            return Json(AjaxResultViewModel.Fail(message));
        }

        return Json(AjaxResultViewModel.Ok(message));
    }

    /// <summary>
    /// GET: /RoleDelegation/Print/{id}?preview=true
    /// Yetki Devri Belgesi PDF İndirme veya Önizleme
    /// </summary>
    [HttpGet("Print/{id:int}")]
    public async Task<IActionResult> Print(int id, [FromQuery] bool preview = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isElevated = IsElevatedAdmin();

            var pdfBytes = await _roleDelegationService.GenerateDelegationPdfAsync(id, userId, isElevated);
            var fileName = $"Yetki_Devri_Belgesi_YD_{id:D5}.pdf";

            if (preview)
            {
                FileDownloadHelper.SetContentDisposition(Response, fileName, preview: true);
                return File(pdfBytes, "application/pdf");
            }

            return File(pdfBytes, "application/pdf", FileDownloadHelper.ToSafeAsciiFileName(fileName));
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Yetki devri belgesi bulunamadı.");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
