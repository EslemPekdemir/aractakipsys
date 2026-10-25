// File: Services/Interfaces/IEmailService.cs
using aractakipsys.Models.Entities;

namespace aractakipsys.Services.Interfaces;

/// <summary>
/// E-posta bildirim servisi arayüzü.
/// Development ortamında ConsoleEmailService, Production ortamında SmtpEmailService tarafından uygulanır.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    Task SendRequestFulfilledEmailAsync(
        VehicleRequest request, 
        Assignment assignment, 
        string? departmentTitle = null, 
        string? chiefNote = null,
        string? contactFullName = null,
        string? contactPhone = null,
        string? contactEmail = null);
    Task SendRequestRejectedEmailAsync(VehicleRequest request);
}
