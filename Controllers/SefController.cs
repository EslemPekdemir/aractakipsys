// File: Controllers/SefController.cs
using aractakipsys.Common;
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

[Authorize(Roles = AppRoles.AdminOrDaireBaskani)]
public class SefController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IRoleDelegationService _roleDelegationService;
    private readonly ILogger<SefController> _logger;

    public SefController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IRoleDelegationService roleDelegationService,
        ILogger<SefController> logger)
    {
        _userManager = userManager;
        _context = context;
        _auditLogService = auditLogService;
        _roleDelegationService = roleDelegationService;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Sef
    /// Ulaşım Şefleri ve diğer yetkili kullanıcıları listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sefUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Kullanici);
        var daireBaskaniUsers = await _userManager.GetUsersInRoleAsync(AppRoles.DaireBaskani);
        var genelSekreterUsers = await _userManager.GetUsersInRoleAsync(AppRoles.GenelSekreter);

        var list = new List<SefListItemViewModel>();

        void AddUsers(IEnumerable<ApplicationUser> users, string roleName, string roleDisplayName)
        {
            foreach (var u in users)
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
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    RoleName = roleName,
                    RoleDisplayName = roleDisplayName
                });
            }
        }

        AddUsers(sefUsers, AppRoles.Kullanici, "Ulaşım Şefi");
        AddUsers(daireBaskaniUsers, AppRoles.DaireBaskani, "Daire Başkanı");
        AddUsers(genelSekreterUsers, AppRoles.GenelSekreter, "Genel Sekreter");

        return View(list.OrderByDescending(x => x.CreatedAt).ToList());
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
        // 0. Güvenlik Kuralı: DaireBaskani ve GenelSekreter hesaplarını YALNIZCA saf Admin oluşturabilir
        var isRealAdmin = User.IsAdmin();
        var targetRole = string.IsNullOrWhiteSpace(model.Role) ? AppRoles.Kullanici : model.Role.Trim();

        if ((targetRole == AppRoles.DaireBaskani || targetRole == AppRoles.GenelSekreter || targetRole == AppRoles.Admin) && !isRealAdmin)
        {
            _logger.LogWarning("Yetkisiz rol oluşturma teşebbüsü! Kullanıcı: {User}, Hedef Rol: {TargetRole}", User.Identity?.Name, targetRole);
            ModelState.AddModelError("Role", "Daire Başkanı veya Genel Sekreter hesabı oluşturma yetkiniz bulunmamaktadır. Bu işlem yalnızca Sistem Yöneticisi (Admin) tarafından yapılabilir.");
            ModelState.AddModelError(string.Empty, "Daire Başkanı veya Genel Sekreter hesabı oluşturma yetkiniz bulunmamaktadır. Bu işlem yalnızca Sistem Yöneticisi (Admin) tarafından yapılabilir.");
            return View(model);
        }

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

        // 4. Belirlenen Rolü Ata
        await _userManager.AddToRoleAsync(user, targetRole);

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
                Role = targetRole
            });

        _logger.LogInformation("Yeni yetkili kullanıcı oluşturuldu: {UserName} ({Email}) - Rol: {Role} - Oluşturan: {Creator}", 
            user.UserName, user.Email, targetRole, User.Identity?.Name);

        TempData["SuccessMessage"] = $"'{user.FullName}' başarıyla '{targetRole}' olarak tanımlandı.";
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
        if (user == null)
        {
            TempData["ErrorMessage"] = "Belirtilen kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Güvenlik: Daire Başkanı yalnızca Şef (Kullanici) hesaplarını düzenleyebilir
        var isTargetProtected = await _userManager.IsInRoleAsync(user, AppRoles.Admin) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.DaireBaskani) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.GenelSekreter);

        if (isTargetProtected && !User.IsAdmin())
        {
            TempData["ErrorMessage"] = "Yönetici (Admin, Daire Başkanı, Genel Sekreter) hesapları üzerinde işlem yapma yetkiniz yoktur.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? AppRoles.Kullanici;

        var model = new SefEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            RoleName = primaryRole,
            RoleDisplayName = primaryRole switch
            {
                AppRoles.Admin => "Sistem Yöneticisi",
                AppRoles.DaireBaskani => "Daire Başkanı",
                AppRoles.GenelSekreter => "Genel Sekreter",
                _ => "Ulaşım Şefi"
            }
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
        if (user == null)
        {
            TempData["ErrorMessage"] = "Güncellenecek kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var currentAdmin = await _userManager.GetUserAsync(User);

        // Güvenlik Kontrolü 1: Kimse kendi hesabını düzenleyemez / rolünü değiştiremez
        if (currentAdmin?.Id == user.Id)
        {
            TempData["ErrorMessage"] = "Kendi hesabınızı bu ekrandan düzenleyemezsiniz. Lütfen Profilim sayfasını kullanınız.";
            return RedirectToAction(nameof(Index));
        }

        // Güvenlik Kontrolü 2: Daire Başkanı Admin/Daire Başkanı/Genel Sekreter'i düzenleyemez
        var isTargetProtected = await _userManager.IsInRoleAsync(user, AppRoles.Admin) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.DaireBaskani) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.GenelSekreter);

        if (isTargetProtected && !User.IsAdmin())
        {
            TempData["ErrorMessage"] = "Yönetici (Admin, Daire Başkanı, Genel Sekreter) hesaplarını düzenleme yetkiniz yoktur.";
            return RedirectToAction(nameof(Index));
        }

        // Güvenlik Kontrolü 3: Yetki Yükseltme Koruması (Role Elevation Protection)
        // Yalnızca saf Admin üst düzey rolleri (Admin, DaireBaskani, GenelSekreter) atayabilir.
        if (!string.IsNullOrWhiteSpace(model.Role) && (model.Role == AppRoles.Admin || model.Role == AppRoles.DaireBaskani || model.Role == AppRoles.GenelSekreter))
        {
            if (!User.IsAdmin())
            {
                _logger.LogWarning("Yetkisiz rol yükseltme teşebbüsü engellendi! Kullanıcı: {Actor}, Hedef Rol: {Role}", User.Identity?.Name, model.Role);
                TempData["ErrorMessage"] = "Yönetici rollerine (Admin, Daire Başkanı, Genel Sekreter) kullanıcı atama yetkiniz yoktur.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Eğer saf Admin rol değiştirmek istediyse rolü güncelle
        if (User.IsAdmin() && !string.IsNullOrWhiteSpace(model.Role))
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, userRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }
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
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Güvenlik Kontrolü: Daire Başkanı Admin/Daire Başkanı/Genel Sekreter'i donduramaz
        var isTargetProtected = await _userManager.IsInRoleAsync(user, AppRoles.Admin) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.DaireBaskani) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.GenelSekreter);

        if (isTargetProtected && !User.IsAdmin())
        {
            TempData["ErrorMessage"] = "Yönetici (Admin, Daire Başkanı, Genel Sekreter) hesaplarını pasifleştirme yetkiniz yoktur.";
            return RedirectToAction(nameof(Index));
        }

        // Kendi kendini donduramaz
        var currentAdmin = await _userManager.GetUserAsync(User);
        if (currentAdmin?.Id == user.Id)
        {
            TempData["ErrorMessage"] = "Kendi hesabınızı donduramazsınız.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        if (!user.IsActive)
        {
            // DÜZELTME 7b: Hesap pasifleştirilirse aktif vekâletler otomatik iptal edilsin
            await _roleDelegationService.AutoRevokeDelegationsForUserAsync(
                user.Id, 
                currentAdmin?.Id ?? "System", 
                "Kullanıcı hesabı pasifleştirildiği için vekâlet otomatik sonlandırıldı.");
        }

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
        if (user == null)
        {
            TempData["ErrorMessage"] = "Şifresi sıfırlanacak kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Güvenlik Kontrolü: Daire Başkanı Admin/Daire Başkanı/Genel Sekreter şifresini sıfırlayamaz!
        var isTargetProtected = await _userManager.IsInRoleAsync(user, AppRoles.Admin) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.DaireBaskani) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.GenelSekreter);

        if (isTargetProtected && !User.IsAdmin())
        {
            _logger.LogWarning("Yetkisiz şifre sıfırlama teşebbüsü engellendi! Kullanıcı: {Actor}, Hedef: {Target}", User.Identity?.Name, user.UserName);
            TempData["ErrorMessage"] = "Yönetici (Admin, Daire Başkanı, Genel Sekreter) hesaplarının şifresini sıfırlama yetkiniz bulunmamaktadır.";
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
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "İşlem yapılacak kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Güvenlik Kontrolü: Daire Başkanı Admin/Daire Başkanı/Genel Sekreter'i silemez!
        var isTargetProtected = await _userManager.IsInRoleAsync(user, AppRoles.Admin) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.DaireBaskani) ||
                                await _userManager.IsInRoleAsync(user, AppRoles.GenelSekreter);

        if (isTargetProtected && !User.IsAdmin())
        {
            TempData["ErrorMessage"] = "Yönetici (Admin, Daire Başkanı, Genel Sekreter) hesaplarını silme veya pasife alma yetkiniz yoktur.";
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

        // DÜZELTME 7b: Hesap silindiğinde/pasife alındığında aktif vekâletler otomatik iptal edilsin
        await _roleDelegationService.AutoRevokeDelegationsForUserAsync(
            user.Id, 
            currentAdmin?.Id ?? "System", 
            "Kullanıcı hesabı silindiği/pasife alındığı için vekâlet otomatik sonlandırıldı.");

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

        _logger.LogInformation("Kullanıcı hesabı pasife alındı (Soft-Delete): {UserName} ({FullName}) - İşlemi Yapan: {Actor}",
            user.UserName, user.FullName, User.Identity?.Name);

        TempData["SuccessMessage"] = $"'{user.FullName}' ({user.UserName}) hesabı başarıyla pasife alındı (donduruldu). Geçmiş işlem ve denetim kayıtları korundu.";
        return RedirectToAction(nameof(Index));
    }
}
