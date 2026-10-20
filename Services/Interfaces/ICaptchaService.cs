// File: Services/Interfaces/ICaptchaService.cs
namespace aractakipsys.Services.Interfaces;

/// <summary>
/// Üretilen Captcha görseli ve şifrelenmiş güvenlik token'ını barındıran sonuç modeli
/// </summary>
public record CaptchaResult(string SvgDataUri, string Token);

/// <summary>
/// Giriş ve form işlemlerinde kaba kuvvet (brute-force) saldırılarını engellemek için Captcha servis arayüzü
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Yeni bir rastgele güvenlik kodu, SVG görseli ve DataProtection korumalı token üretir.
    /// </summary>
    CaptchaResult GenerateCaptcha();

    /// <summary>
    /// Kullanıcı tarafından girilen güvenlik kodunu, şifreli token içindeki değer ve son kullanma süresiyle karşılaştırır.
    /// </summary>
    /// <param name="inputCode">Kullanıcının girdiği kod</param>
    /// <param name="token">Formdan gelen şifrelenmiş token</param>
    /// <returns>Doğrulama başarılı ise true, aksi halde false</returns>
    bool ValidateCaptcha(string? inputCode, string? token);
}
