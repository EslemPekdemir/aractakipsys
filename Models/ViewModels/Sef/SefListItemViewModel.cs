// File: Models/ViewModels/Sef/SefListItemViewModel.cs
namespace aractakipsys.Models.ViewModels.Sef;

public class SefListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int ManagedAssignmentsCount { get; set; }
    public string RoleName { get; set; } = "Kullanici";
    public string Role { get => RoleName; set => RoleName = value; }
    public string RoleDisplayName { get; set; } = "Ulaşım Şefi";
}
