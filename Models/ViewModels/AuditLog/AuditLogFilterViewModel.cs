// File: Models/ViewModels/AuditLog/AuditLogFilterViewModel.cs
using aractakipsys.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace aractakipsys.Models.ViewModels.AuditLog;

public class AuditLogFilterViewModel
{
    public string? EntityName { get; set; }
    public AuditAction? Action { get; set; }
    public string? ChangedByUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public List<AuditLogListItemViewModel> Logs { get; set; } = new();

    // Filtre Dropdown Verileri
    public List<string> EntityNameList { get; set; } = new();
    public List<SelectListItem> UserList { get; set; } = new();
    public Dictionary<string, string> UserMap { get; set; } = new();
}
