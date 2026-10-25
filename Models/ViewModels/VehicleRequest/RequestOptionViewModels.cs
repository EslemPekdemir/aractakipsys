// File: Models/ViewModels/VehicleRequest/RequestOptionViewModels.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Entities;

namespace aractakipsys.Models.ViewModels.VehicleRequest;

public class RequestOptionsPageViewModel
{
    public List<RequestDepartment> Departments { get; set; } = new();
    public List<RequestVehicleType> VehicleTypes { get; set; } = new();
}

public class RequestDepartmentCreateOrEditViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Birim adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Birim adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Birim / Fakülte / Daire Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Görüntüleme Sırası")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;
}

public class RequestVehicleTypeCreateOrEditViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Araç türü adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Araç türü adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Araç Türü Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Görüntüleme Sırası")]
    public int DisplayOrder { get; set; } = 0;

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;
}
