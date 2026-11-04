// File: Models/Enums/TireStatus.cs
namespace aractakipsys.Models.Enums;

/// <summary>
/// Araç lastiğinin kullanım ömrü ve güvenlik durumunu belirten 4'lü durum sınıflandırması.
/// </summary>
public enum TireStatus
{
    /// <summary>
    /// Eşik girilmiş, aşınma ve süre problemi yok (Güvenli sınırda).
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Değişim eşiğine yaklaşılmış (30 gün veya KM uyarı eşiği içinde).
    /// </summary>
    Approaching = 1,

    /// <summary>
    /// Değişim eşiği aşılmış (KM veya tavsiye tarihi dolmuş/geçmiş).
    /// </summary>
    Critical = 2,

    /// <summary>
    /// Değişim eşiği (KM ve tarih) girilmemiş VEYA takılma KM'si girilmediği için Kullanım KM hesaplanamıyor.
    /// </summary>
    MissingData = 3
}

