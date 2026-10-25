// File: Models/Entities/RequestDepartment.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

/// <summary>
/// Araç Talep Formunda birim seçimi için admin tarafından yönetilen birimler listesi.
/// </summary>
public class RequestDepartment : BaseAuditableEntity
{
    [Required(ErrorMessage = "Birim adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Birim adı en fazla 150 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
