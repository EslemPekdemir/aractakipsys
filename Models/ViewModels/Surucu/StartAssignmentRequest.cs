// File: Models/ViewModels/Surucu/StartAssignmentRequest.cs
using System.ComponentModel.DataAnnotations;

namespace aractakipsys.Models.ViewModels.Surucu;

public class StartAssignmentRequest
{
    [Required(ErrorMessage = "Başlangıç kilometresi zorunludur.")]
    [Range(0, int.MaxValue, ErrorMessage = "Geçerli bir kilometre değeri giriniz.")]
    [Display(Name = "Başlangıç KM")]
    public int StartKm { get; set; }
}
