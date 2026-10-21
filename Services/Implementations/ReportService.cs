// File: Services/Implementations/ReportService.cs
using aractakipsys.Data;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Report;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportFilterViewModel> PrepareFilterOptionsAsync(ReportFilterViewModel? filter = null)
    {
        filter ??= new ReportFilterViewModel();

        // Araçlar Listesi
        filter.VehicleList = await _context.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.PlateNumber)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.PlateNumber} - {v.Brand} {v.Model}"
            })
            .ToListAsync();

        // Şoförler Listesi
        filter.DriverList = await _context.Drivers
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.IsActive)
            .Include(d => d.User)
            .OrderBy(d => d.User.FullName)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = $"{d.User.FullName} ({d.Department})"
            })
            .ToListAsync();

        return filter;
    }

    public async Task<UsageReportViewModel> GetUsageReportAsync(ReportFilterViewModel filter)
    {
        // Filtre dropdownlarını hazırla
        await PrepareFilterOptionsAsync(filter);

        // İş Kuralı: Yalnızca Completed (Tamamlanmış) seferler rapora dahil edilir
        var query = _context.Assignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Include(a => a.Driver)
                .ThenInclude(d => d.User)
            .Include(a => a.AssignmentDrivers)
                .ThenInclude(ad => ad.Driver)
                    .ThenInclude(d => d.User)
            .Where(a => !a.IsDeleted && a.Status == AssignmentStatus.Completed);

        // Araç Filtresi
        if (filter.VehicleId.HasValue && filter.VehicleId.Value > 0)
        {
            query = query.Where(a => a.VehicleId == filter.VehicleId.Value);
        }

        // Şoför Filtresi (Faz 2-A: Çoklu şoför ve AssignmentDrivers desteği)
        if (filter.DriverId.HasValue && filter.DriverId.Value > 0)
        {
            query = query.Where(a => a.DriverId == filter.DriverId.Value ||
                                     a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == filter.DriverId.Value));
        }

        // Tarih Filtresi (Fiili başlangıç zamanı, yoksa planlanan başlangıç üzerinden)
        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.Date;
            query = query.Where(a => (a.ActualStartTime ?? a.PlannedStartTime) >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => (a.ActualStartTime ?? a.PlannedStartTime) <= end);
        }

        // En son gerçekleşen seferler en üstte
        query = query.OrderByDescending(a => a.ActualStartTime ?? a.PlannedStartTime);

        var assignments = await query.ToListAsync();

        var reportItems = assignments.Select(a =>
        {
            string driverName;
            string driverDept;

            if (a.IsDriverless)
            {
                driverName = "Şoförsüz Görevlendirme";
                driverDept = "-";
            }
            else if (a.IsExternalDriver)
            {
                driverName = a.ExternalDriverFullName ?? "Dış Şoför";
                driverDept = a.ExternalDriverDepartment ?? "Dış Görevli";
            }
            else
            {
                var assigned = a.AssignmentDrivers?
                    .Where(ad => !ad.IsDeleted)
                    .OrderByDescending(ad => ad.IsPrimary)
                    .Select(ad => ad.Driver?.User?.FullName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();

                if (assigned != null && assigned.Count > 0)
                {
                    driverName = string.Join(", ", assigned);
                    driverDept = a.AssignmentDrivers.FirstOrDefault(ad => !ad.IsDeleted && ad.IsPrimary)?.Driver?.Department ?? a.Driver?.Department ?? "-";
                }
                else
                {
                    driverName = a.Driver?.User?.FullName ?? "-";
                    driverDept = a.Driver?.Department ?? "-";
                }
            }

            return new UsageReportItemViewModel
            {
                AssignmentId = a.Id,
                PlateNumber = a.Vehicle?.PlateNumber ?? "-",
                VehicleBrandModel = $"{a.Vehicle?.Brand} {a.Vehicle?.Model}".Trim(),
                DriverFullName = driverName,
                DriverDepartment = driverDept,
                Purpose = a.Purpose,
                RequestedByDepartment = a.RequestedByDepartment,
                PlannedStartTime = a.PlannedStartTime,
                PlannedEndTime = a.PlannedEndTime,
                ActualStartTime = a.ActualStartTime,
                ActualEndTime = a.ActualEndTime,
                StartKm = a.StartKm,
                EndKm = a.EndKm
            };
        }).ToList();

        // Rapor özeti ve başlıkları
        var report = new UsageReportViewModel
        {
            Filter = filter,
            Items = reportItems,
            SelectedVehicleName = filter.VehicleId.HasValue
                ? filter.VehicleList.FirstOrDefault(v => v.Value == filter.VehicleId.Value.ToString())?.Text
                : "Tüm Araçlar",
            SelectedDriverName = filter.DriverId.HasValue
                ? filter.DriverList.FirstOrDefault(d => d.Value == filter.DriverId.Value.ToString())?.Text
                : "Tüm Şoförler"
        };

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            report.DateRangeText = $"{filter.StartDate.Value:dd.MM.yyyy} - {filter.EndDate.Value:dd.MM.yyyy}";
        }
        else if (filter.StartDate.HasValue)
        {
            report.DateRangeText = $"{filter.StartDate.Value:dd.MM.yyyy}'den İtibaren";
        }
        else if (filter.EndDate.HasValue)
        {
            report.DateRangeText = $"{filter.EndDate.Value:dd.MM.yyyy}'e Kadar";
        }
        else
        {
            report.DateRangeText = "Tüm Tarihler";
        }

        return report;
    }

    public async Task<MaintenanceReportFilterViewModel> PrepareMaintenanceFilterOptionsAsync(MaintenanceReportFilterViewModel? filter = null)
    {
        filter ??= new MaintenanceReportFilterViewModel();

        // Araçlar Listesi
        filter.VehicleList = await _context.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.PlateNumber)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.PlateNumber} - {v.Brand} {v.Model}"
            })
            .ToListAsync();

        // Veritabanındaki mevcut bakım türlerini çek
        var existingTypes = await _context.Maintenances
            .AsNoTracking()
            .Where(m => !m.IsDeleted && !string.IsNullOrWhiteSpace(m.MaintenanceType))
            .Select(m => m.MaintenanceType.Trim())
            .Distinct()
            .ToListAsync();

        // Standart ön tanımlı türler
        var defaultTypes = new List<string>
        {
            "Periyodik Bakım",
            "Mekanik Onarım",
            "Kaza / Kaporta Onarımı",
            "Lastik Değişimi / Balans",
            "Elektrik / Elektronik",
            "Fren Sistemi",
            "Muayene Hazırlık"
        };

        var allTypes = defaultTypes.Union(existingTypes).OrderBy(x => x).ToList();

        filter.MaintenanceTypeList = allTypes.Select(t => new SelectListItem
        {
            Value = t,
            Text = t
        }).ToList();

        return filter;
    }

    public async Task<MaintenanceReportViewModel> GetMaintenanceReportAsync(MaintenanceReportFilterViewModel filter)
    {
        await PrepareMaintenanceFilterOptionsAsync(filter);

        var query = _context.Maintenances
            .AsNoTracking()
            .Include(m => m.Vehicle)
            .Where(m => !m.IsDeleted);

        // Araç Filtresi
        if (filter.VehicleId.HasValue && filter.VehicleId.Value > 0)
        {
            query = query.Where(m => m.VehicleId == filter.VehicleId.Value);
        }

        // Bakım Türü Filtresi
        if (!string.IsNullOrWhiteSpace(filter.MaintenanceType))
        {
            query = query.Where(m => m.MaintenanceType == filter.MaintenanceType);
        }

        // Devam Eden / Tamamlanan Filtresi
        if (filter.IsOngoing.HasValue)
        {
            if (filter.IsOngoing.Value)
            {
                query = query.Where(m => m.EndDate == null);
            }
            else
            {
                query = query.Where(m => m.EndDate != null);
            }
        }

        // Tarih ve Saat Aralığı Filtresi
        if (filter.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(m => m.StartDate >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(m => m.StartDate <= endUtc);
        }

        var list = await query
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();

        var items = list.Select(m => new MaintenanceReportItemViewModel
        {
            Id = m.Id,
            VehicleId = m.VehicleId,
            PlateNumber = m.Vehicle.PlateNumber,
            VehicleBrandModel = $"{m.Vehicle.Brand} {m.Vehicle.Model}",
            MaintenanceType = m.MaintenanceType,
            Description = m.Description,
            StartDate = m.StartDate.ToLocalTime(),
            EndDate = m.EndDate?.ToLocalTime(),
            PerformedBy = m.PerformedBy
        }).ToList();

        var report = new MaintenanceReportViewModel
        {
            Filter = filter,
            Items = items,
            SelectedVehicleName = filter.VehicleId.HasValue
                ? filter.VehicleList.FirstOrDefault(v => v.Value == filter.VehicleId.Value.ToString())?.Text
                : "Tüm Araçlar"
        };

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            report.DateRangeText = $"{filter.StartDate.Value:dd.MM.yyyy HH:mm} - {filter.EndDate.Value:dd.MM.yyyy HH:mm}";
        }
        else if (filter.StartDate.HasValue)
        {
            report.DateRangeText = $"{filter.StartDate.Value:dd.MM.yyyy HH:mm}'den İtibaren";
        }
        else if (filter.EndDate.HasValue)
        {
            report.DateRangeText = $"{filter.EndDate.Value:dd.MM.yyyy HH:mm}'e Kadar";
        }
        else
        {
            report.DateRangeText = "Tüm Tarihler";
        }

        return report;
    }

    public async Task<InsuranceInspectionReportViewModel> GetInsuranceInspectionReportAsync(InsuranceInspectionReportFilterViewModel filter, bool paged = true)
    {
        var today = DateTime.Today;
        var threshold = today.AddDays(30);

        var query = _context.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted);

        // Plaka / Marka / Model arama filtresi
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(v => v.PlateNumber.ToLower().Contains(term) ||
                                     v.Brand.ToLower().Contains(term) ||
                                     v.Model.ToLower().Contains(term));
        }

        // Eğer kullanıcı tarih aralığı girmişse otomatik CustomDate filtre kuralını uygula
        if (filter.FilterOption == DocumentExpiryFilterOption.All && (filter.StartDate.HasValue || filter.EndDate.HasValue))
        {
            filter.FilterOption = DocumentExpiryFilterOption.CustomDate;
        }

        // Seçilen Filtre Kuralı: Tümü / Sadece Süresi Geçenler / Sadece 30 Gün İçinde Dolacaklar / Özel Tarih
        switch (filter.FilterOption)
        {
            case DocumentExpiryFilterOption.Expired:
                if (filter.Scope == ReportScopeType.Insurance)
                {
                    query = query.Where(v => v.InsuranceExpiryDate.Date < today);
                }
                else if (filter.Scope == ReportScopeType.Inspection)
                {
                    query = query.Where(v => v.InspectionExpiryDate.Date < today);
                }
                else
                {
                    query = query.Where(v => v.Status == VehicleStatus.DocumentExpired ||
                                             v.InsuranceExpiryDate.Date < today ||
                                             v.InspectionExpiryDate.Date < today);
                }
                break;

            case DocumentExpiryFilterOption.ExpiringSoon:
                if (filter.Scope == ReportScopeType.Insurance)
                {
                    query = query.Where(v => v.InsuranceExpiryDate.Date >= today && v.InsuranceExpiryDate.Date <= threshold);
                }
                else if (filter.Scope == ReportScopeType.Inspection)
                {
                    query = query.Where(v => v.InspectionExpiryDate.Date >= today && v.InspectionExpiryDate.Date <= threshold);
                }
                else
                {
                    query = query.Where(v => v.Status != VehicleStatus.DocumentExpired &&
                                             ((v.InsuranceExpiryDate.Date >= today && v.InsuranceExpiryDate.Date <= threshold) ||
                                              (v.InspectionExpiryDate.Date >= today && v.InspectionExpiryDate.Date <= threshold)));
                }
                break;

            case DocumentExpiryFilterOption.CustomDate:
                var isStartDateTarget = filter.DateTarget == "StartDate";
                var start = filter.StartDate?.Date;
                var end = filter.EndDate?.Date.AddDays(1).AddTicks(-1);

                if (filter.Scope == ReportScopeType.Insurance)
                {
                    if (isStartDateTarget)
                    {
                        if (start.HasValue) query = query.Where(v => v.InsuranceStartDate >= start.Value);
                        if (end.HasValue) query = query.Where(v => v.InsuranceStartDate <= end.Value);
                    }
                    else
                    {
                        if (start.HasValue) query = query.Where(v => v.InsuranceExpiryDate >= start.Value);
                        if (end.HasValue) query = query.Where(v => v.InsuranceExpiryDate <= end.Value);
                    }
                }
                else if (filter.Scope == ReportScopeType.Inspection)
                {
                    if (isStartDateTarget)
                    {
                        if (start.HasValue) query = query.Where(v => v.InspectionStartDate >= start.Value);
                        if (end.HasValue) query = query.Where(v => v.InspectionStartDate <= end.Value);
                    }
                    else
                    {
                        if (start.HasValue) query = query.Where(v => v.InspectionExpiryDate >= start.Value);
                        if (end.HasValue) query = query.Where(v => v.InspectionExpiryDate <= end.Value);
                    }
                }
                else // Scope == All
                {
                    if (isStartDateTarget)
                    {
                        if (start.HasValue && end.HasValue)
                        {
                            query = query.Where(v => (v.InsuranceStartDate >= start.Value && v.InsuranceStartDate <= end.Value) ||
                                                     (v.InspectionStartDate >= start.Value && v.InspectionStartDate <= end.Value));
                        }
                        else if (start.HasValue)
                        {
                            query = query.Where(v => v.InsuranceStartDate >= start.Value || v.InspectionStartDate >= start.Value);
                        }
                        else if (end.HasValue)
                        {
                            query = query.Where(v => v.InsuranceStartDate <= end.Value || v.InspectionStartDate <= end.Value);
                        }
                    }
                    else // ExpiryDate
                    {
                        if (start.HasValue && end.HasValue)
                        {
                            query = query.Where(v => (v.InsuranceExpiryDate >= start.Value && v.InsuranceExpiryDate <= end.Value) ||
                                                     (v.InspectionExpiryDate >= start.Value && v.InspectionExpiryDate <= end.Value));
                        }
                        else if (start.HasValue)
                        {
                            query = query.Where(v => v.InsuranceExpiryDate >= start.Value || v.InspectionExpiryDate >= start.Value);
                        }
                        else if (end.HasValue)
                        {
                            query = query.Where(v => v.InsuranceExpiryDate <= end.Value || v.InspectionExpiryDate <= end.Value);
                        }
                    }
                }
                break;

            case DocumentExpiryFilterOption.All:
            default:
                break;
        }

        var vehicles = await query
            .OrderBy(v => (v.Status == VehicleStatus.DocumentExpired || v.InsuranceExpiryDate.Date < today || v.InspectionExpiryDate.Date < today) ? 0 : 1)
            .ThenBy(v => v.InspectionExpiryDate < v.InsuranceExpiryDate ? v.InspectionExpiryDate : v.InsuranceExpiryDate)
            .ToListAsync();

        var items = vehicles.Select(v =>
        {
            var statusName = v.Status switch
            {
                VehicleStatus.Idle => "Boşta / Müsait",
                VehicleStatus.OnDuty => "Görevde / Seyirde",
                VehicleStatus.Maintenance => "Bakımda / Serviste",
                VehicleStatus.OutOfService => "Hizmet Dışı",
                VehicleStatus.DocumentExpired => "Belgesi Geçersiz",
                _ => v.Status.ToString()
            };

            var badgeClass = v.Status switch
            {
                VehicleStatus.Idle => "badge-success bg-success text-white",
                VehicleStatus.OnDuty => "badge-primary bg-primary text-white",
                VehicleStatus.Maintenance => "badge-warning bg-warning text-dark",
                VehicleStatus.OutOfService => "badge-danger bg-danger text-white",
                VehicleStatus.DocumentExpired => "badge-danger bg-danger text-white",
                _ => "badge-secondary"
            };

            return new InsuranceInspectionReportItemViewModel
            {
                VehicleId = v.Id,
                PlateNumber = v.PlateNumber,
                Brand = v.Brand,
                Model = v.Model,
                Year = v.Year,
                Color = v.Color,
                Status = v.Status,
                StatusDisplayName = statusName,
                StatusBadgeClass = badgeClass,
                InsuranceStartDate = v.InsuranceStartDate,
                InsuranceExpiryDate = v.InsuranceExpiryDate,
                InsuranceDaysRemaining = (int)(v.InsuranceExpiryDate.Date - today).TotalDays,
                InspectionStartDate = v.InspectionStartDate,
                InspectionExpiryDate = v.InspectionExpiryDate,
                InspectionDaysRemaining = (int)(v.InspectionExpiryDate.Date - today).TotalDays
            };
        })
        .OrderBy(v => Math.Min(v.InspectionDaysRemaining, v.InsuranceDaysRemaining))
        .ToList();

        int totalCount = items.Count;
        int totalVehiclesCount = totalCount;
        int expiredCount = filter.Scope switch
        {
            ReportScopeType.Insurance => items.Count(x => x.IsInsuranceExpired),
            ReportScopeType.Inspection => items.Count(x => x.IsInspectionExpired),
            _ => items.Count(x => x.HasAnyExpired)
        };
        int expiringSoonCount = filter.Scope switch
        {
            ReportScopeType.Insurance => items.Count(x => x.IsInsuranceExpiringSoon),
            ReportScopeType.Inspection => items.Count(x => x.IsInspectionExpiringSoon),
            _ => items.Count(x => x.HasAnyExpiringSoon)
        };
        int validCount = totalVehiclesCount - expiredCount - expiringSoonCount;

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var displayItems = paged
            ? items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList()
            : items;

        return new InsuranceInspectionReportViewModel
        {
            Filter = filter,
            Items = displayItems,
            TotalCount = totalCount,
            TotalVehiclesCount = totalVehiclesCount,
            ExpiredCount = expiredCount,
            ExpiringSoonCount = expiringSoonCount,
            ValidCount = validCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
