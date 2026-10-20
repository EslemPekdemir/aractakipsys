// File: Models/EmailSettings.cs
namespace aractakipsys.Models;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "Düzce Üniversitesi Araç Takip Sistemi";
    public string SenderEmail { get; set; } = "aractakip@duzce.edu.tr";
    public string DepartmentTitle { get; set; } = "Düzce Üniversitesi Ulaşım Şefliği";
    public string OrganizationUnit { get; set; } = "Ulaşım Şefliği • Bilgi İşlem Daire Başkanlığı";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
