// File: Services/Implementations/CaptchaService.cs
using System.Security.Cryptography;
using System.Text;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace aractakipsys.Services.Implementations;

/// <summary>
/// Harici API gerektirmeyen, bağımsız ve güvenli dinamik SVG Captcha servisi
/// </summary>
public class CaptchaService : ICaptchaService
{
    private readonly IDataProtector _protector;

    // Karıştırılabilecek karakterler (0, O, 1, I, l) hariç tutulmuş güvenli karakter havuzu
    private const string CharacterPool = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static readonly string[] TextColors = { "#1e3c72", "#2a5298", "#0f172a", "#0369a1", "#1d4ed8", "#047857", "#b91c1c", "#4338ca" };
    private static readonly string[] NoiseColors = { "#94a3b8", "#cbd5e1", "#64748b", "#38bdf8", "#818cf8" };

    public CaptchaService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("aractakipsys.CaptchaProtection.v1");
    }

    /// <summary>
    /// Yeni bir rastgele güvenlik kodu, SVG görseli ve şifreli token üretir.
    /// </summary>
    public CaptchaResult GenerateCaptcha()
    {
        // 5 karakterli rastgele kod üretimi
        var codeBuilder = new StringBuilder(5);
        for (int i = 0; i < 5; i++)
        {
            var charIndex = RandomNumberGenerator.GetInt32(0, CharacterPool.Length);
            codeBuilder.Append(CharacterPool[charIndex]);
        }
        var code = codeBuilder.ToString();

        // SVG Görseli oluştur
        var svg = GenerateSvgImage(code);
        var svgDataUri = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";

        // 3 dakika geçerli şifrelenmiş token üret (Format: CAPTCHA:{kod}:{sonGecerlilikZamani}:{tekilGuvenlikKimligi})
        var expiry = DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");
        var payload = $"CAPTCHA:{code}:{expiry}:{nonce}";
        var token = _protector.Protect(payload);

        return new CaptchaResult(svgDataUri, token);
    }

    /// <summary>
    /// Girilen güvenlik kodunu şifreli token ile doğrular.
    /// </summary>
    public bool ValidateCaptcha(string? inputCode, string? token)
    {
        if (string.IsNullOrWhiteSpace(inputCode) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmedCode = inputCode.Trim();

        try
        {
            var decryptedPayload = _protector.Unprotect(token);
            var parts = decryptedPayload.Split(':');

            // Format kontrolü: CAPTCHA:{code}:{expiry}:{nonce}
            if (parts.Length != 4 || parts[0] != "CAPTCHA")
            {
                return false;
            }

            var expectedCode = parts[1];
            if (!long.TryParse(parts[2], out var expiryUnixTime))
            {
                return false;
            }

            // Süre aşımı kontrolü (3 dakika)
            var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentUnixTime > expiryUnixTime)
            {
                return false;
            }

            // Büyük/küçük harf duyarsız (Case-insensitive) karşılaştırma
            return string.Equals(expectedCode, trimmedCode, StringComparison.OrdinalIgnoreCase);
        }
        catch (CryptographicException)
        {
            // Token bozulmuş, geçersiz veya tahrif edilmiş
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Bot ayrıştırmasını zorlaştıracak gürültü çizgileri ve rastgele açılı harfler içeren vektörel SVG oluşturur.
    /// </summary>
    private static string GenerateSvgImage(string code)
    {
        const int width = 140;
        const int height = 46;

        var sb = new StringBuilder();
        sb.Append($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" style="user-select: none;">""");
        
        // Arka plan kutusu ve hafif degradeli renk
        sb.Append($"""<rect width="{width}" height="{height}" rx="8" fill="#f8fafc" stroke="#e2e8f0" stroke-width="1.5" />""");

        // Gürültü noktacıkları (25 adet rastgele nokta)
        for (int i = 0; i < 25; i++)
        {
            var cx = RandomNumberGenerator.GetInt32(5, width - 5);
            var cy = RandomNumberGenerator.GetInt32(5, height - 5);
            var r = RandomNumberGenerator.GetInt32(1, 3);
            var color = NoiseColors[RandomNumberGenerator.GetInt32(0, NoiseColors.Length)];
            var opacity = (RandomNumberGenerator.GetInt32(3, 8) / 10.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            sb.Append($"""<circle cx="{cx}" cy="{cy}" r="{r}" fill="{color}" opacity="{opacity}" />""");
        }

        // Yanıltıcı eğri ve gürültü çizgileri (4 adet)
        for (int i = 0; i < 4; i++)
        {
            var x1 = RandomNumberGenerator.GetInt32(5, 40);
            var y1 = RandomNumberGenerator.GetInt32(5, height - 5);
            var qx = RandomNumberGenerator.GetInt32(40, 100);
            var qy = RandomNumberGenerator.GetInt32(5, height - 5);
            var x2 = RandomNumberGenerator.GetInt32(90, width - 5);
            var y2 = RandomNumberGenerator.GetInt32(5, height - 5);
            var color = NoiseColors[RandomNumberGenerator.GetInt32(0, NoiseColors.Length)];
            var strokeWidth = (RandomNumberGenerator.GetInt32(10, 20) / 10.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            sb.Append($"""<path d="M{x1},{y1} Q{qx},{qy} {x2},{y2}" stroke="{color}" stroke-width="{strokeWidth}" fill="none" opacity="0.6" />""");
        }

        // Karakterleri çiz
        for (int i = 0; i < code.Length; i++)
        {
            var ch = code[i];
            var x = 16 + (i * 24) + RandomNumberGenerator.GetInt32(-2, 3);
            var y = 32 + RandomNumberGenerator.GetInt32(-3, 3);
            var rot = RandomNumberGenerator.GetInt32(-18, 19);
            var fontSize = RandomNumberGenerator.GetInt32(23, 27);
            var color = TextColors[RandomNumberGenerator.GetInt32(0, TextColors.Length)];

            sb.Append($"""<text x="{x}" y="{y}" font-family="'Courier New', Consolas, monospace" font-size="{fontSize}" font-weight="bold" fill="{color}" transform="rotate({rot}, {x}, {y})">{ch}</text>""");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
