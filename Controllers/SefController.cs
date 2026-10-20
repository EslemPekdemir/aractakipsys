// File: Controllers/SefController.cs
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Sef;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Controllers;

[Authorize(Roles = "Admin")]
public class SefController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SefController> _logger;

    public SefController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<SefController> logger)
    {
        _userManager = userManager;
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Sef
    /// Tüm Ulaşım Şeflerini (Kullanici rolündeki kullanıcıları) listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sefUsers = await _userManager.GetUsersInRoleAsync("Kullanici");

        var list = new List<SefListItemViewModel>();

        foreach (var u in sefUsers.OrderByDescending(x => x.CreatedAt))
        {
            list.Add(new SefListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                ProfilePictureUrl = u.ProfilePictureUrl
            });
        }

        return View(list);
    }

    /// <summary>
    /// GET: /Sef/Create
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View(new SefCreateViewModel());
    }

    /// <summary>
    /// POST: /Sef/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SefCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cleanEmail = model.Email.Trim().ToLowerInvariant();
        var cleanUserName = model.UserName.Trim().ToLowerInvariant();

        // 1. E-Posta Kontrolü
        var existingEmail = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingEmail != null)
        {
            ModelState.AddModelError("Email", "Bu e-posta adresi zaten başka bir kullanıcı tarafından kullanılmaktadır.");
            return View(model);
        }

        // 2. Kullanıcı Adı Kontrolü
        var existingUser = await _userManager.FindByNameAsync(cleanUserName);
        if (existingUser != null)
        {
            ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten kullanılmaktadır.");
            return View(model);
        }

        // 3. Kullanıcıyı Oluştur
        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            UserName = cleanUserName,
            Email = cleanEmail,
            PhoneNumber = model.PhoneNumber?.Trim(),
            EmailConfirmed = true,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // 4. 'Kullanici' (Şef) Rolü Ata
        await _userManager.AddToRoleAsync(user, "Kullanici");

        // 5. Audit Log
        var currentAdmin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(
            nameof(ApplicationUser),
            0,
            AuditAction.Create,
            currentAdmin?.Id,
            currentAdmin?.FullName,
            new
            {
                CreatedUserId = user.Id,
                user.FullName,
                user.UserName,
                user.Email,
                Role = "Kullanici (Şef)"
            });

        _logger.LogInformation("Yeni Ulaşım Şefi oluşturuldu: {UserName} ({Email}) - Oluşturan: {Admin}", 
            user.UserName, user.Email, User.Identity?.Name);

        TempData["SuccessMessage"] = $"'{user.FullName}' başarıyla yeni Ulaşım Şefi olarak tanımlandı.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET: /Sef/Edit/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Kullanici"))
        {
            TempData["ErrorMessage"] = "Belirtilen Ulaşım Şefi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var model = new SefEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive
        };

        return View(model);
    }

    /// <summary>
    /// POST: /Sef/Edit
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SefEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Kullanici"))
        {
            TempData["ErrorMessage"] = "Güncellenecek Ulaşım Şefi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        var currentAdmin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(
            nameof(ApplicationUser),
            0,
            AuditAction.Update,
            currentAdmin?.Id,
            currentAdmin?.FullName,
            new
            {
                UpdatedUserId = user.Id,
                user.FullName,
                user.PhoneNumber,
                user.IsActive
            });

        TempData["SuccessMessage"] = $"'{user.FullName}' bilgileri başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: /Sef/ToggleStatus/{id}
    /// Şefin durumunu dondurur (pasif yapar) veya tekrar aktif eder.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Kullanici"))
        {
            TempData["ErrorMessage"] = "Ulaşım Şefi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var currentAdmin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(
            nameof(ApplicationUser),
            0,
            AuditAction.Update,
            currentAdmin?.Id,
            currentAdmin?.FullName,
            new
            {
                UserId = user.Id,
                user.UserName,
                NewStatus = user.IsActive ? "Aktif" : "Pasif"
            });

        var statusText = user.IsActive ? "aktif edildi" : "donduruldu (pasif yapıldı)";
        TempData["SuccessMessage"] = $"'{user.FullName}' hesabı başarıyla {statusText}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: /Sef/ResetPassword
    /// Admin'in unutan şef için doğrudan yeni şifre belirlemesini sağlar.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(SefResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Şifre en az 10 karakter olmalı ve şifre tekrarı ile eşleşmelidir.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Kullanici"))
        {
            TempData["ErrorMessage"] = "Şifresi sıfırlanacak şef bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        var currentAdmin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(
            nameof(ApplicationUser),
            0,
            AuditAction.Update,
            currentAdmin?.Id,
            currentAdmin?.FullName,
            new
            {
                UserId = user.Id,
                user.UserName,
                Action = "AdminPasswordReset"
            });

        TempData["SuccessMessage"] = $"'{user.FullName}' için yeni şifre başarıyla tanımlandı.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: /Sef/Delete/{id}
    /// Ulaşım Şefini pasife alır (Soft Delete / Hesap Dondurma).
    /// Sistem genelinde denetim izleri (AuditLog) ve veri bütünlüğünün korunması amacıyla kalıcı silme yerine soft-delete uygulanır.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Kullanici"))
        {
            TempData["ErrorMessage"] = "İşlem yapılacak Ulaşım Şefi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Kendi hesabını pasife alma engeli
        var currentAdmin = await _userManager.GetUserAsync(User);
        if (currentAdmin?.Id == user.Id)
        {
            TempData["ErrorMessage"] = "Kendi hesabınızı silemez veya pasife alamazsınız.";
            return RedirectToAction(nameof(Index));
        }

        if (!user.IsActive)
        {
            TempData["ErrorMessage"] = $"'{user.FullName}' hesabı zaten dondurulmuş (pasif) durumdadır.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Kullanıcı durumu güncellenirken bir hata oluştu: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _auditLogService.LogAsync(
            nameof(ApplicationUser),
            0,
            AuditAction.Delete,
            currentAdmin?.Id,
            currentAdmin?.FullName,
            new
            {
                DeactivatedUserId = user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                ActionType = "SoftDelete"
            });

        _logger.LogInformation("Ulaşım Şefi hesabı pasife alındı (Soft-Delete): {UserName} ({FullName}) - İşlemi Yapan Admin: {Admin}",
            user.UserName, user.FullName, User.Identity?.Name);

        TempData["SuccessMessage"] = $"'{user.FullName}' ({user.UserName}) hesabı başarıyla pasife alındı (donduruldu). Geçmiş işlem ve denetim kayıtları korundu.";
        return RedirectToAction(nameof(Index));
    }
}
