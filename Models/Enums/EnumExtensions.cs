// File: Models/Enums/EnumExtensions.cs
namespace aractakipsys.Models.Enums;

public static class EnumExtensions
{
    public static string ToTurkishString(this VehicleStatus status) => status switch
    {
        VehicleStatus.Idle => "Boşta / Müsait",
        VehicleStatus.OnDuty => "Görevde",
        VehicleStatus.Maintenance => "Bakımda",
        VehicleStatus.OutOfService => "Hizmet Dışı",
        VehicleStatus.DocumentExpired => "Belgesi Geçersiz (Muayene/Sigorta Süresi Dolmuş)",
        _ => status.ToString()
    };

    public static string ToShortTurkishString(this VehicleStatus status) => status switch
    {
        VehicleStatus.Idle => "Boşta",
        VehicleStatus.OnDuty => "Görevde",
        VehicleStatus.Maintenance => "Bakımda",
        VehicleStatus.OutOfService => "Hizmet Dışı",
        VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
        _ => status.ToString()
    };

    public static string ToTurkishString(this AssignmentStatus status) => status switch
    {
        AssignmentStatus.Planned => "Planlandı",
        AssignmentStatus.InProgress => "Devam Ediyor / Görevde",
        AssignmentStatus.Completed => "Tamamlandı",
        AssignmentStatus.Cancelled => "İptal Edildi",
        _ => status.ToString()
    };

    public static string ToTurkishString(this VehicleRequestStatus status) => status switch
    {
        VehicleRequestStatus.Pending => "Onay Bekliyor",
        VehicleRequestStatus.Fulfilled => "Karşılandı / Onaylandı",
        VehicleRequestStatus.Rejected => "Reddedildi",
        _ => status.ToString()
    };

    public static string ToTurkishString(this AuditAction action) => action switch
    {
        AuditAction.Create => "Oluşturma",
        AuditAction.Update => "Güncelleme",
        AuditAction.Delete => "Silme",
        _ => action.ToString()
    };
}
