// File: Services/Interfaces/ISystemDefinitionService.cs
using aractakipsys.Common;
using aractakipsys.Models.ViewModels.SystemDefinition;

namespace aractakipsys.Services.Interfaces;

public interface ISystemDefinitionService
{
    IReadOnlyList<SystemDefinitionCategories.CategoryMeta> GetCategories();

    Task<SystemDefinitionPageViewModel> GetPageDataAsync(string? categoryKey, bool canManage, bool isReadOnly);

    Task<List<string>> GetActiveOptionNamesAsync(string categoryKey);

    Task<(bool Success, string Message, int? Id)> CreateAsync(string categoryKey, string name, int displayOrder, string? userId);

    Task<(bool Success, string Message)> UpdateNameAsync(int id, string newName, string? userId);

    Task<(bool Success, string Message)> ToggleStatusAsync(int id, string? userId);

    Task<(bool Success, string Message)> DeleteAsync(int id, string? userId);

    Task<int> GetUsageCountAsync(string categoryKey, string name);

    Task<(bool IsValid, string? ErrorMessage)> ValidateOptionAsync(string categoryKey, string? value, string? existingValue = null);
}
