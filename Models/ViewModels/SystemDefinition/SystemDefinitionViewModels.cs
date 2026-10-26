// File: Models/ViewModels/SystemDefinition/SystemDefinitionViewModels.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Common;

namespace aractakipsys.Models.ViewModels.SystemDefinition;

public class SystemDefinitionItemViewModel
{
    public int Id { get; set; }
    public string CategoryKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}

public class SystemDefinitionPageViewModel
{
    public SystemDefinitionCategories.CategoryMeta SelectedCategory { get; set; } = null!;
    public IReadOnlyList<SystemDefinitionCategories.CategoryMeta> AllCategories { get; set; } = new List<SystemDefinitionCategories.CategoryMeta>();
    public List<SystemDefinitionItemViewModel> Items { get; set; } = new();
    public bool CanManage { get; set; }
    public bool IsReadOnly { get; set; }

    public int ActiveCount => Items.Count(x => x.IsActive);
    public int TotalCount => Items.Count;
}

public class SystemDefinitionCreateViewModel
{
    [Required(ErrorMessage = "Kategori zorunludur.")]
    public string CategoryKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seçenek adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Seçenek adı en fazla 150 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 9999, ErrorMessage = "Sıra numarası 0 ile 9999 arasında olmalıdır.")]
    public int DisplayOrder { get; set; } = 0;
}

public class SystemDefinitionEditNameViewModel
{
    [Required]
    public int Id { get; set; }

    public string? CategoryKey { get; set; }

    [Required(ErrorMessage = "Seçenek adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Seçenek adı en fazla 150 karakter olabilir.")]
    public string NewName { get; set; } = string.Empty;
}
