// File: Services/Implementations/ConsoleEmailService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Services.Interfaces;

using aractakipsys.Models;
using Microsoft.Extensions.Options;

namespace aractakipsys.Services.Implementations;

/// <summary>
/// Development ortamında gerçek mail göndermek yerine konsola/log'a yazan e-posta servisi.
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger, IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var logOutput =
            $"\n================================== [GİDEN E-POSTA SİMÜLASYONU] ==================================\n" +
            $"Şuraya şu mail gidecekti: {toEmail}\n" +
            $"Konu: {subject}\n" +
            $"Format: {(isHtml ? "HTML" : "Düz Metin")}\n" +
            $"İçerik:\n{body}\n" +
            $"=================================================================================================";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(logOutput);
        Console.ResetColor();

        _logger.LogInformation("Şuraya şu mail gidecekti: {ToEmail} | Konu: {Subject}", toEmail, subject);

        return Task.CompletedTask;
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
