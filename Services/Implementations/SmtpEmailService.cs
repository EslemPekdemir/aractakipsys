// File: Services/Implementations/SmtpEmailService.cs
using System.Net;
using System.Net.Mail;
using aractakipsys.Models;
using aractakipsys.Models.Entities;
using aractakipsys.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace aractakipsys.Services.Implementations;

/// <summary>
/// Production ortamında appsettings.json içerisindeki EmailSettings ayarlarını kullanarak gerçek SMTP ile e-posta gönderen servis.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> emailSettings, ILogger<SmtpEmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Alıcı e-posta adresi boş olduğu için e-posta gönderimi atlandı. Konu: {Subject}", subject);
            return;
        }

        // Host veya ayarlar boş ise uyarı ver ve sistemi kesintiye uğratma
        if (string.IsNullOrWhiteSpace(_emailSettings.Host))
        {
            _logger.LogWarning(
                "SMTP Host bilgisi henüz yapılandırılmadığı için e-posta gönderimi atlandı (Placeholder durumunda). Alıcı: {ToEmail}, Konu: {Subject}",
                toEmail, subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
            {
                EnableSsl = _emailSettings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_emailSettings.UserName) && !string.IsNullOrWhiteSpace(_emailSettings.Password))
            {
                client.Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password);
            }

            var senderEmail = !string.IsNullOrWhiteSpace(_emailSettings.SenderEmail) 
                ? _emailSettings.SenderEmail 
                : "aractakip@duzce.edu.tr";

            var senderName = !string.IsNullOrWhiteSpace(_emailSettings.SenderName)
                ? _emailSettings.SenderName
                : "Düzce Üniversitesi Araç Takip Sistemi";

            var utf8 = System.Text.Encoding.UTF8;

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName, utf8),
                Subject = subject,
                SubjectEncoding = utf8,
                Body = body,
                BodyEncoding = utf8,
                HeadersEncoding = utf8,
                IsBodyHtml = isHtml
            };

            if (isHtml)
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(
                    body,
                    new System.Net.Mime.ContentType("text/html; charset=utf-8"));
                mailMessage.AlternateViews.Add(htmlView);
            }

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("SMTP üzerinden e-posta başarıyla iletildi: {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP üzerinden e-posta gönderilirken hata oluştu. Alıcı: {ToEmail}", toEmail);
        }
    }

    public Task SendRequestFulfilledEmailAsync(
        VehicleRequest request, 
        Assignment assignment, 
        bool includeDriverDetails = true, 
        string? departmentTitle = null,
        string? chiefNote = null)
    {
        var activeDepartment = !string.IsNullOrWhiteSpace(departmentTitle) 
            ? departmentTitle 
            : _emailSettings.DepartmentTitle;

        var subject = $"Araç Talebiniz Onaylandı (#TALEP-{request.Id:D4}) — Düzce Üniversitesi";
        var htmlBody = aractakipsys.Common.Helpers.EmailTemplateBuilder.BuildRequestFulfilledEmailHtml(
            request, 
            assignment, 
            activeDepartment, 
            _emailSettings.OrganizationUnit,
            includeDriverDetails,
            chiefNote);

        return SendEmailAsync(request.RequesterEmail, subject, htmlBody, isHtml: true);
    }

    public Task SendRequestRejectedEmailAsync(VehicleRequest request)
    {
        var subject = $"Araç Talebiniz Hakkında (#TALEP-{request.Id:D4}) — Düzce Üniversitesi";
        var htmlBody = aractakipsys.Common.Helpers.EmailTemplateBuilder.BuildRequestRejectedEmailHtml(
            request, 
            _emailSettings.DepartmentTitle, 
            _emailSettings.OrganizationUnit);

        return SendEmailAsync(request.RequesterEmail, subject, htmlBody, isHtml: true);
    }
}
