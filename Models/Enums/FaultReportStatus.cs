// File: Models/Enums/FaultReportStatus.cs
namespace aractakipsys.Models.Enums;

public enum FaultReportStatus
{
    Pending = 0,    // Beklemede (Şef Onayı Bekliyor)
    Approved = 1,   // İncelendi / Onaylandı
    Resolved = 2,   // Onarıldı / Çözüldü
    Rejected = 3    // İptal Edildi / Reddedildi
}
