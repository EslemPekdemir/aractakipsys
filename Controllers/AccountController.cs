// File: Controllers/AccountController.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using aractakipsys.Services.Interfaces;

namespace aractakipsys.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<AccountController> _logger;
    private readonly IEmailService _emailService;
    private readonly ICaptchaService _captchaService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment,
        ILogger<AccountController> logger,
        IEmailService emailService,
        ICaptchaService captchaService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
        _emailService = emailService;
        _captchaService = captchaService;
    }

    /// <summary>
    /// Kullanıcı Giriş Ekranı (GET: /Account/Login)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectUserByRole();
        }

        var captcha = _captchaService.GenerateCaptcha();
        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            CaptchaToken = captcha.Token,
            CaptchaSvgImage = captcha.SvgDataUri
        };

        return View(model);
    }

    /// <summary>
    /// Güvenlik Kodu Yenileme Eylemi (GET: /Account/GetNewCaptcha)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("captcha-limit")]
    public IActionResult GetNewCaptcha()
    {
        var captcha = _captchaService.GenerateCaptcha();
        return Json(new { success = true, svg = captcha.SvgDataUri, token = captcha.Token });
    }

    /// <summary>
    /// Kullanıcı Giriş Eylemi (POST: /Account/Login)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login-limit")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        // Geliştirme ortamında çalışan otomatik test scriptlerinin (PowerShell vb.) kesintiye uğramaması için bypass kontrolü
        var isDevScriptBypass = _webHostEnvironment.IsDevelopment() && string.IsNullOrEmpty(model.CaptchaCode) && string.IsNullOrEmpty(model.CaptchaToken);

        if (isDevScriptBypass)
        {
            ModelState.Remove(nameof(model.CaptchaCode));
        }

        if (!ModelState.IsValid)
        {
            RefreshModelCaptcha(model);
            return View(model);
        }

        // Kaba kuvvet (Brute-Force) koruması: Kullanıcı adı/şifre kontrolünden ÖNCE Captcha doğrulanır
        if (!isDevScriptBypass)
        {
            if (!_captchaService.ValidateCaptcha(model.CaptchaCode, model.CaptchaToken))
            {
                _logger.LogWarning("Başarısız Captcha doğrulaması: {Username}", model.Username);
                ModelState.AddModelError(nameof(model.CaptchaCode), "Güvenlik kodu hatalı veya süresi dolmuş.");
                RefreshModelCaptcha(model);
                return View(model);
            }
        }

        // Kullanıcıyı kullanıcı adı veya e-posta ile ara
        var user = await _userManager.FindByNameAsync(model.Username) 
                   ?? await _userManager.FindByEmailAsync(model.Username);

        if (user == null)
        {
            _logger.LogWarning("Başarısız oturum açma denemesi: Kullanıcı bulunamadı ({Username})", model.Username);
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            RefreshModelCaptcha(model);
            return View(model);
        }

        // Hesap aktiflik kontrolü
        if (!user.IsActive)
        {
            _logger.LogWarning("Pasif hesap giriş denemesi: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Hesabınız pasife alınmıştır. Lütfen sistem yöneticisi ile iletişime geçiniz.");
            RefreshModelCaptcha(model);
            return View(model);
        }

        // Giriş doğrulaması (Brute-Force koruması için Lockout açık)
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Kullanıcı giriş yaptı: {Username}", user.UserName);

            var roles = await _userManager.GetRolesAsync(user);
            var isDriver = roles.Contains("Driver");
            var isAdminOrKullanici = roles.Contains("Admin") || roles.Contains("Kullanici");

            // ReturnUrl doğrulama: Sadece kullanıcının rolüyle yetki uyumluysa ReturnUrl'e yönlendir
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                var returnUrlLower = model.ReturnUrl.ToLowerInvariant();

                if (isDriver && !isAdminOrKullanici)
                {
                    // Şoför yalnızca /surucu veya kendi /account sayfalarına gidebilir; aksi takdirde görmezden gel
                    if (returnUrlLower.StartsWith("/surucu") || returnUrlLower.StartsWith("/account"))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    _logger.LogInformation("Şoför kullanıcısı için yetkisiz ReturnUrl ({ReturnUrl}) yok sayıldı, /Surucu'ya yönlendiriliyor.", model.ReturnUrl);
                }
                else if (isAdminOrKullanici)
                {
                    // Admin veya Şef/Kullanıcı şoför portalına (/surucu) gidemez; harici panel sayfalarını açabilir
                    if (!returnUrlLower.StartsWith("/surucu"))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    _logger.LogInformation("Yönetici/Kullanıcı için uyumsuz ReturnUrl ({ReturnUrl}) yok sayıldı, Dashboard'a yönlendiriliyor.", model.ReturnUrl);
                }
            }

            // Rol bazlı varsayılan yönlendirme
            if (isDriver && !isAdminOrKullanici)
            {
                return RedirectToAction("Index", "Surucu");
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Hesap kilitlendi: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Çok sayıda hatalı giriş denemesi nedeniyle hesabınız 15 dakika süreyle kilitlenmiştir.");
            RefreshModelCaptcha(model);
            return View(model);
        }

        _logger.LogWarning("Hatalı parola girişi: {Username}", model.Username);
        ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
        RefreshModelCaptcha(model);
        return View(model);
    }

    /// <summary>
    /// Model yeniden render edilirken yeni bir tek kullanımlık Captcha üretir
    /// </summary>
    private void RefreshModelCaptcha(LoginViewModel model)
    {
        var newCaptcha = _captchaService.GenerateCaptcha();
        model.CaptchaToken = newCaptcha.Token;
        model.CaptchaSvgImage = newCaptcha.SvgDataUri;
        model.CaptchaCode = string.Empty;
    }

    /// <summary>
    /// Şifremi Unuttum Ekranı (GET: /Account/ForgotPassword)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectUserByRole();
        }
        return View(new ForgotPasswordViewModel());
    }

    /// <summary>
    /// Şifremi Unuttum Eylemi (POST: /Account/ForgotPassword)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("forgot-password-limit")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cleanInput = model.EmailOrUsername.Trim();
        var user = await _userManager.FindByEmailAsync(cleanInput) 
                   ?? await _userManager.FindByNameAsync(cleanInput);

        if (user != null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            var subject = "Şifre Sıfırlama Talebi — Düzce Üniversitesi Araç Takip Sistemi";
            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 25px; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #e5e7eb; border-radius: 8px;'>
                    <h2 style='color: #1e3c72; margin-top: 0;'>Düzce Üniversitesi Araç Takip Sistemi</h2>
                    <p>Merhaba <strong>{user.FullName}</strong>,</p>
                    <p>Hesabınız için şifre sıfırlama talebinde bulunuldu. Şifrenizi yenilemek için lütfen aşağıdaki butona tıklayınız:</p>
                    <p style='margin: 25px 0;'>
                        <a href='{resetLink}' style='background-color: #1e3c72; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Şifremi Sıfırla</a>
                    </p>
                    <p style='color: #666; font-size: 0.9rem;'>Eğer buton çalışmıyorsa aşağıdaki bağlantıyı tarayıcınıza kopyalayabilirsiniz:</p>
                    <p style='word-break: break-all; color: #2563eb; font-size: 0.85rem;'>{resetLink}</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 25px 0;' />
                    <small style='color: #888;'>Bu talebi siz yapmadıysanız lütfen bu e-postayı dikkate almayınız. Şifreniz siz yeni bir şifre belirleyene kadar değişmeyecektir.</small>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(user.Email!, subject, body, isHtml: true);
                _logger.LogInformation("Şifre sıfırlama e-postası gönderildi: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilirken hata oluştu: {Email}", user.Email);
            }
        }

        ViewBag.SuccessMessage = "Şifre sıfırlama talebiniz işleme alınmıştır. Sistemde kayıtlı olan e-posta adresine talimatlar gönderilmiştir.";
        return View("ForgotPasswordConfirmation");
    }

    /// <summary>
    /// Şifre Sıfırlama Ekranı (GET: /Account/ResetPassword)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı.";
            return RedirectToAction(nameof(Login));
        }

        var model = new ResetPasswordViewModel
        {
            Token = token,
            Email = email
        };

        return View(model);
    }

    /// <summary>
    /// Şifre Sıfırlama Eylemi (POST: /Account/ResetPassword)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("reset-password-limit")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Güvenlik için kullanıcı bulunamasa dahi başarılı gibi yönlendir
            TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Kullanıcı şifresini başarıyla sıfırladı: {Email}", model.Email);
            TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Çıkış İşlemi (POST: /Account/Logout)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Kullanıcı çıkış yaptı.");
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Çıkış İşlemi (GET: /Account/Logout - Layout linkleri için güvenli yönlendirme)
    /// </summary>
    [HttpGet]
    [ActionName("Logout")]
    public async Task<IActionResult> LogoutGet()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Kullanıcı çıkış yaptı.");
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Kullanıcı Profil Ekranı (GET: /Account/Profile)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var model = new ProfileViewModel
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };

        return View(model);
    }

    /// <summary>
    /// Kullanıcı Profil Güncelleme (POST: /Account/Profile)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Güncellenebilir alanlar: FullName, PhoneNumber
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError(nameof(model.FullName), "Ad Soyad alanı zorunludur.");
        }

        string? originalProfilePictureUrl = user.ProfilePictureUrl;
        string? newlyUploadedFilePath = null;
        string? oldPhotoToCleanOnSuccess = null;

        // Profil Fotoğrafı Yükleme (Magic Number & Boyut & Uzantı Doğrulaması)
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            if (!IsValidImageFile(model.ProfilePicture, out var fileValidationError))
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), fileValidationError!);
            }
            else
            {
                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Eski özel fotoğraf silinmek üzere işaretlenir
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/profiles/"))
                    {
                        oldPhotoToCleanOnSuccess = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                    }

                    var fileExt = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                    var uniqueFileName = $"{user.Id}_{DateTime.UtcNow.Ticks}{fileExt}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }

                    newlyUploadedFilePath = filePath;
                    user.ProfilePictureUrl = $"/uploads/profiles/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profil fotoğrafı kaydedilirken hata oluştu.");
                    ModelState.AddModelError(string.Empty, "Profil fotoğrafı yüklenemedi.");
                }
            }
        }

        // Parola değiştirme yalnızca kullanıcı bilinçli olarak "Yeni Parola" kutusunu doldurduğunda devreye girer
        bool isPasswordChangeRequested = !string.IsNullOrWhiteSpace(model.NewPassword);
        if (isPasswordChangeRequested)
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Mevcut parolanızı girmelisiniz.");
            }
            if (model.NewPassword!.Length < 10)
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Yeni parola en az 10 karakter uzunluğunda olmalıdır.");
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Yeni parolalar birbiriyle eşleşmiyor.");
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        model.Roles = roles.ToList();
        model.Username = user.UserName ?? string.Empty;
        model.Email = user.Email ?? string.Empty;
        model.CreatedAt = user.CreatedAt;
        model.IsActive = user.IsActive;
        model.ProfilePictureUrl = user.ProfilePictureUrl;

        if (!ModelState.IsValid)
        {
            // Validasyon hatası oluştuysa yeni yüklenen dosyayı sunucudan temizle
            if (newlyUploadedFilePath != null && System.IO.File.Exists(newlyUploadedFilePath))
            {
                try { System.IO.File.Delete(newlyUploadedFilePath); } catch { }
            }
            model.ProfilePictureUrl = originalProfilePictureUrl;
            return View(model);
        }

        // 1. Bilgileri Güncelle
        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            if (newlyUploadedFilePath != null && System.IO.File.Exists(newlyUploadedFilePath))
            {
                try { System.IO.File.Delete(newlyUploadedFilePath); } catch { }
            }
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.ProfilePictureUrl = originalProfilePictureUrl;
            return View(model);
        }

        // Eski fotoğrafı fiziksel diskten temizle (başarılı güncelleme sonrası)
        if (oldPhotoToCleanOnSuccess != null && System.IO.File.Exists(oldPhotoToCleanOnSuccess))
        {
            try { System.IO.File.Delete(oldPhotoToCleanOnSuccess); } catch { }
        }

        // 2. Parola Değiştir (İstenmişse)
        if (isPasswordChangeRequested)
        {
            var changePassResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);
            if (!changePassResult.Succeeded)
            {
                foreach (var error in changePassResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Profil bilgileriniz ve parolanız başarıyla güncellendi.";
        }
        else
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
        }

        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Profil Fotoğrafını Kaldır (POST: /Account/RemoveProfilePhoto)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProfilePhoto()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null && !string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            if (user.ProfilePictureUrl.StartsWith("/uploads/profiles/"))
            {
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Profil fotoğrafınız kaldırıldı ve varsayılana dönüldü.";
        }

        return RedirectToAction(nameof(Profile));
    }

    /// <summary>
    /// Yetkisiz Erişim Ekranı (GET: /Account/AccessDenied)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectUserByRole()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Kullanici"))
        {
            return RedirectToAction("Index", "Home");
        }
        else if (User.IsInRole("Driver"))
        {
            return RedirectToAction("Index", "Surucu");
        }
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Profil fotoğrafı güvenlik doğrulaması (Uzantı, Boyut ve Magic Number / Dosya Başlığı Baytları)
    /// Saldırganların zararlı scriptlerin uzantısını değiştirerek sisteme yüklemesini engeller.
    /// </summary>
    private static bool IsValidImageFile(IFormFile file, out string? errorMessage)
    {
        errorMessage = null;

        if (file == null || file.Length == 0)
        {
            errorMessage = "Lütfen geçerli bir resim dosyası seçiniz.";
            return false;
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            errorMessage = "Profil fotoğrafı boyutu en fazla 5 MB olabilir.";
            return false;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(ext))
        {
            errorMessage = "Sadece .jpg, .jpeg, .png veya .webp formatında resim yükleyebilirsiniz.";
            return false;
        }

        // Magic Number (Dosya başlığı baytları) doğrulaması
        try
        {
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[12];
            var bytesRead = stream.Read(headerBytes, 0, 12);

            if (bytesRead < 4)
            {
                errorMessage = "Geçersiz veya bozuk dosya içeriği.";
                return false;
            }

            // JPEG/JPG: FF D8 FF
            if (ext is ".jpg" or ".jpeg")
            {
                if (headerBytes[0] != 0xFF || headerBytes[1] != 0xD8 || headerBytes[2] != 0xFF)
                {
                    errorMessage = "Dosya içeriği geçerli bir JPEG/JPG resmi değil.";
                    return false;
                }
            }
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            else if (ext == ".png")
            {
                if (bytesRead < 8 ||
                    headerBytes[0] != 0x89 || headerBytes[1] != 0x50 || headerBytes[2] != 0x4E || headerBytes[3] != 0x47 ||
                    headerBytes[4] != 0x0D || headerBytes[5] != 0x0A || headerBytes[6] != 0x1A || headerBytes[7] != 0x0A)
                {
                    errorMessage = "Dosya içeriği geçerli bir PNG resmi değil.";
                    return false;
                }
            }
            // WebP: RIFF .... WEBP
            else if (ext == ".webp")
            {
                if (bytesRead < 12 ||
                    headerBytes[0] != 0x52 || headerBytes[1] != 0x49 || headerBytes[2] != 0x46 || headerBytes[3] != 0x46 || // RIFF
                    headerBytes[8] != 0x57 || headerBytes[9] != 0x45 || headerBytes[10] != 0x42 || headerBytes[11] != 0x50) // WEBP
                {
                    errorMessage = "Dosya içeriği geçerli bir WebP resmi değil.";
                    return false;
                }
            }
        }
        catch
        {
            errorMessage = "Dosya içeriği okunamadı veya doğrulanamadı.";
            return false;
        }

        return true;
    }
}
