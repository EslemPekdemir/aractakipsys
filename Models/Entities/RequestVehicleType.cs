// File: Models/Entities/RequestVehicleType.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

/// <summary>
/// Araç Talep Formunda istenen araç türü seçimi için admin tarafından yönetilen araç türleri listesi (VehicleType enum'undan bağımsız).
/// </summary>
public class RequestVehicleType : BaseAuditableEntity
{
    [Required(ErrorMessage = "Araç türü adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Araç türü adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
