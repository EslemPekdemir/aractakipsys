// File: Models/ViewModels/Vehicle/VehicleDocumentItemViewModel.cs
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.ViewModels.Vehicle;

public class VehicleDocumentItemViewModel
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public VehicleDocumentType DocumentType { get; set; }
    public string DocumentTypeName => DocumentType switch
    {
        VehicleDocumentType.Inspection => "Fenni Muayene",
        VehicleDocumentType.Insurance => "Trafik Sigortası",
        VehicleDocumentType.Kasko => "Kasko",
        _ => DocumentType.ToString()
    };

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Cost { get; set; }
    public string? PolicyOrStationNumber { get; set; }
    public string? CompanyName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsExpired => EndDate.Date < DateTime.Today;
    public int DaysRemaining => (int)(EndDate.Date - DateTime.Today).TotalDays;
    public bool IsExpiringSoon => !IsExpired && DaysRemaining <= 30;
    public bool IsNotStarted => StartDate.Date > DateTime.Today;

    // Aracın o belge türündeki en son (en güncel) kaydı mı?
    public bool IsLatest { get; set; }

    /// <summary>
    /// Ortak durum hesaplama kuralı (Tüm belge türleri için tek merkez - Kopya kod yok).
    /// </summary>
    public VehicleDocumentStatus Status
    {
        get
        {
            // Eğer aynı türde daha yeni bir kayıt varsa, bu belge geçmiş kayıttır (Gri).
            if (!IsLatest) return VehicleDocumentStatus.Historical;

            // En son kayıt ama başlangıç tarihi henüz gelmemişse (Başlamadı/Planlı).
            if (IsNotStarted) return VehicleDocumentStatus.NotStarted;

            // En son kayıt ama bitiş tarihi geçmişse (Kırmızı - Asla yeşil olamaz!).
            if (IsExpired) return VehicleDocumentStatus.Expired;

            // En son kayıt ve bitiş tarihine 30 gün veya daha az kalmışsa (Sarı / Turuncu).
            if (IsExpiringSoon) return VehicleDocumentStatus.ExpiringSoon;

            // Bitiş tarihi bugünden sonra veya bugün olan aktif geçerli belge (Yeşil).
            return VehicleDocumentStatus.Valid;
        }
    }

    public string StatusDisplayName => Status switch
    {
        VehicleDocumentStatus.Valid => "Güncel Geçerli",
        VehicleDocumentStatus.ExpiringSoon => "Süresi Yaklaşıyor",
        VehicleDocumentStatus.Expired => "Süresi Dolmuş",
        VehicleDocumentStatus.Historical => "Geçmiş Kayıt",
        VehicleDocumentStatus.NotStarted => "Başlamadı / Planlı",
        _ => Status.ToString()
    };

    public string StatusBadgeClass => Status switch
    {
        VehicleDocumentStatus.Valid => "badge-success",
        VehicleDocumentStatus.ExpiringSoon => "badge-warning text-dark",
        VehicleDocumentStatus.Expired => "badge-danger text-white",
        VehicleDocumentStatus.Historical => "badge-light border text-muted",
        VehicleDocumentStatus.NotStarted => "badge-info text-white",
        _ => "badge-secondary"
    };

    // Geriye dönük uyumluluk: SADECE gerçekten süresi devam eden aktif belgelere true döner.
    // Süresi dolmuş hiçbir belgeye ASLA true dönmez!
    public bool IsCurrentValid => Status == VehicleDocumentStatus.Valid || Status == VehicleDocumentStatus.ExpiringSoon;
}
