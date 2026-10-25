// File: Controllers/AuditLogController.cs
using aractakipsys.Models.ViewModels.AuditLog;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using aractakipsys.Common;

namespace aractakipsys.Controllers;

[Authorize(Roles = AppRoles.ManagementAndAudit)]
[Route("AuditLog")]
public class AuditLogController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(
        IAuditLogService auditLogService,
        ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Denetim Günlüğü Listesi (Salt Okunur - ReadOnly)
    /// GET: /AuditLog
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterViewModel filter)
    {
        var model = await _auditLogService.GetAuditLogsPagedAsync(filter);
        return View(model);
    }
}
