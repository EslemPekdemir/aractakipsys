// File: Services/Implementations/VehicleRequestService.cs
using System.Text.RegularExpressions;
using aractakipsys.Data;
using aractakipsys.Models;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using aractakipsys.Models.ViewModels.Assignment;
using aractakipsys.Models.ViewModels.VehicleRequest;
using aractakipsys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace aractakipsys.Services.Implementations;

public class VehicleRequestService : IVehicleRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IAssignmentService _assignmentService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<VehicleRequestService> _logger;

    private static readonly Regex DuzceEmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@duzce\.edu\.tr$", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public VehicleRequestService(
        ApplicationDbContext context,
        IAssignmentService assignmentService,
        IAuditLogService auditLogService,
        IEmailService emailService,
        IOptions<EmailSettings> emailSettings,
        ILogger<VehicleRequestService> logger)
    {
        _context = context;
        _assignmentService = assignmentService;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, int RequestId)> CreateRequestAsync(VehicleRequestCreateViewModel model)
    {
        try
        {
            var email = model.RequesterEmail?.Trim() ?? string.Empty;

            // Server-side regex ve alan kontrolleri
            if (string.IsNullOrWhiteSpace(email) || !DuzceEmailRegex.IsMatch(email))
            {
                return (false, "E-posta adresi yalnızca @duzce.edu.tr uzantılı geçerli bir kurumsal adres olmalıdır.", 0);
            }

            if (string.IsNullOrWhiteSpace(model.RequesterFullName) ||
                string.IsNullOrWhiteSpace(model.RequesterPhone) ||
                string.IsNullOrWhiteSpace(model.Purpose) ||
                string.IsNullOrWhiteSpace(model.Department))
            {
                return (false, "Lütfen tüm zorunlu alanları (Ad Soyad, E-posta, Telefon, Birim ve Amaç) eksiksiz doldurunuz.", 0);
            }

            // Server-side telefon numarası ve uzunluk kontrolü (10 veya 11 haneli cep telefonu)
            var phoneDigits = new string(model.RequesterPhone.Where(char.IsDigit).ToArray());
            if (phoneDigits.StartsWith("90") && phoneDigits.Length == 12)
            {
                phoneDigits = phoneDigits.Substring(2);
            }
            if (phoneDigits.StartsWith("0"))
            {
                phoneDigits = phoneDigits.Substring(1);
            }
            if (phoneDigits.Length != 10 || !phoneDigits.StartsWith("5"))
            {
                return (false, "Lütfen geçerli ve eksiksiz bir cep telefonu numarası giriniz (Örn: 05xx xxx xx xx - 10 veya 11 hane).", 0);
            }

            // Maksimum uzunluk doğrulamaları (Veritabanı kolon sınırlarıyla uyumlu)
            if (model.RequesterFullName.Trim().Length > 150)
            {
                return (false, "Ad Soyad en fazla 150 karakter olabilir.", 0);
            }
            if (email.Length > 150)
            {
                return (false, "E-posta adresi en fazla 150 karakter olabilir.", 0);
            }
            if (model.RequesterPhone.Trim().Length > 20)
            {
                return (false, "Telefon numarası en fazla 20 karakter olabilir.", 0);
            }
            if (model.Department.Trim().Length > 150)
            {
                return (false, "Birim adı en fazla 150 karakter olabilir.", 0);
            }
            if (!string.IsNullOrWhiteSpace(model.RequestedVehicleType) && model.RequestedVehicleType.Trim().Length > 100)
            {
                return (false, "İstenen araç türü en fazla 100 karakter olabilir.", 0);
            }
            if (model.Purpose.Trim().Length > 500)
            {
                return (false, "Talep gerekçesi en fazla 500 karakter olabilir.", 0);
            }

            // Birim doğrulama: RequestDepartments tablosundaki AKTİF kayıtlardan biriyle eşleşmeli (büyük/küçük harf duyarsız, trimli)
            var trimmedDept = model.Department.Trim();
            var activeDept = await _context.RequestDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IsActive && d.Name.ToLower() == trimmedDept.ToLower());

            if (activeDept == null)
            {
                return (false, "Geçersiz birim seçimi. Lütfen listeden aktif bir birim seçiniz.", 0);
            }

            // (a) İstenen araç türü zorunluluk ve aktiflik kontrolü
            if (string.IsNullOrWhiteSpace(model.RequestedVehicleType))
            {
                return (false, "Araç türü seçilmelidir.", 0);
            }

            var trimmedType = model.RequestedVehicleType.Trim();
            var activeType = await _context.RequestVehicleTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IsActive && v.Name.ToLower() == trimmedType.ToLower());

            if (activeType == null)
            {
                return (false, "Geçersiz araç türü seçimi. Lütfen listeden aktif bir araç türü seçiniz.", 0);
            }

            // (b) Kişi sayısı (PassengerCount) zorunluluk ve sınır kontrolü (1 - 200)
            if (!model.PassengerCount.HasValue || model.PassengerCount.Value < 1 || model.PassengerCount.Value > 200)
            {
                return (false, "Kişi sayısı 1 ile 200 arasında bir değer olmalıdır.", 0);
            }

            // (c) Başlangıç ve bitiş tarih-saat zorunluluk, sıralama ve geçmiş zaman kontrolü
            if (!model.RequestedStartTime.HasValue || !model.RequestedEndTime.HasValue)
            {
                return (false, "Talep edilen başlangıç ve bitiş tarih ve saatleri zorunludur.", 0);
            }

            // Sunucu saati toleransı (1 dakika pay ile geçmiş zaman denetimi)
            if (model.RequestedStartTime.Value < DateTime.Now.AddMinutes(-1))
            {
                return (false, "Görev başlangıç tarihi geçmiş bir zaman olamaz.", 0);
            }

            if (model.RequestedEndTime.Value <= model.RequestedStartTime.Value)
            {
                return (false, "Bitiş tarih ve saati, başlangıç tarih ve saatinden sonra olmalıdır.", 0);
            }

            var request = new VehicleRequest
            {
                RequesterFullName = model.RequesterFullName.Trim(),
                Department = activeDept.Name,
                RequestedVehicleType = activeType.Name,
                PassengerCount = model.PassengerCount,
                RequestedStartTime = model.RequestedStartTime,
                RequestedEndTime = model.RequestedEndTime,
                RequesterEmail = email.ToLowerInvariant(),
                RequesterPhone = model.RequesterPhone.Trim(),
                RequestType = model.RequestType,
                Purpose = model.Purpose.Trim(),
                Status = VehicleRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = email.ToLowerInvariant(),
                IsDeleted = false
            };

            _context.VehicleRequests.Add(request);
            await _context.SaveChangesAsync();

            // Audit Log
            try
            {
                await _auditLogService.LogAsync(
                    nameof(VehicleRequest),
                    request.Id,
                    AuditAction.Create,
                    null, // Anonim public talep olduğu için null geçirilir, FK hatası önlenir
                    null,
                    new
                    {
                        request.RequesterFullName,
                        request.Department,
                        request.RequestedVehicleType,
                        request.PassengerCount,
                        request.RequestedStartTime,
                        request.RequestedEndTime,
                        request.RequesterEmail,
                        request.RequesterPhone,
                        request.Purpose,
                        Status = request.Status.ToString()
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Araç talebi için AuditLog kaydı oluşturulurken hata: {RequestId}", request.Id);
            }

            _logger.LogInformation("Yeni araç talebi alındı. ID: {RequestId}, Talep Eden: {Email}", request.Id, request.RequesterEmail);

            return (true, "Talebiniz alındı, Şef tarafından değerlendirilecek", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç talebi oluşturulurken beklenmeyen bir hata meydana geldi.");
            return (false, "Talep oluşturulurken bir sistem hatası oluştu. Lütfen daha sonra tekrar deneyiniz.", 0);
        }
    }

    public async Task<VehicleRequest?> GetRequestByIdAsync(int id)
    {
        return await _context.VehicleRequests
            .AsNoTracking()
            .Include(r => r.FulfilledAssignment)
            .Include(r => r.FulfilledByUser)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<VehicleRequestFilterViewModel> GetRequestsPagedAsync(VehicleRequestFilterViewModel filter)
    {
        var baseQuery = _context.VehicleRequests.AsNoTracking().AsQueryable();

        // Durum sayaçları
        filter.PendingCount = await _context.VehicleRequests.CountAsync(x => x.Status == VehicleRequestStatus.Pending);
        filter.FulfilledCount = await _context.VehicleRequests.CountAsync(x => x.Status == VehicleRequestStatus.Fulfilled);
        filter.RejectedCount = await _context.VehicleRequests.CountAsync(x => x.Status == VehicleRequestStatus.Rejected);

        // Mevcut birimler listesini doldur (filtre dropdown'u için)
        filter.AvailableDepartments = await _context.VehicleRequests
            .AsNoTracking()
            .Where(r => !string.IsNullOrEmpty(r.Department))
            .Select(r => r.Department!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var query = baseQuery;

        if (filter.Status.HasValue)
        {
            query = query.Where(r => r.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var dept = filter.Department.Trim();
            query = query.Where(r => r.Department == dept);
        }

        if (filter.StartDate.HasValue)
        {
            var startUtc = filter.StartDate.Value.Date;
            query = query.Where(r => r.CreatedAt >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = filter.EndDate.Value.Date.AddDays(1);
            query = query.Where(r => r.CreatedAt < endUtc);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(r =>
                r.RequesterFullName.ToLower().Contains(search) ||
                (r.Department != null && r.Department.ToLower().Contains(search)) ||
                r.RequesterEmail.ToLower().Contains(search) ||
                r.RequesterPhone.Contains(search) ||
                r.Purpose.ToLower().Contains(search));
        }

        filter.TotalCount = await query.CountAsync();
        filter.PageNumber = Math.Max(1, filter.PageNumber);
        filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        filter.Items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new VehicleRequestListItemViewModel
            {
                Id = r.Id,
                RequestType = r.RequestType,
                RequesterFullName = r.RequesterFullName,
                Department = r.Department,
                RequestedVehicleType = r.RequestedVehicleType,
                PassengerCount = r.PassengerCount,
                RequestedStartTime = r.RequestedStartTime,
                RequestedEndTime = r.RequestedEndTime,
                RequesterEmail = r.RequesterEmail,
                RequesterPhone = r.RequesterPhone,
                Purpose = r.Purpose,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                FulfilledAssignmentId = r.FulfilledAssignmentId,
                FulfilledByUserId = r.FulfilledByUserId,
                FulfilledByUserName = r.FulfilledByUser != null ? r.FulfilledByUser.FullName : null,
                FulfilledAt = r.FulfilledAt,
                RejectionReason = r.RejectionReason,
                ContactFullName = r.ContactFullName,
                ContactPhone = r.ContactPhone
            })
            .ToListAsync();

        return filter;
    }

    public async Task<VehicleRequestFulfillViewModel?> GetFulfillViewModelAsync(int requestId, DateTime? startTime = null, DateTime? endTime = null)
    {
        var request = await _context.VehicleRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
        {
            return null;
        }

        var plannedStart = startTime ?? request.RequestedStartTime ?? DateTime.Today.AddDays(1).AddHours(9);
        var plannedEnd = endTime ?? request.RequestedEndTime ?? DateTime.Today.AddDays(1).AddHours(17);

        // AssignmentService üzerinden araç ve şoför müsaitlik listesini al
        var assignmentCreateVm = await _assignmentService.GetCreateViewModelAsync(plannedStart, plannedEnd);

        // İletişim Kişileri Listesi: Admin, Ulaşım Şefleri (Kullanici) ve Şoförler (Driver)
        var contactPersons = new List<SelectableContactPersonViewModel>();
        try
        {
            var usersWithRoles = await (from u in _context.Users
                                        join ur in _context.UserRoles on u.Id equals ur.UserId
                                        join r in _context.Roles on ur.RoleId equals r.Id
                                        where u.IsActive && !string.IsNullOrEmpty(u.FullName)
                                        select new
                                        {
                                            u.Id,
                                            u.FullName,
                                            u.PhoneNumber,
                                            u.Email,
                                            RoleName = r.Name
                                        }).Distinct().ToListAsync();

            contactPersons = usersWithRoles
                .OrderBy(u => u.RoleName == "Admin" ? 1 : (u.RoleName == "Kullanici" ? 2 : 3))
                .ThenBy(u => u.FullName)
                .Select(u => new SelectableContactPersonViewModel
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    RoleName = u.RoleName == "Kullanici" ? "Ulaşım Şefi" : (u.RoleName == "Admin" ? "Yönetici" : "Şoför"),
                    PhoneNumber = u.PhoneNumber,
                    Email = u.Email
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "İletişim kişileri listesi çekilirken hata oluştu.");
        }

        // Varsayılan iletişim kişisi: İlk Ulaşım Şefi veya Admin
        var defaultContactId = contactPersons.FirstOrDefault(c => c.RoleName == "Ulaşım Şefi")?.UserId 
            ?? contactPersons.FirstOrDefault()?.UserId ?? string.Empty;

        return new VehicleRequestFulfillViewModel
        {
            RequestId = request.Id,
            RequesterFullName = request.RequesterFullName,
            Department = request.Department,
            RequesterEmail = request.RequesterEmail,
            RequesterPhone = request.RequesterPhone,
            Purpose = request.Purpose,
            CreatedAt = request.CreatedAt,
            PlannedStartTime = plannedStart,
            PlannedEndTime = plannedEnd,
            RequestedVehicleType = request.RequestedVehicleType,
            PassengerCount = request.PassengerCount,
            ContactUserId = defaultContactId,
            AvailableVehicles = assignmentCreateVm.AvailableVehicles,
            AvailableDrivers = assignmentCreateVm.AvailableDrivers,
            AvailableContactPersons = contactPersons,
            DepartmentTitle = !string.IsNullOrWhiteSpace(_emailSettings.DepartmentTitle)
                ? _emailSettings.DepartmentTitle
                : "Düzce Üniversitesi Ulaşım Şefliği"
        };
    }

    public async Task<(bool Success, string Message, int AssignmentId)> FulfillRequestAsync(VehicleRequestFulfillViewModel model, string adminUserId)
    {
        var request = await _context.VehicleRequests.FirstOrDefaultAsync(r => r.Id == model.RequestId);
        if (request == null)
        {
            return (false, "İşleme alınacak araç talebi bulunamadı.", 0);
        }

        if (request.Status != VehicleRequestStatus.Pending)
        {
            return (false, $"Bu talep zaten {request.Status.ToTurkishString()} durumundadır.", 0);
        }

        // 1. AssignmentCreateViewModel oluştur
        var purposeForAssignment = !string.IsNullOrWhiteSpace(request.Purpose) 
            ? request.Purpose.Trim() 
            : (!string.IsNullOrWhiteSpace(model.Purpose) ? model.Purpose.Trim() : "Resmi Kurum Görevi");

        if (!string.IsNullOrWhiteSpace(model.ChiefNote))
        {
            purposeForAssignment += $" (Şef Notu: {model.ChiefNote.Trim()})";
        }

        var assignmentModel = new AssignmentCreateViewModel
        {
            VehicleId = model.VehicleId,
            DriverId = model.DriverId,
            RequestedByDepartment = !string.IsNullOrWhiteSpace(request.Department) ? request.Department : request.RequesterFullName,
            Purpose = purposeForAssignment,
            PlannedStartTime = model.PlannedStartTime,
            PlannedEndTime = model.PlannedEndTime
        };

        // 2. Mevcut CreateAssignmentAsync çağrılır (tüm çakışma, ehliyet ve KM kontrolleri aynen çalışır)
        var (success, message, assignmentId) = await _assignmentService.CreateAssignmentAsync(assignmentModel, adminUserId);
        if (!success)
        {
            return (false, message, 0);
        }

        // İletişim kişisi bilgilerini çözümle
        string? contactFullName = null;
        string? contactPhone = null;
        string? contactEmail = null;

        if (!string.IsNullOrWhiteSpace(model.ContactUserId))
        {
            var contactUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == model.ContactUserId);
            if (contactUser != null)
            {
                contactFullName = contactUser.FullName;
                contactPhone = contactUser.PhoneNumber;
                contactEmail = contactUser.Email;
            }
        }

        // 3. VehicleRequest bilgilerini güncelle
        request.Status = VehicleRequestStatus.Fulfilled;
        request.FulfilledAssignmentId = assignmentId;
        request.FulfilledByUserId = adminUserId;
        request.FulfilledAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = adminUserId;
        request.ContactUserId = model.ContactUserId;
        request.ContactFullName = contactFullName;
        request.ContactPhone = contactPhone;
        request.ContactEmail = contactEmail;

        try
        {
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(VehicleRequest),
                request.Id,
                AuditAction.Update,
                adminUserId,
                new { Status = VehicleRequestStatus.Pending.ToString() },
                new
                {
                    Status = VehicleRequestStatus.Fulfilled.ToString(),
                    request.FulfilledAssignmentId,
                    request.FulfilledByUserId,
                    request.FulfilledAt,
                    request.ContactFullName,
                    request.ContactPhone
                });

            // 4. Talep sahibine bildirim maili gönder (Aşama 4 servisi ile)
            try
            {
                var assignment = await _assignmentService.GetAssignmentByIdAsync(assignmentId);
                if (assignment != null)
                {
                    await _emailService.SendRequestFulfilledEmailAsync(
                        request, 
                        assignment, 
                        model.DepartmentTitle,
                        model.ChiefNote,
                        contactFullName,
                        contactPhone,
                        contactEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Talep onay e-postası gönderilirken hata oluştu: Talep #{RequestId}", request.Id);
            }

            return (true, "Araç talebi başarıyla karşılandı ve görevlendirme oluşturuldu.", assignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep karşılandı olarak güncellenirken hata oluştu: Talep #{RequestId}", request.Id);
            return (true, "Görevlendirme oluşturuldu ancak talep durumu güncellenirken bir sorun oluştu.", assignmentId);
        }
    }

    public async Task<(bool Success, string Message)> RejectRequestAsync(int requestId, string rejectionReason, string adminUserId)
    {
        var request = await _context.VehicleRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null)
        {
            return (false, "Reddedilecek araç talebi bulunamadı.");
        }

        if (request.Status != VehicleRequestStatus.Pending)
        {
            return (false, $"Bu talep zaten {request.Status.ToTurkishString()} durumundadır.");
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            return (false, "Lütfen bir ret gerekçesi belirtiniz.");
        }

        request.Status = VehicleRequestStatus.Rejected;
        request.RejectionReason = rejectionReason.Trim();
        request.FulfilledByUserId = adminUserId;
        request.FulfilledAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = adminUserId;

        try
        {
            await _context.SaveChangesAsync();

            // Audit Log
            await _auditLogService.LogAsync(
                nameof(VehicleRequest),
                request.Id,
                AuditAction.Update,
                adminUserId,
                new { Status = VehicleRequestStatus.Pending.ToString() },
                new
                {
                    Status = VehicleRequestStatus.Rejected.ToString(),
                    request.RejectionReason,
                    request.FulfilledByUserId,
                    request.FulfilledAt
                });

            // Talep sahibine bildirim maili gönder (Aşama 4 servisi ile)
            try
            {
                await _emailService.SendRequestRejectedEmailAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Talep ret e-postası gönderilirken hata oluştu: Talep #{RequestId}", request.Id);
            }

            return (true, "Araç talebi gerekçesiyle birlikte reddedildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç talebi reddedilirken hata oluştu: Talep #{RequestId}", requestId);
            return (false, "Talep reddedilirken bir veritabanı hatası oluştu.");
        }
    }

    public async Task<PendingNotificationSummaryViewModel> GetPendingNotificationSummaryAsync()
    {
        try
        {
            var query = _context.VehicleRequests
                .Where(r => !r.IsDeleted && r.Status == VehicleRequestStatus.Pending);

            var count = await query.CountAsync();

            var latest = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new PendingNotificationItemViewModel
                {
                    Id = r.Id,
                    RequesterFullName = r.RequesterFullName,
                    Purpose = r.Purpose,
                    FormattedDate = r.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            return new PendingNotificationSummaryViewModel
            {
                PendingCount = count,
                LatestRequests = latest
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bekleyen araç talepleri bildirim özeti alınırken hata oluştu.");
            return new PendingNotificationSummaryViewModel();
        }
    }

    #region Talep Seçenekleri Yönetimi (Admin)

    public async Task<RequestOptionsPageViewModel> GetRequestOptionsAsync()
    {
        var departments = await _context.RequestDepartments
            .AsNoTracking()
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .ToListAsync();

        var vehicleTypes = await _context.RequestVehicleTypes
            .AsNoTracking()
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Name)
            .ToListAsync();

        return new RequestOptionsPageViewModel
        {
            Departments = departments,
            VehicleTypes = vehicleTypes
        };
    }

    public async Task<(bool Success, string Message)> SaveDepartmentAsync(RequestDepartmentCreateOrEditViewModel model, string adminUserId)
    {
        var name = model.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Birim adı boş olamaz.");
        }

        if (model.Id.HasValue && model.Id.Value > 0)
        {
            var dept = await _context.RequestDepartments.FirstOrDefaultAsync(d => d.Id == model.Id.Value);
            if (dept == null)
            {
                return (false, "Güncellenecek birim bulunamadı.");
            }

            dept.Name = name;
            dept.DisplayOrder = model.DisplayOrder;
            dept.IsActive = model.IsActive;
            dept.UpdatedAt = DateTime.UtcNow;
            dept.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();
            return (true, "Birim başarıyla güncellendi.");
        }
        else
        {
            var dept = new RequestDepartment
            {
                Name = name,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                IsDeleted = false
            };

            _context.RequestDepartments.Add(dept);
            await _context.SaveChangesAsync();
            return (true, "Yeni birim başarıyla eklendi.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleDepartmentActiveAsync(int id, string adminUserId)
    {
        var dept = await _context.RequestDepartments.FirstOrDefaultAsync(d => d.Id == id);
        if (dept == null)
        {
            return (false, "İşlem yapılacak birim bulunamadı.");
        }

        dept.IsActive = !dept.IsActive;
        dept.UpdatedAt = DateTime.UtcNow;
        dept.UpdatedBy = adminUserId;

        await _context.SaveChangesAsync();
        var statusText = dept.IsActive ? "aktif" : "pasif";
        return (true, $"Birim durumu {statusText} olarak güncellendi.");
    }

    public async Task<(bool Success, string Message)> SaveVehicleTypeAsync(RequestVehicleTypeCreateOrEditViewModel model, string adminUserId)
    {
        var name = model.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Araç türü adı boş olamaz.");
        }

        if (model.Id.HasValue && model.Id.Value > 0)
        {
            var vt = await _context.RequestVehicleTypes.FirstOrDefaultAsync(v => v.Id == model.Id.Value);
            if (vt == null)
            {
                return (false, "Güncellenecek araç türü bulunamadı.");
            }

            vt.Name = name;
            vt.DisplayOrder = model.DisplayOrder;
            vt.IsActive = model.IsActive;
            vt.UpdatedAt = DateTime.UtcNow;
            vt.UpdatedBy = adminUserId;

            await _context.SaveChangesAsync();
            return (true, "Araç türü başarıyla güncellendi.");
        }
        else
        {
            var vt = new RequestVehicleType
            {
                Name = name,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId,
                IsDeleted = false
            };

            _context.RequestVehicleTypes.Add(vt);
            await _context.SaveChangesAsync();
            return (true, "Yeni araç türü başarıyla eklendi.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleVehicleTypeActiveAsync(int id, string adminUserId)
    {
        var vt = await _context.RequestVehicleTypes.FirstOrDefaultAsync(v => v.Id == id);
        if (vt == null)
        {
            return (false, "İşlem yapılacak araç türü bulunamadı.");
        }

        vt.IsActive = !vt.IsActive;
        vt.UpdatedAt = DateTime.UtcNow;
        vt.UpdatedBy = adminUserId;

        await _context.SaveChangesAsync();
        var statusText = vt.IsActive ? "aktif" : "pasif";
        return (true, $"Araç türü durumu {statusText} olarak güncellendi.");
    }

    public async Task<List<string>> GetActiveDepartmentsAsync()
    {
        return await _context.RequestDepartments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .Select(d => d.Name)
            .ToListAsync();
    }

    public async Task<List<string>> GetActiveVehicleTypesAsync()
    {
        return await _context.RequestVehicleTypes
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Name)
            .Select(v => v.Name)
            .ToListAsync();
    }

    #endregion
}
