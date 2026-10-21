// File: Program.cs
using aractakipsys.Common.ModelBinders;
using aractakipsys.Data;
using aractakipsys.Models.Entities;
using aractakipsys.Services.Implementations;
using aractakipsys.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. ConnectionString & DbContext Kaydı (MS SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 1.1 DataProtection Anahtar Kalıcılığı (Uygulama yeniden başladığında çerezlerin bozulmasını ve oturumun düşmesini önler)
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
if (!Directory.Exists(keysFolder))
{
    Directory.CreateDirectory(keysFolder);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("aractakipsys");

// 2. ASP.NET Core Identity Servis Kaydı (Kurumsal Güvenlik Politikası)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Parola Karmaşıklık Gereksinimleri
    options.Password.RequireDigit = true;            // En az 1 rakam zorunlu
    options.Password.RequireLowercase = true;        // En az 1 küçük harf zorunlu
    options.Password.RequireUppercase = true;        // En az 1 büyük harf zorunlu
    options.Password.RequireNonAlphanumeric = true;   // En az 1 özel karakter zorunlu (!@$?_-*)
    options.Password.RequiredLength = 10;            // En az 10 karakter zorunlu
    options.Password.RequiredUniqueChars = 3;        // En az 3 farklı karakter içermeli

    // Kullanıcı Gereksinimleri
    options.User.RequireUniqueEmail = true;          // E-posta adresi benzersiz olmalı

    // Kaba Kuvvet (Brute-Force) Saldırılarına Karşı Hesap Kilitleme
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // 15 dakika kilitleme süresi
    options.Lockout.MaxFailedAccessAttempts = 5;                       // 5 hatalı denemede hesap kilitlenir
    options.Lockout.AllowedForNewUsers = true;                         // Yeni kullanıcılar için aktif
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 2.1 Cookie ve Kimlik Doğrulama Yönlendirmeleri (LoginPath & AccessDeniedPath)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

// 2.2 Security Stamp Doğrulama Sıklığı (Gereksiz kısa aralıklı session düşmelerini önler)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromHours(8);
});

// 3. Servis Katmanı Bağımlılıkları (DI)
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IVehicleRequestService, VehicleRequestService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IFaultReportService, FaultReportService>();
builder.Services.AddScoped<IKmValidationService, KmValidationService>();

// 3.2 Arka Plan Servisleri (Background / Hosted Services)
builder.Services.AddHostedService<aractakipsys.Services.BackgroundServices.VehicleDocumentExpirationWorker>();

// 3.1 E-Posta Servisi (Host yapılandırılmışsa SmtpEmailService, boşsa simülasyon ConsoleEmailService)
builder.Services.Configure<aractakipsys.Models.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var emailHost = builder.Configuration["EmailSettings:Host"];
if (!string.IsNullOrWhiteSpace(emailHost))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}

// 4. MVC Controller ve View Servisleri (Özel Invariant Decimal Model Binder ile hem nokta hem virgül desteği)
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});

// 4.1 İstek Hız Sınırlandırması (Rate Limiting - ASP.NET Core Built-in)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var isAjaxOrApi = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json");

        if (isAjaxOrApi)
        {
            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Çok fazla istekte bulundunuz, lütfen 1 dakika sonra tekrar deneyin."
            }, token);
        }
        else
        {
            context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
            await context.HttpContext.Response.WriteAsync(
                "<!DOCTYPE html><html lang='tr'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>" +
                "<title>429 - Çok Fazla İstek</title>" +
                "<style>" +
                "body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background-color:#f4f6f9;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;padding:15px;}" +
                ".card{background:#fff;border-radius:12px;box-shadow:0 10px 30px rgba(0,0,0,0.08);padding:35px 30px;max-width:480px;width:100%;text-align:center;border-top:5px solid #e02424;}" +
                ".icon{width:64px;height:64px;background:#fee2e2;color:#dc2626;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;font-size:28px;margin:0 auto 20px;font-weight:bold;}" +
                "h2{color:#1f2937;margin:0 0 12px;font-size:1.4rem;font-weight:700;}" +
                "p{color:#4b5563;font-size:0.95rem;line-height:1.6;margin:0 0 24px;}" +
                ".btn{display:inline-block;padding:10px 24px;background:#1e3c72;color:#fff;text-decoration:none;border-radius:6px;font-weight:600;font-size:0.9rem;transition:opacity 0.2s;}" +
                ".btn:hover{opacity:0.9;}" +
                "</style></head>" +
                "<body><div class='card'>" +
                "<div class='icon'>!</div>" +
                "<h2>Çok Fazla İstek Yapıldı</h2>" +
                "<p>Çok fazla istekte bulundunuz, lütfen 1 dakika sonra tekrar deneyin.</p>" +
                "<a href='javascript:location.reload()' class='btn'>Sayfayı Yenile</a>" +
                "</div></body></html>",
                token);
        }
    };

    // 4. Grup: Global Kalkan (Tüm sistem - IP başına dakikada 150 istek)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 150,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // 1. Grup: Login Politikası (IP başına dakikada 5 deneme - Kayan Pencere / Sliding Window)
    options.AddPolicy("login-limit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // 1. Grup: ForgotPassword Politikası (IP başına 5 dakikada 3 istek)
    options.AddPolicy("forgot-password-limit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            });
    });

    // 1. Grup: ResetPassword Politikası (IP başına 5 dakikada 5 istek)
    options.AddPolicy("reset-password-limit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            });
    });

    // 1. Grup: Captcha Yenileme Politikası (IP başına dakikada 15 istek)
    options.AddPolicy("captcha-limit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // 2. Grup: Araç Talep Formu Politikası (IP başına dakikada 5 istek - Kayan Pencere / Sliding Window)
    options.AddPolicy("vehicle-request-limit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// 5. QuestPDF Lisans Yapılandırması (Community Lisansı - Üniversite/Eğitim Kurumu)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// HTTP Request Pipeline Konfigürasyonu
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Rate Limiting Ara Yazılımı (Routing sonrası, Auth öncesi)
app.UseRateLimiter();

// Mobil cihazlarda geliştirme esnasında eski arayüzün önbellekten (cache) gelmesini engeller
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var path = context.Request.Path.Value ?? "";
        if (context.Response.ContentType?.Contains("text/html") == true ||
            path == "/" ||
            path.StartsWith("/Home") ||
            path.StartsWith("/Report") ||
            path.StartsWith("/Vehicle") ||
            path.StartsWith("/Assignment") ||
            path.StartsWith("/Maintenance") ||
            path.StartsWith("/Driver") ||
            path.StartsWith("/Surucu"))
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
        }
        return Task.CompletedTask;
    });
    await next();
});

// Identity Authentication & Authorization ara yazılımları
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// 5. Seed Data Çalıştırma (app.Run() öncesi)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı seed işlemi sırasında bir hata oluştu.");
    }
}

app.Run();
