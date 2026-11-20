// File: Models/ViewModels/Report/VehicleRequestReportViewModels.cs
using System;
using System.Collections.Generic;

namespace aractakipsys.Models.ViewModels.Report;

public class VehicleRequestReportFilterViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool HasSearched { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class VehicleRequestReportItemViewModel
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int FulfilledCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
}

public class VehicleRequestReportViewModel
{
    public VehicleRequestReportFilterViewModel Filter { get; set; } = new();
    public List<VehicleRequestReportItemViewModel> Items { get; set; } = new();

    public int GrandTotalRequests { get; set; }
    public int GrandTotalFulfilled { get; set; }
    public int GrandTotalRejected { get; set; }
    public int GrandTotalPending { get; set; }

    public string DateRangeText { get; set; } = "Tüm Zamanlar";

    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
