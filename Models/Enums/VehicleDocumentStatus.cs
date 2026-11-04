// File: Models/Enums/VehicleDocumentStatus.cs
namespace aractakipsys.Models.Enums;

/// <summary>
/// Araç belgelerinin (Muayene, Trafik Sigortası, Kasko) tek merkezden hesaplanan durum sınıflandırması.
/// </summary>
public enum VehicleDocumentStatus
{
    /// <summary>
    /// Bitiş tarihi bugünden sonra veya bugün olan aktif geçerli belge (Yeşil).
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Bitiş tarihine 30 gün veya daha az kalmış belge (Sarı / Turuncu).
    /// </summary>
    ExpiringSoon = 1,

    /// <summary>
    /// En son kayıt ancak bitiş tarihi geçmiş (Kırmızı). Asla yeşil gösterilemez.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Aynı türde daha yeni bir kayıt varken arşiv/eski kalan belge (Gri).
    /// </summary>
    Historical = 3,

    /// <summary>
    /// Başlangıç tarihi henüz gelmemiş ileri tarihli belge (Planlı / Başlamadı).
    /// </summary>
    NotStarted = 4
}

