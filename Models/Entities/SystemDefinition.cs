// File: Models/Entities/SystemDefinition.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.Entities;

/// <summary>
/// Admin ve Daire Başkanı tarafından yönetilen dinamik seçenek listesi elemanı.
/// </summary>
public class SystemDefinition : BaseAuditableEntity
{
    [Required]
    [StringLength(50)]
    public string CategoryKey { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string NormalizedName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sistem varsayılanı olan seçenekler (adı değiştirilemez, ancak pasife alınabilir).
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;
}
