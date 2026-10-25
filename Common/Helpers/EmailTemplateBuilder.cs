// File: Common/Helpers/EmailTemplateBuilder.cs
using System.Net;
using aractakipsys.Models.Entities;

namespace aractakipsys.Common.Helpers;

/// <summary>
/// Düzce Üniversitesi kurumsal kimliğine uygun, modern, duyarlı (responsive) ve şık HTML e-posta şablonları üretir.
/// </summary>
public static class EmailTemplateBuilder
{
    private const string DefaultDepartmentTitle = "Düzce Üniversitesi Ulaşım Şefliği";
    private const string DefaultOrganizationUnit = "Ulaşım Şefliği &bull; Bilgi İşlem Daire Başkanlığı";

    /// <summary>
    /// Araç talebi onaylandığında ve görevlendirme yapıldığında gönderilen üst düzey kurumsal HTML e-posta şablonu.
    /// </summary>
    public static string BuildRequestFulfilledEmailHtml(
        VehicleRequest request, 
        Assignment assignment, 
        string? departmentTitle = null, 
        string? organizationUnit = null,
        string? chiefNote = null,
        string? contactFullName = null,
        string? contactPhone = null,
        string? contactEmail = null)
    {
        var activeDepartment = !string.IsNullOrWhiteSpace(departmentTitle) ? departmentTitle : DefaultDepartmentTitle;
        var activeUnit = !string.IsNullOrWhiteSpace(organizationUnit) ? organizationUnit : DefaultOrganizationUnit;

        var requesterName = WebUtility.HtmlEncode(request.RequesterFullName ?? "Sayın Yetkili");
        var department = WebUtility.HtmlEncode(request.Department ?? "Belirtilmedi");
        // Görev Amacı: Karşı tarafın (talep sahibinin) e-postada görmemesi için [Talep #...] veya Talep #6 gibi ibareleri temizle
        var rawPurpose = !string.IsNullOrWhiteSpace(request.Purpose) 
            ? request.Purpose 
            : (assignment.Purpose ?? "Resmi Kurum Görevi");

        rawPurpose = System.Text.RegularExpressions.Regex.Replace(
            rawPurpose, 
            @"^\[?\s*Talep\s*#?\s*\d+\s*\]?\s*[:-]?\s*", 
            "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

        if (string.IsNullOrWhiteSpace(rawPurpose))
        {
            rawPurpose = "Resmi Kurum Görevi";
        }

        var purpose = WebUtility.HtmlEncode(rawPurpose);
        var plateNumber = WebUtility.HtmlEncode(assignment.Vehicle?.PlateNumber ?? "81 DU ---");
        var vehicleModel = WebUtility.HtmlEncode(assignment.Vehicle != null ? $"{assignment.Vehicle.Brand} {assignment.Vehicle.Model}" : "Tahsis Edilen Araç");
        
        // FAZ 6 KURALI: Görevli şoförün yalnızca ADI görünür, telefonu/e-postası görünmez!
        var driverName = WebUtility.HtmlEncode(assignment.Driver?.User?.FullName ?? $"Şoför #{assignment.DriverId}");
        
        // İletişim Kişisi (Ulaşım Şefi / İletişim Personeli)
        var contactName = WebUtility.HtmlEncode(contactFullName ?? "Ulaşım Koordinasyon Şefliği");
        var contactPhoneDisplay = !string.IsNullOrWhiteSpace(contactPhone) ? contactPhone : "Kurum Santrali";
        var contactPhoneClean = new string((contactPhone ?? "").Where(char.IsDigit).ToArray());
        var contactEmailDisplay = WebUtility.HtmlEncode(contactEmail ?? "aractakip@duzce.edu.tr");

        var startTime = assignment.PlannedStartTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        var endTime = assignment.PlannedEndTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        var driverSectionHtml = $@"<strong style=""font-weight: 600; color: #0f172a; font-size: 14px;"">{driverName}</strong>";

        var contactSectionHtml = $@"
            <div style=""font-weight: 600; color: #0f172a; font-size: 14px; margin-bottom: 3px;"">{contactName}</div>
            <div style=""font-size: 13px; color: #475569;"">
                {(string.IsNullOrWhiteSpace(contactPhoneClean) ? "" : $@"
                <span style=""margin-right: 12px;"">
                    Tel: <a href=""tel:{contactPhoneClean}"" style=""color: #2563eb; text-decoration: none; font-weight: 600;"">{contactPhoneDisplay}</a>
                </span>")}
                <span>
                    E-Posta: <a href=""mailto:{contactEmailDisplay}"" style=""color: #2563eb; text-decoration: none; font-weight: 500;"">{contactEmailDisplay}</a>
                </span>
            </div>";

        var reminderText = "Görev başlangıç saatinden <strong>en az 15 dakika önce</strong> tahsis edilen araç başında hazır bulunulması, görevle ilgili her türlü soru ve koordinasyon için yukarıda belirtilen <strong>İletişim Kişisi</strong> ile irtibat kurulması önemle rica olunur.";

        var chiefNoteRowHtml = "";
        if (!string.IsNullOrWhiteSpace(chiefNote))
        {
            var encodedChiefNote = WebUtility.HtmlEncode(chiefNote.Trim());
            chiefNoteRowHtml = $@"
                                <!-- Şeflik Notu / Özel Talimat -->
                                <tr>
                                    <td style=""padding: 13px 18px; font-size: 13px; color: #b45309; font-weight: 700; vertical-align: top; background-color: #fffbeb;"">
                                        Şeflik Notu
                                    </td>
                                    <td style=""padding: 13px 18px; font-size: 13px; color: #92400e; line-height: 1.5; font-weight: 500; background-color: #fffbeb;"">
                                        {encodedChiefNote}
                                    </td>
                                </tr>";
        }

        var html = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Araç Talebiniz Onaylandı</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, 'Helvetica Neue', Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f1f5f9; padding: 32px 10px;"">
        <tr>
            <td align=""center"">
                <!-- ANA KART KONTEYNER -->
                <table role=""presentation"" width=""100%"" style=""max-width: 630px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.1), 0 8px 10px -6px rgba(15, 23, 42, 0.05); border: 1px solid #e2e8f0;"" cellspacing=""0"" cellpadding=""0"">
                    
                    <!-- 1. KURUMSAL HEADER -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #091e3a 0%, #1e3a8a 100%); padding: 36px 28px 30px 28px; text-align: center; border-bottom: 3px solid #d97706;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td align=""center"">
                                        <!-- DÜZCE ÜNİVERSİTESİ ETİKETİ -->
                                        <div style=""display: inline-block; background-color: rgba(255, 255, 255, 0.12); border: 1px solid rgba(255, 255, 255, 0.22); border-radius: 50px; padding: 6px 20px; margin-bottom: 14px; backdrop-filter: blur(4px);"">
                                            <span style=""color: #e2e8f0; font-size: 11px; font-weight: 800; letter-spacing: 2px; text-transform: uppercase;"">DÜZCE ÜNİVERSİTESİ</span>
                                        </div>
                                        <h1 style=""margin: 0; color: #ffffff; font-size: 23px; font-weight: 800; letter-spacing: -0.4px; line-height: 1.3;"">Araç Takip ve Görevlendirme Sistemi</h1>
                                        <p style=""margin: 8px 0 0 0; color: #93c5fd; font-size: 13px; font-weight: 500;"">{activeUnit}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- 2. DURUM HERO ŞERİDİ -->
                    <tr>
                        <td style=""background: linear-gradient(180deg, #ecfdf5 0%, #f0fdf4 100%); border-bottom: 1px solid #bbf7d0; padding: 18px 28px; text-align: center;"">
                            <div style=""display: inline-block; background-color: #10b981; color: #ffffff; font-size: 10px; font-weight: 900; padding: 4px 12px; border-radius: 20px; letter-spacing: 1px; text-transform: uppercase; margin-bottom: 6px;"">
                                &#10004; TALEBİNİZ ONAYLANDI
                            </div>
                            <div style=""color: #065f46; font-size: 17px; font-weight: 800; letter-spacing: -0.2px;"">
                                Resmi Araç ve Şoför Görevlendirmesi Tamamlandı
                            </div>
                        </td>
                    </tr>

                    <!-- 3. GÖVDE İÇERİĞİ -->
                    <tr>
                        <td style=""padding: 32px 30px;"">
                            
                            <!-- Hitap ve Giriş -->
                            <p style=""margin: 0 0 10px 0; font-size: 16px; color: #0f172a; line-height: 1.5;"">
                                Sayın <strong>{requesterName}</strong>,
                            </p>
                            <p style=""margin: 0 0 24px 0; font-size: 14px; color: #475569; line-height: 1.6;"">
                                Yapmış olduğunuz resmi araç talebi <strong>{activeDepartment}</strong> tarafından incelenmiş, uygun görülerek resmi görevlendirme ve araç tahsisi yapılmıştır.
                            </p>

                            <!-- BİLGİ KARTI -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; margin-bottom: 24px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.02);"">
                                <tr>
                                    <td colspan=""2"" style=""background: linear-gradient(90deg, #f1f5f9 0%, #f8fafc 100%); padding: 12px 18px; border-bottom: 1px solid #e2e8f0; font-size: 11px; font-weight: 800; color: #475569; letter-spacing: 1px; text-transform: uppercase;"">
                                        ARAÇ VE TAHSİS BİLGİLERİ
                                    </td>
                                </tr>
                                
                                <!-- Talep Numarası -->
                                <tr>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; width: 34%; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Talep Numarası
                                    </td>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px;"">
                                        <span style=""display: inline-block; background-color: #eff6ff; color: #1e40af; border: 1px solid #bfdbfe; font-weight: 800; padding: 4px 10px; border-radius: 6px; font-size: 12px;"">#TALEP-{request.Id:D4}</span>
                                    </td>
                                </tr>

                                <!-- Tahsis Edilen Araç -->
                                <tr>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Tahsis Edilen Araç
                                    </td>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 14px; color: #0f172a;"">
                                        <strong style=""color: #0f172a; font-weight: 700; letter-spacing: 0.5px;"">{plateNumber}</strong> <span style=""color: #64748b; margin: 0 4px;"">&mdash;</span> <span style=""color: #475569; font-weight: 400; font-size: 14px;"">{vehicleModel}</span>
                                    </td>
                                </tr>

                                <!-- Görevli Şoför -->
                                <tr>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Görevli Şoför
                                    </td>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 14px; color: #0f172a;"">
                                        {driverSectionHtml}
                                    </td>
                                </tr>

                                <!-- İletişim Kişisi (Ulaşım Şefi / İrtibat) -->
                                <tr>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #64748b; font-weight: 600; vertical-align: top;"">
                                        İletişim Kişisi
                                    </td>
                                    <td style=""padding: 13px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #0f172a;"">
                                        {contactSectionHtml}
                                    </td>
                                </tr>

                                <!-- Zaman Çizelgesi -->
                                <tr>
                                    <td style=""padding: 14px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Görev Çizelgesi
                                    </td>
                                    <td style=""padding: 14px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #0f172a;"">
                                        <div style=""margin-bottom: 4px;"">
                                            <span style=""display: inline-block; width: 10px; height: 10px; border-radius: 50%; background-color: #10b981; margin-right: 6px;""></span>
                                            <strong style=""color: #065f46;"">Başlangıç:</strong> {startTime}
                                        </div>
                                        <div>
                                            <span style=""display: inline-block; width: 10px; height: 10px; border-radius: 50%; background-color: #ef4444; margin-right: 6px;""></span>
                                            <strong style=""color: #991b1b;"">Tahmini Bitiş:</strong> {endTime}
                                        </div>
                                    </td>
                                </tr>

                                <!-- Talep Eden Birim -->
                                <tr>
                                    <td style=""padding: 12px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Talep Eden Birim
                                    </td>
                                    <td style=""padding: 12px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #0f172a; font-weight: 700;"">
                                        {department}
                                    </td>
                                </tr>

                                <!-- Görev Amacı -->
                                <tr>
                                    <td style=""padding: 13px 18px; {(string.IsNullOrWhiteSpace(chiefNote) ? "" : "border-bottom: 1px solid #edf2f7;")} font-size: 13px; color: #64748b; font-weight: 600; vertical-align: top;"">
                                        Görev Amacı
                                    </td>
                                    <td style=""padding: 13px 18px; {(string.IsNullOrWhiteSpace(chiefNote) ? "" : "border-bottom: 1px solid #edf2f7;")} font-size: 13px; color: #334155; line-height: 1.5;"">
                                        {purpose}
                                    </td>
                                </tr>
                                {chiefNoteRowHtml}
                            </table>

                            <!-- ÖNEMLİ BİLGİLENDİRME PANELİ -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #eff6ff; border: 1px solid #bfdbfe; border-left: 4px solid #2563eb; border-radius: 8px; padding: 14px 18px; margin-bottom: 24px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0; font-size: 12px; color: #1e40af; line-height: 1.6;"">
                                            <strong>Önemli Hatırlatma:</strong> {reminderText}
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- İMZA -->
                            <p style=""margin: 0; font-size: 13px; color: #64748b; line-height: 1.6;"">
                                İyi çalışmalar dileriz.<br>
                                <strong style=""color: #0f172a; font-size: 15px;"">{activeDepartment}</strong>
                            </p>
                        </td>
                    </tr>

                    <!-- 4. FOOTER / ALT BİLGİ -->
                    <tr>
                        <td style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 22px 28px; text-align: center;"">
                            <p style=""margin: 0 0 6px 0; font-size: 11px; color: #94a3b8;"">
                                Bu e-posta Düzce Üniversitesi Araç Takip ve Görevlendirme Sistemi tarafından otomatik olarak üretilmiştir.
                            </p>
                            <p style=""margin: 0; font-size: 11px; color: #cbd5e1; font-weight: 500;"">
                                &copy; 2026 Düzce Üniversitesi &bull; Bilgi İşlem Daire Başkanlığı &bull; Konuralp Yerleşkesi / DÜZCE
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        return html;
    }

    /// <summary>
    /// Araç talebi reddedildiğinde gönderilen üst düzey kurumsal HTML e-posta şablonu.
    /// </summary>
    public static string BuildRequestRejectedEmailHtml(
        VehicleRequest request, 
        string? departmentTitle = null, 
        string? organizationUnit = null)
    {
        var activeDepartment = !string.IsNullOrWhiteSpace(departmentTitle) ? departmentTitle : DefaultDepartmentTitle;
        var activeUnit = !string.IsNullOrWhiteSpace(organizationUnit) ? organizationUnit : DefaultOrganizationUnit;

        var requesterName = WebUtility.HtmlEncode(request.RequesterFullName ?? "Sayın Yetkili");
        var department = WebUtility.HtmlEncode(request.Department ?? "Belirtilmedi");
        var purpose = WebUtility.HtmlEncode(request.Purpose ?? "Resmi Kurum Görevi");
        var rejectionReason = WebUtility.HtmlEncode(request.RejectionReason ?? "Mevcut araç filosu ve şoför planlaması uygun görülmemiştir.");

        var html = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Araç Talebiniz Hakkında</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9; font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, 'Helvetica Neue', Arial, sans-serif; -webkit-font-smoothing: antialiased; color: #1e293b;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f1f5f9; padding: 32px 10px;"">
        <tr>
            <td align=""center"">
                <!-- ANA KART KONTEYNER -->
                <table role=""presentation"" width=""100%"" style=""max-width: 630px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.1), 0 8px 10px -6px rgba(15, 23, 42, 0.05); border: 1px solid #e2e8f0;"" cellspacing=""0"" cellpadding=""0"">
                    
                    <!-- 1. KURUMSAL HEADER -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #091e3a 0%, #1e3a8a 100%); padding: 36px 28px 30px 28px; text-align: center; border-bottom: 3px solid #e11d48;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td align=""center"">
                                        <div style=""display: inline-block; background-color: rgba(255, 255, 255, 0.12); border: 1px solid rgba(255, 255, 255, 0.22); border-radius: 50px; padding: 6px 20px; margin-bottom: 14px; backdrop-filter: blur(4px);"">
                                            <span style=""color: #e2e8f0; font-size: 11px; font-weight: 800; letter-spacing: 2px; text-transform: uppercase;"">DÜZCE ÜNİVERSİTESİ</span>
                                        </div>
                                        <h1 style=""margin: 0; color: #ffffff; font-size: 23px; font-weight: 800; letter-spacing: -0.4px; line-height: 1.3;"">Araç Takip ve Görevlendirme Sistemi</h1>
                                        <p style=""margin: 8px 0 0 0; color: #93c5fd; font-size: 13px; font-weight: 500;"">{activeUnit}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- 2. DURUM HERO ŞERİDİ (REDDEDİLDİ) -->
                    <tr>
                        <td style=""background: linear-gradient(180deg, #fff1f2 0%, #ffe4e6 100%); border-bottom: 1px solid #fecdd3; padding: 18px 28px; text-align: center;"">
                            <div style=""display: inline-block; background-color: #e11d48; color: #ffffff; font-size: 10px; font-weight: 900; padding: 4px 12px; border-radius: 20px; letter-spacing: 1px; text-transform: uppercase; margin-bottom: 6px;"">
                                &#10008; TALEP BİLGİLENDİRMESİ
                            </div>
                            <div style=""color: #9f1239; font-size: 17px; font-weight: 800; letter-spacing: -0.2px;"">
                                Araç Talebiniz Onaylanamadı (#TALEP-{request.Id:D4})
                            </div>
                        </td>
                    </tr>

                    <!-- 3. GÖVDE İÇERİĞİ -->
                    <tr>
                        <td style=""padding: 32px 30px;"">
                            
                            <p style=""margin: 0 0 10px 0; font-size: 16px; color: #0f172a; line-height: 1.5;"">
                                Sayın <strong>{requesterName}</strong>,
                            </p>
                            <p style=""margin: 0 0 20px 0; font-size: 14px; color: #475569; line-height: 1.6;"">
                                <strong>#TALEP-{request.Id:D4}</strong> numaralı resmi araç talebiniz <strong>{activeDepartment}</strong> tarafından incelenmiş olup, mevcut filo yoğunluğu ve şoför görev planlaması doğrultusunda maalesef <strong>onaylanamamıştır</strong>.
                            </p>

                            <!-- RET GEREKÇESİ VURGULU KART -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #fff1f2; border: 1px solid #fecdd3; border-left: 4px solid #e11d48; border-radius: 10px; padding: 16px 20px; margin-bottom: 24px;"">
                                <tr>
                                    <td>
                                        <div style=""font-size: 11px; font-weight: 800; color: #9f1239; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 6px;"">
                                            RET GEREKÇESİ / ŞEF AÇIKLAMASI:
                                        </div>
                                        <div style=""font-size: 14px; color: #881337; font-weight: 700; line-height: 1.6;"">
                                            {rejectionReason}
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- TALEP ÖZETİ TABLOSU -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; margin-bottom: 24px;"">
                                <tr>
                                    <td style=""padding: 12px 18px; border-bottom: 1px solid #edf2f7; width: 34%; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Talep Eden Birim
                                    </td>
                                    <td style=""padding: 12px 18px; border-bottom: 1px solid #edf2f7; font-size: 13px; color: #0f172a; font-weight: 700;"">
                                        {department}
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 18px; font-size: 13px; color: #64748b; font-weight: 600;"">
                                        Talep Amacı
                                    </td>
                                    <td style=""padding: 12px 18px; font-size: 13px; color: #334155; font-style: italic;"">
                                        &ldquo;{purpose}&rdquo;
                                    </td>
                                </tr>
                            </table>

                            <!-- YÖNLENDİRME BİLGİSİ -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 14px 18px; margin-bottom: 24px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0; font-size: 13px; color: #475569; line-height: 1.5;"">
                                            İhtiyaç durumunda farklı bir tarih veya saat aralığı belirleyerek sistem üzerinden yeniden resmi araç talebinde bulunabilir ya da birim şefliği ile görüşebilirsiniz.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0; font-size: 13px; color: #64748b; line-height: 1.6;"">
                                İyi çalışmalar dileriz.<br>
                                <strong style=""color: #0f172a; font-size: 15px;"">{activeDepartment}</strong>
                            </p>
                        </td>
                    </tr>

                    <!-- 4. FOOTER -->
                    <tr>
                        <td style=""background-color: #f8fafc; border-top: 1px solid #e2e8f0; padding: 22px 28px; text-align: center;"">
                            <p style=""margin: 0 0 6px 0; font-size: 11px; color: #94a3b8;"">
                                Bu e-posta Düzce Üniversitesi Araç Takip ve Görevlendirme Sistemi tarafından otomatik olarak üretilmiştir.
                            </p>
                            <p style=""margin: 0; font-size: 11px; color: #cbd5e1; font-weight: 500;"">
                                &copy; 2026 Düzce Üniversitesi &bull; Bilgi İşlem Daire Başkanlığı &bull; Konuralp Yerleşkesi / DÜZCE
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        return html;
    }
}
