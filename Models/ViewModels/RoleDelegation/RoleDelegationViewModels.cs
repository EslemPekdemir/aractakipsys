// File: Models/ViewModels/RoleDelegation/RoleDelegationViewModels.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.RoleDelegation;

public class RoleDelegationListViewModel
{
    public bool IsElevatedAdmin { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentUserFullName { get; set; } = string.Empty;
    public List<RoleDelegationItemViewModel> Delegations { get; set; } = new();
    public RoleDelegationItemViewModel? ActiveDelegatedToMe { get; set; }
    public RoleDelegationItemViewModel? ActiveDelegatedByMe { get; set; }
}

public class RoleDelegationItemViewModel
{
    public int Id { get; set; }
    public string DelegatorUserId { get; set; } = string.Empty;
    public string DelegatorFullName { get; set; } = string.Empty;
    public string DelegatorRole { get; set; } = string.Empty;

    public string DelegateeUserId { get; set; } = string.Empty;
    public string DelegateeFullName { get; set; } = string.Empty;
    public string OriginalDelegateeRole { get; set; } = string.Empty;
    public string DelegatedRole { get; set; } = "Kullanici";

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ActualRevokedAt { get; set; }
    public string? RevokedByFullName { get; set; }
    public string? RevokeReason { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Hesaplanmış durum (DÜZELTME 2: Planlı / Aktif / Bitti / İptal)
    public string StatusText
    {
        get
        {
            if (ActualRevokedAt.HasValue) return "İptal";
            var now = DateTime.UtcNow;
            if (now < StartDate) return "Planlı";
            if (now >= StartDate && now <= EndDate) return "Aktif";
            return "Bitti";
        }
    }

    public string StatusBadgeClass => StatusText switch
    {
        "Aktif" => "badge-primary bg-primary text-white",
        "Planlı" => "badge-info bg-info text-white",
        "Bitti" => "badge-secondary bg-secondary text-white",
        "İptal" => "badge-danger bg-danger text-white",
        _ => "badge-secondary"
    };

    public bool CanBeRevoked
    {
        get
        {
            if (ActualRevokedAt.HasValue) return false;
            return DateTime.UtcNow <= EndDate;
        }
    }
}

public class RoleDelegationCreateModalViewModel
{
    public bool IsElevatedAdmin { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentUserFullName { get; set; } = string.Empty;
    public List<SelectListItem> EligibleDelegators { get; set; } = new();
    public List<SelectListItem> EligibleDelegatees { get; set; } = new();
}

public class RoleDelegationCreateInputModel
{
    // DÜZELTME 4: Şef için formdan alınmaz; Admin/Daire Başkanı seçebilir
    public string? DelegatorUserId { get; set; }

    [Required(ErrorMessage = "Lütfen görevi devralacak personeli seçiniz.")]
    public string DelegateeUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen başlangıç tarih ve saatini giriniz.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Lütfen bitiş tarih ve saatini giriniz.")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Lütfen yetki devri gerekçesini yazınız.")]
    [StringLength(500, ErrorMessage = "Gerekçe en fazla 500 karakter olabilir.")]
    public string Reason { get; set; } = string.Empty;
}

public class RoleDelegationRevokeInputModel
{
    public int DelegationId { get; set; }

    [StringLength(500, ErrorMessage = "İptal gerekçesi en fazla 500 karakter olabilir.")]
    public string? RevokeReason { get; set; }
}
