// File: Models/ViewModels/Surucu/EndAssignmentRequest.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Surucu;

public class EndAssignmentRequest
{
    [Required(ErrorMessage = "Bitiş kilometresi zorunludur.")]
    [Range(0, int.MaxValue, ErrorMessage = "Geçerli bir kilometre değeri giriniz.")]
    [Display(Name = "Bitiş KM")]
    public int EndKm { get; set; }

    [StringLength(500, ErrorMessage = "Teslim notu en fazla 500 karakter olabilir.")]
    [Display(Name = "Teslim Notu")]
    public string? DriverReturnNotes { get; set; }

    // Araç Kontrol Tikleri (Faz 3)
    public bool CheckOil { get; set; }
    public bool CheckWater { get; set; }
    public bool CheckFuel { get; set; }
    public bool CheckHydraulic { get; set; }
    public bool CheckTires { get; set; }
}
