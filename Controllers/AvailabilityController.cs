// File: Controllers/AvailabilityController.cs
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aractakipsys.Controllers;

[Authorize(Roles = "Admin,Kullanici")]
[Route("Availability")]
public class AvailabilityController : Controller
{
    private readonly IAssignmentService _assignmentService;

    public AvailabilityController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Belirtilen zaman aralığında aracın müsaitliğini kontrol eder (AJAX).
    /// GET: /Availability/CheckVehicleAvailability?vehicleId=1&start=...&end=...&excludeAssignmentId=...
    /// </summary>
    [HttpGet("CheckVehicleAvailability")]
    public async Task<IActionResult> CheckVehicleAvailability(
        [FromQuery] int vehicleId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int? excludeAssignmentId = null)
    {
        if (vehicleId <= 0 || start >= end)
        {
            return Json(new { isAvailable = false, reason = "Geçersiz araç ID veya tarih aralığı." });
        }

        var (isAvailable, reason, conflictingAssignmentId) = await _assignmentService.CheckVehicleAvailabilityAsync(
            vehicleId, start, end, excludeAssignmentId);

        return Json(new
        {
            isAvailable,
            reason,
            conflictingAssignmentId
        });
    }

    /// <summary>
    /// Belirtilen zaman aralığında şoförün müsaitliğini ve ehliyet geçerliliğini kontrol eder (AJAX).
    /// GET: /Availability/CheckDriverAvailability?driverId=1&start=...&end=...&excludeAssignmentId=...
    /// </summary>
    [HttpGet("CheckDriverAvailability")]
    public async Task<IActionResult> CheckDriverAvailability(
        [FromQuery] int driverId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int? excludeAssignmentId = null)
    {
        if (driverId <= 0 || start >= end)
        {
            return Json(new { isAvailable = false, reason = "Geçersiz şoför ID veya tarih aralığı." });
        }

        var (isAvailable, reason, conflictingAssignmentId) = await _assignmentService.CheckDriverAvailabilityAsync(
            driverId, start, end, excludeAssignmentId);

        return Json(new
        {
            isAvailable,
            reason,
            conflictingAssignmentId
        });
    }

    /// <summary>
    /// Verilen tarih aralığı için tüm araç ve şoförlerin güncel müsaitlik listesini döner (AJAX).
    /// GET: /Availability/GetOptions?start=...&end=...&excludeAssignmentId=...
    /// </summary>
    [HttpGet("GetOptions")]
    public async Task<IActionResult> GetOptions(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] int? excludeAssignmentId = null)
    {
        if (start >= end)
        {
            return Json(new { success = false, message = "Bitiş zamanı başlangıç zamanından ileri olmalıdır." });
        }

        var model = await _assignmentService.GetCreateViewModelAsync(start, end);

        return Json(new
        {
            success = true,
            vehicles = model.AvailableVehicles,
            drivers = model.AvailableDrivers
        });
    }
}
