// File: Models/Entities/VehicleRequest.cs
using System.ComponentModel.DataAnnotations;
using aractakipsys.Models.Enums;

namespace aractakipsys.Models.Entities;

public class VehicleRequest : BaseAuditableEntity
{
    [Required(ErrorMessage = "Talep eden ad soyad alanı zorunludur.")]
    [StringLength(150, ErrorMessage = "Ad soyad en fazla 150 karakter olabilir.")]
    public string RequesterFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [StringLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@duzce\.edu\.tr$", ErrorMessage = "E-posta adresi @duzce.edu.tr uzantılı olmalıdır.")]
    public string RequesterEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    public string RequesterPhone { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Birim adı en fazla 150 karakter olabilir.")]
    public string? Department { get; set; }

    [Required(ErrorMessage = "Araç talep gerekçesi zorunludur.")]
    [StringLength(500, ErrorMessage = "Talep gerekçesi en fazla 500 karakter olabilir.")]
    public string Purpose { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Güzergah en fazla 500 karakter olabilir.")]
    public string? Route { get; set; }

    [Required]
    public VehicleRequestType RequestType { get; set; } = VehicleRequestType.OfficialTrip;

    [Required]
    public VehicleRequestStatus Status { get; set; } = VehicleRequestStatus.Pending;

    public int? FulfilledAssignmentId { get; set; }

    [StringLength(450)]
    public string? FulfilledByUserId { get; set; }

    public DateTime? FulfilledAt { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    [StringLength(100, ErrorMessage = "İstenen araç türü en fazla 100 karakter olabilir.")]
    public string? RequestedVehicleType { get; set; }

    public int? PassengerCount { get; set; }

    public DateTime? RequestedStartTime { get; set; }

    public DateTime? RequestedEndTime { get; set; }

    [StringLength(450)]
    public string? ContactUserId { get; set; }

    [StringLength(150)]
    public string? ContactFullName { get; set; }

    [StringLength(30)]
    public string? ContactPhone { get; set; }

    [StringLength(150)]
    public string? ContactEmail { get; set; }

    // Navigation Properties
    public Assignment? FulfilledAssignment { get; set; }
    public ApplicationUser? FulfilledByUser { get; set; }
    public ApplicationUser? ContactUser { get; set; }
}
