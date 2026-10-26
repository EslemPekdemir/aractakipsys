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

        var reportItems = new List<UsageReportItemViewModel>();

        foreach (var a in assignments)
        {
            var tripDurationMinutes = (int)Math.Round(((a.ActualEndTime ?? a.PlannedEndTime) - (a.ActualStartTime ?? a.PlannedStartTime)).TotalMinutes);
            if (tripDurationMinutes < 0) tripDurationMinutes = 0;

            if (a.IsDriverless)
            {
                reportItems.Add(new UsageReportItemViewModel
                {
                    AssignmentId = a.Id,
                    PlateNumber = a.Vehicle?.PlateNumber ?? "-",
                    VehicleBrandModel = $"{a.Vehicle?.Brand} {a.Vehicle?.Model}".Trim(),
                    DriverFullName = "Şoförsüz Görevlendirme",
                    DriverDepartment = "-",
                    DriverId = null,
                    IsPrimaryDriver = true,
                    Purpose = a.Purpose,
                    RequestedByDepartment = a.RequestedByDepartment,
                    PlannedStartTime = a.PlannedStartTime,
                    PlannedEndTime = a.PlannedEndTime,
                    ActualStartTime = a.ActualStartTime,
                    ActualEndTime = a.ActualEndTime,
                    StartKm = a.StartKm,
                    EndKm = a.EndKm
                });
            }
            else if (a.IsExternalDriver)
            {
                reportItems.Add(new UsageReportItemViewModel
                {
                    AssignmentId = a.Id,
                    PlateNumber = a.Vehicle?.PlateNumber ?? "-",
                    VehicleBrandModel = $"{a.Vehicle?.Brand} {a.Vehicle?.Model}".Trim(),
                    DriverFullName = a.ExternalDriverFullName ?? "Dış Şoför",
                    DriverDepartment = a.ExternalDriverDepartment ?? "Dış Görevli",
                    DriverId = null,
                    IsPrimaryDriver = true,
                    Purpose = a.Purpose,
                    RequestedByDepartment = a.RequestedByDepartment,
                    PlannedStartTime = a.PlannedStartTime,
                    PlannedEndTime = a.PlannedEndTime,
                    ActualStartTime = a.ActualStartTime,
                    ActualEndTime = a.ActualEndTime,
                    StartKm = a.StartKm,
                    EndKm = a.EndKm
                });
            }
            else
            {
                var assignedDrivers = a.AssignmentDrivers?
                    .Where(ad => !ad.IsDeleted && ad.Driver != null)
                    .OrderByDescending(ad => ad.IsPrimary)
                    .ToList();

                if (assignedDrivers != null && assignedDrivers.Count > 0)
                {
                    // Çoklu şoförlü görevde her şoför için grup kaydı oluşturulur
                    foreach (var ad in assignedDrivers)
                    {
                        reportItems.Add(new UsageReportItemViewModel
                        {
                            AssignmentId = a.Id,
                            PlateNumber = a.Vehicle?.PlateNumber ?? "-",
                            VehicleBrandModel = $"{a.Vehicle?.Brand} {a.Vehicle?.Model}".Trim(),
                            DriverFullName = ad.Driver?.User?.FullName ?? "Belirtilmemiş",
                            DriverDepartment = ad.Driver?.Department ?? "-",
                            DriverId = ad.DriverId,
                            IsPrimaryDriver = ad.IsPrimary,
                            Purpose = a.Purpose,
                            RequestedByDepartment = a.RequestedByDepartment,
                            PlannedStartTime = a.PlannedStartTime,
                            PlannedEndTime = a.PlannedEndTime,
                            ActualStartTime = a.ActualStartTime,
                            ActualEndTime = a.ActualEndTime,
                            StartKm = a.StartKm,
                            EndKm = a.EndKm
                        });
                    }
                }
                else
                {
                    reportItems.Add(new UsageReportItemViewModel
                    {
                        AssignmentId = a.Id,
                        PlateNumber = a.Vehicle?.PlateNumber ?? "-",
                        VehicleBrandModel = $"{a.Vehicle?.Brand} {a.Vehicle?.Model}".Trim(),
                        DriverFullName = a.Driver?.User?.FullName ?? "Belirtilmemiş",
                        DriverDepartment = a.Driver?.Department ?? "-",
                        DriverId = a.DriverId,
                        IsPrimaryDriver = true,
                        Purpose = a.Purpose,
                        RequestedByDepartment = a.RequestedByDepartment,
                        PlannedStartTime = a.PlannedStartTime,
                        PlannedEndTime = a.PlannedEndTime,
                        ActualStartTime = a.ActualStartTime,
                        ActualEndTime = a.ActualEndTime,
                        StartKm = a.StartKm,
                        EndKm = a.EndKm
                    });
                }
            }
        }

        // Şoför bazında grupla
        var driverGroups = reportItems
            .GroupBy(x => new { x.DriverFullName, x.DriverDepartment })
            .OrderBy(g => g.Key.DriverFullName)
            .Select(g => new UsageReportDriverGroupViewModel
            {
                DriverFullName = g.Key.DriverFullName,
                DriverDepartment = g.Key.DriverDepartment,
                DriverId = g.FirstOrDefault(x => x.DriverId.HasValue)?.DriverId,
                Items = g.OrderByDescending(x => x.TripDate).ToList()
            })
            .ToList();

        // Rapor özeti ve başlıkları
        var report = new UsageReportViewModel
        {
            Filter = filter,
            Items = reportItems,
            DriverGroups = driverGroups,
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
            PerformedBy = m.PerformedBy,
            Cost = m.Cost
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

        // Araç listesini filtre dropdown'ı için hazırla
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

        var query = _context.Vehicles
            .AsNoTracking()
            .Where(v => !v.IsDeleted);

        // Tek Araç Filtresi
        if (filter.VehicleId.HasValue && filter.VehicleId.Value > 0)
        {
            query = query.Where(v => v.Id == filter.VehicleId.Value);
        }

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
                else if (filter.Scope == ReportScopeType.Kasko)
                {
                    query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate.Date < today));
                }
                else
                {
                    query = query.Where(v => v.Status == VehicleStatus.DocumentExpired ||
                                             v.InsuranceExpiryDate.Date < today ||
                                             v.InspectionExpiryDate.Date < today ||
                                             v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate.Date < today));
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
                else if (filter.Scope == ReportScopeType.Kasko)
                {
                    query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate.Date >= today && d.EndDate.Date <= threshold));
                }
                else
                {
                    query = query.Where(v => v.Status != VehicleStatus.DocumentExpired &&
                                             ((v.InsuranceExpiryDate.Date >= today && v.InsuranceExpiryDate.Date <= threshold) ||
                                              (v.InspectionExpiryDate.Date >= today && v.InspectionExpiryDate.Date <= threshold) ||
                                              v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate.Date >= today && d.EndDate.Date <= threshold)));
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
                else if (filter.Scope == ReportScopeType.Kasko)
                {
                    if (isStartDateTarget)
                    {
                        if (start.HasValue && end.HasValue) query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.StartDate >= start.Value && d.StartDate <= end.Value));
                        else if (start.HasValue) query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.StartDate >= start.Value));
                        else if (end.HasValue) query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.StartDate <= end.Value));
                    }
                    else
                    {
                        if (start.HasValue && end.HasValue) query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate >= start.Value && d.EndDate <= end.Value));
                        else if (start.HasValue) query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate >= start.Value));
                        else if (end.HasValue) query = query.Where(v => v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate <= end.Value));
                    }
                }
                else // Scope == All
                {
                    if (isStartDateTarget)
                    {
                        if (start.HasValue && end.HasValue)
                        {
                            query = query.Where(v => (v.InsuranceStartDate >= start.Value && v.InsuranceStartDate <= end.Value) ||
                                                     (v.InspectionStartDate >= start.Value && v.InspectionStartDate <= end.Value) ||
                                                     v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.StartDate >= start.Value && d.StartDate <= end.Value));
                        }
                        else if (start.HasValue)
                        {
                            query = query.Where(v => v.InsuranceStartDate >= start.Value || v.InspectionStartDate >= start.Value ||
                                                     v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.StartDate >= start.Value));
                        }
                        else if (end.HasValue)
                        {
                            query = query.Where(v => v.InsuranceStartDate <= end.Value || v.InspectionStartDate <= end.Value ||
                                                     v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.StartDate <= end.Value));
                        }
                    }
                    else // ExpiryDate
                    {
                        if (start.HasValue && end.HasValue)
                        {
                            query = query.Where(v => (v.InsuranceExpiryDate >= start.Value && v.InsuranceExpiryDate <= end.Value) ||
                                                     (v.InspectionExpiryDate >= start.Value && v.InspectionExpiryDate <= end.Value) ||
                                                     v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate >= start.Value && d.EndDate <= end.Value));
                        }
                        else if (start.HasValue)
                        {
                            query = query.Where(v => v.InsuranceExpiryDate >= start.Value || v.InspectionExpiryDate >= start.Value ||
                                                     v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate >= start.Value));
                        }
                        else if (end.HasValue)
                        {
                            query = query.Where(v => v.InsuranceExpiryDate <= end.Value || v.InspectionExpiryDate <= end.Value ||
                                                     v.Documents.Any(d => !d.IsDeleted && d.DocumentType == VehicleDocumentType.Kasko && d.EndDate <= end.Value));
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

        var vehicleIds = vehicles.Select(v => v.Id).ToList();
        var allDocs = await _context.VehicleDocuments
            .AsNoTracking()
            .Where(d => vehicleIds.Contains(d.VehicleId) && !d.IsDeleted)
            .OrderByDescending(d => d.EndDate)
            .ThenByDescending(d => d.Id)
            .ToListAsync();

        var docsByVehicle = allDocs
            .GroupBy(d => d.VehicleId)
            .ToDictionary(g => g.Key, g => g.ToList());

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

            docsByVehicle.TryGetValue(v.Id, out var vDocs);
            var latestInsuranceDoc = vDocs?.FirstOrDefault(d => d.DocumentType == VehicleDocumentType.Insurance);
            var latestInspectionDoc = vDocs?.FirstOrDefault(d => d.DocumentType == VehicleDocumentType.Inspection);
            var latestKaskoDoc = vDocs?.FirstOrDefault(d => d.DocumentType == VehicleDocumentType.Kasko);

            var insCost = latestInsuranceDoc?.Cost;
            var inspCost = latestInspectionDoc?.Cost;
            var kaskoCost = latestKaskoDoc?.Cost;

            DateTime? kaskoStart = latestKaskoDoc?.StartDate;
            DateTime? kaskoEnd = latestKaskoDoc?.EndDate;
            int? kaskoDaysRemaining = kaskoEnd.HasValue ? (int)(kaskoEnd.Value.Date - today).TotalDays : null;

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
                InsuranceStartDate = latestInsuranceDoc != null ? latestInsuranceDoc.StartDate : v.InsuranceStartDate,
                InsuranceExpiryDate = latestInsuranceDoc != null ? latestInsuranceDoc.EndDate : v.InsuranceExpiryDate,
                InsuranceDaysRemaining = (int)((latestInsuranceDoc != null ? latestInsuranceDoc.EndDate.Date : v.InsuranceExpiryDate.Date) - today).TotalDays,
                InsuranceCost = insCost,
                InsuranceCompanyName = latestInsuranceDoc?.CompanyName,
                InsurancePolicyNumber = latestInsuranceDoc?.PolicyOrStationNumber,
                InspectionStartDate = latestInspectionDoc != null ? latestInspectionDoc.StartDate : v.InspectionStartDate,
                InspectionExpiryDate = latestInspectionDoc != null ? latestInspectionDoc.EndDate : v.InspectionExpiryDate,
                InspectionDaysRemaining = (int)((latestInspectionDoc != null ? latestInspectionDoc.EndDate.Date : v.InspectionExpiryDate.Date) - today).TotalDays,
                InspectionCost = inspCost,
                InspectionStationName = latestInspectionDoc?.CompanyName,
                InspectionPolicyNumber = latestInspectionDoc?.PolicyOrStationNumber,
                KaskoStartDate = kaskoStart,
                KaskoExpiryDate = kaskoEnd,
                KaskoDaysRemaining = kaskoDaysRemaining,
                KaskoCost = kaskoCost,
                KaskoCompanyName = latestKaskoDoc?.CompanyName,
                KaskoPolicyNumber = latestKaskoDoc?.PolicyOrStationNumber
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
            ReportScopeType.Kasko => items.Count(x => x.IsKaskoExpired),
            _ => items.Count(x => x.HasAnyExpired)
        };
        int expiringSoonCount = filter.Scope switch
        {
            ReportScopeType.Insurance => items.Count(x => x.IsInsuranceExpiringSoon),
            ReportScopeType.Inspection => items.Count(x => x.IsInspectionExpiringSoon),
            ReportScopeType.Kasko => items.Count(x => x.IsKaskoExpiringSoon),
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

    public async Task<VehicleRequestReportViewModel> GetVehicleRequestReportAsync(VehicleRequestReportFilterViewModel filter, bool paged = true)
    {
        var query = _context.VehicleRequests.AsNoTracking().Where(r => !r.IsDeleted);

        // Tarih aralığı filtresi (talep OLUŞTURULMA tarihine göre)
        if (filter.StartDate.HasValue)
        {
            var startUtc = filter.StartDate.Value.Date;
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.CreatedAt <= endUtc);
        }

        var rawList = await query.Select(r => new
        {
            r.Department,
            r.Status
        }).ToListAsync();

        // Birim bazında gruplama:
        // - Boşluk ve büyük/küçük harf duyarsız gruplama
        // - Boş olanlar "Belirtilmemiş" satırında toplanır
        // - Eşleşmeyen serbest metinler olduğu gibi kalır (uydurma düzeltme yok)
        var grouped = rawList
            .GroupBy(r =>
            {
                var dept = r.Department?.Trim();
                if (string.IsNullOrWhiteSpace(dept))
                {
                    return "Belirtilmemiş";
                }
                return dept;
            }, StringComparer.OrdinalIgnoreCase)
            .Select(g => new VehicleRequestReportItemViewModel
            {
                DepartmentName = g.Key,
                TotalRequests = g.Count(),
                FulfilledCount = g.Count(x => x.Status == VehicleRequestStatus.Fulfilled),
                RejectedCount = g.Count(x => x.Status == VehicleRequestStatus.Rejected),
                PendingCount = g.Count(x => x.Status == VehicleRequestStatus.Pending)
            })
            .OrderByDescending(x => x.TotalRequests)
            .ThenBy(x => x.DepartmentName)
            .ToList();

        string dateRangeText = "Tüm Zamanlar";
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            dateRangeText = $"{filter.StartDate.Value:dd.MM.yyyy} - {filter.EndDate.Value:dd.MM.yyyy}";
        }
        else if (filter.StartDate.HasValue)
        {
            dateRangeText = $"{filter.StartDate.Value:dd.MM.yyyy} Tarihinden İtibaren";
        }
        else if (filter.EndDate.HasValue)
        {
            dateRangeText = $"{filter.EndDate.Value:dd.MM.yyyy} Tarihine Kadar";
        }

        var totalCount = grouped.Count;
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 7 : filter.PageSize;

        var displayItems = paged
            ? grouped.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList()
            : grouped;

        return new VehicleRequestReportViewModel
        {
            Filter = filter,
            Items = displayItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            GrandTotalRequests = grouped.Sum(x => x.TotalRequests),
            GrandTotalFulfilled = grouped.Sum(x => x.FulfilledCount),
            GrandTotalRejected = grouped.Sum(x => x.RejectedCount),
            GrandTotalPending = grouped.Sum(x => x.PendingCount),
            DateRangeText = dateRangeText
        };
    }
}
