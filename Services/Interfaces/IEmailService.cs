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
    Task SendRequestFulfilledEmailAsync(VehicleRequest request, Assignment assignment, bool includeDriverDetails = true, string? departmentTitle = null, string? chiefNote = null);
    Task SendRequestRejectedEmailAsync(VehicleRequest request);
}
