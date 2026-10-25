// File: Services/Interfaces/IRoleDelegationService.cs
using aractakipsys.Models.Entities;
using aractakipsys.Models.ViewModels.RoleDelegation;

namespace aractakipsys.Services.Interfaces;

public interface IRoleDelegationService
{
    Task<RoleDelegationListViewModel> GetDelegationListAsync(string currentUserId, bool isElevatedAdmin);
    Task<RoleDelegationCreateModalViewModel> GetCreateModalViewModelAsync(string currentUserId, bool isElevatedAdmin);
    Task<(bool Success, string Message, int? DelegationId, List<string>? ConflictingAssignments)> CreateDelegationAsync(
        RoleDelegationCreateInputModel input, 
        string actorUserId, 
        bool isElevatedAdmin);
    Task<(bool Success, string Message)> RevokeDelegationAsync(
        int delegationId, 
        string actorUserId, 
        string? reason, 
        bool isElevatedAdmin);
    Task<RoleDelegation?> GetActiveDelegationForUserAsync(string userId, DateTime? targetTime = null);
    Task<RoleDelegation?> GetActiveDelegationByDelegatorAsync(string delegatorUserId, DateTime? targetTime = null);
    Task<bool> HasDelegationConflictForDriverAsync(int driverId, DateTime start, DateTime end);
    Task AutoRevokeDelegationsForUserAsync(string userId, string revokedByUserId, string reason);
    Task<byte[]> GenerateDelegationPdfAsync(int delegationId, string actorUserId, bool isElevatedAdmin);
}
