// File: Data/DbInitializer.cs
using System.Security.Cryptography;
using aractakipsys.Models.Entities;
using aractakipsys.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // 1. ROLLERİN OLUŞTURULMASI (Admin, Driver, Kullanici, DaireBaskani, GenelSekreter)
        string[] roles = { "Admin", "Driver", "Kullanici", "DaireBaskani", "GenelSekreter" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol oluşturuldu: {Role}", role);
            }
        }

        // 2. VARSAYILAN ADMIN KULLANICISI
        var adminEmail = "admin@duzce.edu.tr";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        var adminPassword = configuration["SeedUsers:AdminPassword"] ?? "Admin123!*";
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                FullName = "Sistem Yöneticisi (Bilgi İşlem)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("===============================================================================");
                Console.WriteLine(" [GÜVENLİK BİLGİSİ] Varsayılan Admin Kullanıcısı Oluşturuldu!");
                Console.WriteLine($"  E-posta       : {adminEmail}");
                Console.WriteLine($"  Kullanıcı Adı : {adminUser.UserName}");
                Console.WriteLine($"  Parola        : {adminPassword}");
                Console.WriteLine("===============================================================================");
                Console.ResetColor();

                logger.LogInformation("Varsayılan Admin oluşturuldu ({Email}).", adminEmail);
            }
            else
            {
                logger.LogError("Admin kullanıcısı oluşturulamadı: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Kullanıcı zaten varsa şifresine ASLA dokunulmaz (SecurityStamp korunur, oturum düşmez)
            // Sadece rolü eksikse tanımlanır:
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Mevcut Admin kullanıcısına eksik olan 'Admin' rolü tanımlandı.");
            }
        }

        // 2.1 VARSAYILAN KULLANICI (Birim Şefi / Personel)
        var sefEmail = "sef@duzce.edu.tr";
        var sefUser = await userManager.FindByEmailAsync(sefEmail);
        if (sefUser == null)
        {
            sefUser = new ApplicationUser
            {
                UserName = "sef",
                Email = sefEmail,
                FullName = "Ahmet Şef (Ulaşım Şefi)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var sefPassword = configuration["SeedUsers:KullaniciPassword"] ?? "Sef12345!*";
            var result = await userManager.CreateAsync(sefUser, sefPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(sefUser, "Kullanici");

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("===============================================================================");
                Console.WriteLine(" [BİLGİ] Varsayılan 'Kullanici' (Şef) Kullanıcısı Oluşturuldu!");
                Console.WriteLine($"  E-posta       : {sefEmail}");
                Console.WriteLine($"  Kullanıcı Adı : {sefUser.UserName}");
                Console.WriteLine($"  Parola        : {sefPassword}");
                Console.WriteLine("===============================================================================");
                Console.ResetColor();

                logger.LogInformation("Varsayılan Kullanici (Şef) oluşturuldu ({Email}).", sefEmail);
            }
            else
            {
                logger.LogError("Şef kullanıcısı oluşturulamadı: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Kullanıcı zaten varsa şifresine ASLA dokunulmaz (SecurityStamp korunur, oturum düşmez)
            // Sadece rolü eksikse tanımlanır:
            if (!await userManager.IsInRoleAsync(sefUser, "Kullanici"))
            {
                await userManager.AddToRoleAsync(sefUser, "Kullanici");
                logger.LogInformation("Mevcut Şef kullanıcısına eksik olan 'Kullanici' rolü tanımlandı.");
            }
        }

        // 3. TEST ŞOFÖRLERİ (Identity + Driver Entity - Transaction İçinde)
        var driver1Email = "ahmet.yilmaz@duzce.edu.tr";
        var driver1User = await userManager.FindByEmailAsync(driver1Email);
        var driver1Password = configuration["SeedUsers:Driver1Password"] ?? "Sofor123!*";
        if (driver1User == null)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var user1 = new ApplicationUser
                {
                    UserName = "ahmet.yilmaz",
                    Email = driver1Email,
                    FullName = "Ahmet Yılmaz",
                    EmailConfirmed = true,
                    PhoneNumber = "0532 111 22 33",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user1, driver1Password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user1, "Driver");

                    var driver1 = new Driver
                    {
                        UserId = user1.Id,
                        LicenseNumber = "TR-81-10293",
                        LicenseClass = "B",
                        LicenseExpiryDate = DateTime.UtcNow.AddYears(5),
                        Department = "Rektörlük Ulaştırma Birimi",
                        IsActive = true,
                        CreatedBy = adminUser?.Id ?? "SystemSeed",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Drivers.Add(driver1);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("-------------------------------------------------------------------------------");
                    Console.WriteLine(" [BİLGİ] Test Şoförü 1 Oluşturuldu:");
                    Console.WriteLine($"  E-posta       : {driver1Email}");
                    Console.WriteLine($"  Kullanıcı Adı : {user1.UserName}");
                    Console.WriteLine($"  Parola        : {driver1Password}");
                    Console.WriteLine("-------------------------------------------------------------------------------");
                    Console.ResetColor();

                    logger.LogInformation("Şoför 1 oluşturuldu: {FullName} ({Email})", user1.FullName, driver1Email);
                }
                else
                {
                    await transaction.RollbackAsync();
                    logger.LogError("Şoför 1 Identity kullanıcısı oluşturulamadı: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Şoför 1 oluşturulurken transaction hatası oluştu.");
            }
        }
        else
        {
            // Kullanıcı zaten varsa şifresine ASLA dokunulmaz (SecurityStamp korunur, oturum düşmez)
            // Sadece rolü eksikse tanımlanır:
            if (!await userManager.IsInRoleAsync(driver1User, "Driver"))
            {
                await userManager.AddToRoleAsync(driver1User, "Driver");
                logger.LogInformation("Mevcut Şoför 1 kullanıcısına eksik olan 'Driver' rolü tanımlandı.");
            }
        }

        var driver2Email = "mehmet.kaya@duzce.edu.tr";
        var driver2User = await userManager.FindByEmailAsync(driver2Email);
        var driver2Password = configuration["SeedUsers:Driver2Password"] ?? "Sofor123!*";
        if (driver2User == null)
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var user2 = new ApplicationUser
                {
                    UserName = "mehmet.kaya",
                    Email = driver2Email,
                    FullName = "Mehmet Kaya",
                    EmailConfirmed = true,
                    PhoneNumber = "0533 444 55 66",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user2, driver2Password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user2, "Driver");

                    var driver2 = new Driver
                    {
                        UserId = user2.Id,
                        LicenseNumber = "TR-81-48201",
                        LicenseClass = "D",
                        LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                        Department = "Sağlık Kültür ve Spor Dairesi",
                        IsActive = true,
                        CreatedBy = adminUser?.Id ?? "SystemSeed",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Drivers.Add(driver2);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("-------------------------------------------------------------------------------");
                    Console.WriteLine(" [BİLGİ] Test Şoförü 2 Oluşturuldu:");
                    Console.WriteLine($"  E-posta       : {driver2Email}");
                    Console.WriteLine($"  Kullanıcı Adı : {user2.UserName}");
                    Console.WriteLine($"  Parola        : {driver2Password}");
                    Console.WriteLine("-------------------------------------------------------------------------------");
                    Console.ResetColor();

                    logger.LogInformation("Şoför 2 oluşturuldu: {FullName} ({Email})", user2.FullName, driver2Email);
                }
                else
                {
                    await transaction.RollbackAsync();
                    logger.LogError("Şoför 2 Identity kullanıcısı oluşturulamadı: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Şoför 2 oluşturulurken transaction hatası oluştu.");
            }
        }
        else
        {
            // Kullanıcı zaten varsa şifresine ASLA dokunulmaz (SecurityStamp korunur, oturum düşmez)
            // Sadece rolü eksikse tanımlanır:
            if (!await userManager.IsInRoleAsync(driver2User, "Driver"))
            {
                await userManager.AddToRoleAsync(driver2User, "Driver");
                logger.LogInformation("Mevcut Şoför 2 kullanıcısına eksik olan 'Driver' rolü tanımlandı.");
            }
        }

        // 3.1 EK ŞOFÖRLER (10 Yeni Şoför - Identity + Driver Entity)
        var additionalDriverData = new[]
        {
            new { FullName = "Ali Demir", UserName = "ali.demir", Email = "ali.demir@duzce.edu.tr", Phone = "0532 201 11 22", LicenseNumber = "TR-81-20101", LicenseClass = "B", Department = "Rektörlük Ulaştırma Birimi" },
            new { FullName = "Hasan Çelik", UserName = "hasan.celik", Email = "hasan.celik@duzce.edu.tr", Phone = "0533 302 22 33", LicenseNumber = "TR-81-20102", LicenseClass = "D", Department = "Sağlık Kültür ve Spor Daire Bşk." },
            new { FullName = "Murat Aydın", UserName = "murat.aydin", Email = "murat.aydin@duzce.edu.tr", Phone = "0535 403 33 44", LicenseNumber = "TR-81-20103", LicenseClass = "CE", Department = "Yapı İşleri ve Teknik Daire Bşk." },
            new { FullName = "Osman Koç", UserName = "osman.koc", Email = "osman.koc@duzce.edu.tr", Phone = "0536 504 44 55", LicenseNumber = "TR-81-20104", LicenseClass = "B", Department = "Genel Sekreterlik" },
            new { FullName = "İbrahim Şahin", UserName = "ibrahim.sahin", Email = "ibrahim.sahin@duzce.edu.tr", Phone = "0537 605 55 66", LicenseNumber = "TR-81-20105", LicenseClass = "C", Department = "İdari ve Mali İşler Daire Bşk." },
            new { FullName = "Emre Yıldız", UserName = "emre.yildiz", Email = "emre.yildiz@duzce.edu.tr", Phone = "0538 706 66 77", LicenseNumber = "TR-81-20106", LicenseClass = "B", Department = "Öğrenci İşleri Daire Bşk." },
            new { FullName = "Mustafa Öztürk", UserName = "mustafa.ozturk", Email = "mustafa.ozturk@duzce.edu.tr", Phone = "0539 807 77 88", LicenseNumber = "TR-81-20107", LicenseClass = "D", Department = "Tıp Fakültesi Araştırma Hastanesi Ulaşım" },
            new { FullName = "Kemal Arslan", UserName = "kemal.arslan", Email = "kemal.arslan@duzce.edu.tr", Phone = "0542 908 88 99", LicenseNumber = "TR-81-20108", LicenseClass = "B", Department = "Ziraat Fakültesi Araştırma Birimi" },
            new { FullName = "Serkan Doğan", UserName = "serkan.dogan", Email = "serkan.dogan@duzce.edu.tr", Phone = "0543 019 99 00", LicenseNumber = "TR-81-20109", LicenseClass = "B", Department = "Orman Fakültesi Arazi Hizmetleri" },
            new { FullName = "Hüseyin Polat", UserName = "huseyin.polat", Email = "huseyin.polat@duzce.edu.tr", Phone = "0544 120 00 11", LicenseNumber = "TR-81-20110", LicenseClass = "CE", Department = "Çevre ve Sağlık Teknolojileri İhtisaslaşma Koor." }
        };

        foreach (var item in additionalDriverData)
        {
            var user = await userManager.FindByEmailAsync(item.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = item.UserName,
                    Email = item.Email,
                    FullName = item.FullName,
                    EmailConfirmed = true,
                    PhoneNumber = item.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createRes = await userManager.CreateAsync(user, "Sofor123!*");
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Driver");

                    var driverEntity = new Driver
                    {
                        UserId = user.Id,
                        LicenseNumber = item.LicenseNumber,
                        LicenseClass = item.LicenseClass,
                        LicenseExpiryDate = DateTime.UtcNow.AddYears(5),
                        Department = item.Department,
                        IsActive = true,
                        CreatedBy = adminUser?.Id ?? "SystemSeed",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Drivers.Add(driverEntity);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Ek test şoförü eklendi: {FullName} ({Email})", item.FullName, item.Email);
                }
                else
                {
                    logger.LogError("Ek test şoförü ({Email}) oluşturulamadı: {Errors}", item.Email,
                        string.Join(", ", createRes.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, "Driver"))
                {
                    await userManager.AddToRoleAsync(user, "Driver");
                }
            }
        }

        // 4. ÖRNEK ARAÇ KAYITLARI (Idle, OnDuty, Maintenance)
        if (!await context.Vehicles.AnyAsync())
        {
            var vehicles = new List<Vehicle>
            {
                new()
                {
                    PlateNumber = "81 DU 001",
                    Brand = "Renault",
                    Model = "Megane",
                    Year = 2022,
                    Color = "Beyaz",
                    VehicleType = "Sedan",
                    FuelType = "Dizel",
                    CurrentKm = 42500,
                    Status = VehicleStatus.Idle,
                    InspectionStartDate = new DateTime(2026, 1, 15),
                    InspectionExpiryDate = new DateTime(2027, 1, 15),
                    InsuranceStartDate = new DateTime(2026, 2, 1),
                    InsuranceExpiryDate = new DateTime(2027, 2, 1),
                    TireBrand = "Michelin Primacy 4",
                    TireYear = 2023,
                    TireSeason = "Yazlık",
                    Notes = "Rektörlük makam aracı",
                    CreatedBy = adminUser?.Id ?? "SystemSeed",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    PlateNumber = "81 DU 102",
                    Brand = "Ford",
                    Model = "Tourneo Custom",
                    Year = 2021,
                    Color = "Gümüş Gri",
                    VehicleType = "Minibüs",
                    FuelType = "Dizel",
                    CurrentKm = 78200,
                    Status = VehicleStatus.OnDuty,
                    InspectionStartDate = new DateTime(2026, 4, 10),
                    InspectionExpiryDate = new DateTime(2027, 4, 10),
                    InsuranceStartDate = new DateTime(2026, 3, 15),
                    InsuranceExpiryDate = new DateTime(2027, 3, 15),
                    TireBrand = "Continental VanContact",
                    TireYear = 2023,
                    TireSeason = "Yazlık",
                    Notes = "Personel ve heyet servis aracı",
                    CreatedBy = adminUser?.Id ?? "SystemSeed",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    PlateNumber = "81 DU 205",
                    Brand = "Fiat",
                    Model = "Egea",
                    Year = 2023,
                    Color = "Füme",
                    VehicleType = "Sedan",
                    FuelType = "Benzin",
                    CurrentKm = 19800,
                    Status = VehicleStatus.Maintenance,
                    InspectionStartDate = DateTime.Today.AddDays(15).AddYears(-1),
                    InspectionExpiryDate = DateTime.Today.AddDays(15),
                    InsuranceStartDate = new DateTime(2026, 5, 20),
                    InsuranceExpiryDate = new DateTime(2027, 5, 20),
                    TireBrand = "Lassa Driveways",
                    TireYear = 2024,
                    TireSeason = "Yazlık",
                    FineNotes = "Hız sınırı aşımı uyarısı (EDS - 12.08.2026)",
                    Notes = "10.000 KM periyodik bakımı ve fren balata değişimi yetkili serviste devam ediyor.",
                    CreatedBy = adminUser?.Id ?? "SystemSeed",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    PlateNumber = "81 DU 310",
                    Brand = "Volkswagen",
                    Model = "Crafter",
                    Year = 2020,
                    Color = "Beyaz",
                    VehicleType = "Otobüs",
                    FuelType = "Dizel",
                    CurrentKm = 115000,
                    Status = VehicleStatus.Idle,
                    InspectionStartDate = DateTime.Today.AddDays(-385),
                    InspectionExpiryDate = DateTime.Today.AddDays(-20),
                    InsuranceStartDate = DateTime.Today.AddDays(-375),
                    InsuranceExpiryDate = DateTime.Today.AddDays(-10),
                    TireBrand = "Bridgestone Duravis",
                    TireYear = 2022,
                    TireSeason = "Kışlık",
                    FineNotes = "Eksik evrak ve muayene gecikmesi tespiti",
                    Notes = "Öğrenci ve kongre ring aracı",
                    CreatedBy = adminUser?.Id ?? "SystemSeed",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Vehicles.AddRange(vehicles);
            await context.SaveChangesAsync();
            logger.LogInformation("4 adet örnek araç kaydı oluşturuldu.");
        }
        else
        {
            // Mevcut veritabanında daha önce oluşturulmuş 4 test aracını (81 DU 001, 102, 205, 310) özel olarak güncelle
            var testPlates = new[] { "81 DU 001", "81 DU 102", "81 DU 205", "81 DU 310" };
            var existingTestVehicles = await context.Vehicles
                .Where(v => testPlates.Contains(v.PlateNumber))
                .ToListAsync();

            bool hasChanges = false;
            foreach (var vehicle in existingTestVehicles)
            {
                switch (vehicle.PlateNumber)
                {
                    case "81 DU 001":
                        vehicle.InspectionStartDate = new DateTime(2026, 1, 15);
                        vehicle.InspectionExpiryDate = new DateTime(2027, 1, 15);
                        vehicle.InsuranceStartDate = new DateTime(2026, 2, 1);
                        vehicle.InsuranceExpiryDate = new DateTime(2027, 2, 1);
                        vehicle.TireBrand ??= "Michelin Primacy 4";
                        vehicle.TireYear ??= 2023;
                        vehicle.TireSeason ??= "Yazlık";
                        if (vehicle.Status == VehicleStatus.DocumentExpired)
                        {
                            vehicle.Status = VehicleStatus.Idle;
                        }
                        hasChanges = true;
                        break;

                    case "81 DU 102":
                        vehicle.InspectionStartDate = new DateTime(2026, 4, 10);
                        vehicle.InspectionExpiryDate = new DateTime(2027, 4, 10);
                        vehicle.InsuranceStartDate = new DateTime(2026, 3, 15);
                        vehicle.InsuranceExpiryDate = new DateTime(2027, 3, 15);
                        vehicle.TireBrand ??= "Continental VanContact";
                        vehicle.TireYear ??= 2023;
                        vehicle.TireSeason ??= "Yazlık";
                        if (vehicle.Status == VehicleStatus.DocumentExpired)
                        {
                            vehicle.Status = VehicleStatus.OnDuty;
                        }
                        hasChanges = true;
                        break;

                    case "81 DU 205":
                        vehicle.InspectionStartDate = DateTime.Today.AddDays(15).AddYears(-1);
                        vehicle.InspectionExpiryDate = DateTime.Today.AddDays(15);
                        vehicle.InsuranceStartDate = new DateTime(2026, 5, 20);
                        vehicle.InsuranceExpiryDate = new DateTime(2027, 5, 20);
                        vehicle.TireBrand ??= "Lassa Driveways";
                        vehicle.TireYear ??= 2024;
                        vehicle.TireSeason ??= "Yazlık";
                        vehicle.FineNotes ??= "Hız sınırı aşımı uyarısı (EDS - 12.08.2026)";
                        // 15 gün sonra dolacak (geçerli) olduğu için DocumentExpired olamaz
                        if (vehicle.Status == VehicleStatus.DocumentExpired)
                        {
                            vehicle.Status = VehicleStatus.Maintenance;
                        }
                        hasChanges = true;
                        break;

                    case "81 DU 310":
                        vehicle.InspectionStartDate = DateTime.Today.AddDays(-385);
                        vehicle.InspectionExpiryDate = DateTime.Today.AddDays(-20);
                        vehicle.InsuranceStartDate = DateTime.Today.AddDays(-375);
                        vehicle.InsuranceExpiryDate = DateTime.Today.AddDays(-10);
                        vehicle.TireBrand ??= "Bridgestone Duravis";
                        vehicle.TireYear ??= 2022;
                        vehicle.TireSeason ??= "Kışlık";
                        vehicle.FineNotes ??= "Eksik evrak ve muayene gecikmesi tespiti";
                        vehicle.Status = VehicleStatus.DocumentExpired;
                        hasChanges = true;
                        break;
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Mevcut 4 test aracının muayene, sigorta ve lastik/ceza bilgileri güncellendi.");
            }
        }

        // 4.1 EK ARAÇ KAYITLARI (10 Yeni Araç: 81 DU 103 - 81 DU 112)
        var additionalVehicles = new List<Vehicle>
        {
            new()
            {
                PlateNumber = "81 DU 103",
                Brand = "Ford",
                Model = "Focus",
                Year = 2021,
                Color = "Beyaz",
                VehicleType = "Sedan",
                FuelType = "Dizel",
                CurrentKm = 54200,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 2, 10),
                InspectionExpiryDate = new DateTime(2027, 2, 10),
                InsuranceStartDate = new DateTime(2026, 3, 1),
                InsuranceExpiryDate = new DateTime(2027, 3, 1),
                TireBrand = "Goodyear EfficientGrip",
                TireYear = 2023,
                TireSeason = "Yazlık",
                Notes = "Mühendislik Fakültesi resmi hizmet aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 104",
                Brand = "Renault",
                Model = "Clio",
                Year = 2022,
                Color = "Gri",
                VehicleType = "Hatchback",
                FuelType = "Benzin",
                CurrentKm = 32400,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 3, 15),
                InspectionExpiryDate = new DateTime(2027, 3, 15),
                InsuranceStartDate = new DateTime(2026, 4, 1),
                InsuranceExpiryDate = new DateTime(2027, 4, 1),
                TireBrand = "Lassa Greenways",
                TireYear = 2024,
                TireSeason = "Yazlık",
                Notes = "Şehir içi resmi evrak dağıtım aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 105",
                Brand = "Toyota",
                Model = "Corolla",
                Year = 2023,
                Color = "Beyaz",
                VehicleType = "Sedan",
                FuelType = "Hibrit",
                CurrentKm = 21100,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 5, 20),
                InspectionExpiryDate = new DateTime(2027, 5, 20),
                InsuranceStartDate = new DateTime(2026, 6, 1),
                InsuranceExpiryDate = new DateTime(2027, 6, 1),
                TireBrand = "Bridgestone Turanza",
                TireYear = 2023,
                TireSeason = "Dört Mevsim",
                Notes = "Genel Sekreterlik ve misafir heyet protokol aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 106",
                Brand = "Fiat",
                Model = "Doblo",
                Year = 2020,
                Color = "Beyaz",
                VehicleType = "Kombi",
                FuelType = "Dizel",
                CurrentKm = 91500,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 1, 5),
                InspectionExpiryDate = new DateTime(2027, 1, 5),
                InsuranceStartDate = new DateTime(2026, 1, 20),
                InsuranceExpiryDate = new DateTime(2027, 1, 20),
                TireBrand = "Pirelli Carrier",
                TireYear = 2022,
                TireSeason = "Kışlık",
                Notes = "Yapı İşleri ve Teknik Daire Bşk. saha aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 107",
                Brand = "Ford",
                Model = "Transit",
                Year = 2022,
                Color = "Mavi",
                VehicleType = "Panelvan",
                FuelType = "Dizel",
                CurrentKm = 67800,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 4, 18),
                InspectionExpiryDate = new DateTime(2027, 4, 18),
                InsuranceStartDate = new DateTime(2026, 5, 1),
                InsuranceExpiryDate = new DateTime(2027, 5, 1),
                TireBrand = "Continental VanContact",
                TireYear = 2023,
                TireSeason = "Dört Mevsim",
                Notes = "Kütüphane ve arşiv malzeme nakil aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 108",
                Brand = "Mercedes-Benz",
                Model = "Sprinter",
                Year = 2021,
                Color = "Beyaz",
                VehicleType = "Minibüs",
                FuelType = "Dizel",
                CurrentKm = 83600,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 3, 10),
                InspectionExpiryDate = new DateTime(2027, 3, 10),
                InsuranceStartDate = new DateTime(2026, 3, 25),
                InsuranceExpiryDate = new DateTime(2027, 3, 25),
                TireBrand = "Michelin Agilis",
                TireYear = 2023,
                TireSeason = "Yazlık",
                Notes = "Akademik personel ve kongre servis minibüsü",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 109",
                Brand = "Isuzu",
                Model = "Novo",
                Year = 2019,
                Color = "Beyaz",
                VehicleType = "Otobüs",
                FuelType = "Dizel",
                CurrentKm = 138400,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 2, 28),
                InspectionExpiryDate = new DateTime(2027, 2, 28),
                InsuranceStartDate = new DateTime(2026, 3, 12),
                InsuranceExpiryDate = new DateTime(2027, 3, 12),
                TireBrand = "Bridgestone Duravis",
                TireYear = 2022,
                TireSeason = "Kışlık",
                Notes = "Öğrenci toplulukları ve spor kafileleri otobüsü",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 110",
                Brand = "Dacia",
                Model = "Duster",
                Year = 2023,
                Color = "Kahverengi",
                VehicleType = "SUV",
                FuelType = "Dizel",
                CurrentKm = 18900,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 6, 12),
                InspectionExpiryDate = new DateTime(2027, 6, 12),
                InsuranceStartDate = new DateTime(2026, 6, 25),
                InsuranceExpiryDate = new DateTime(2027, 6, 25),
                TireBrand = "Goodyear Vector 4Seasons",
                TireYear = 2024,
                TireSeason = "Dört Mevsim",
                Notes = "Orman Fakültesi ve Çevre Enstitüsü arazi araştırma aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 111",
                Brand = "Volkswagen",
                Model = "Passat",
                Year = 2021,
                Color = "Siyah",
                VehicleType = "Sedan",
                FuelType = "Benzin",
                CurrentKm = 61200,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 1, 22),
                InspectionExpiryDate = new DateTime(2027, 1, 22),
                InsuranceStartDate = new DateTime(2026, 2, 5),
                InsuranceExpiryDate = new DateTime(2027, 2, 5),
                TireBrand = "Continental PremiumContact",
                TireYear = 2023,
                TireSeason = "Yazlık",
                Notes = "Rektör Yardımcılığı makam hizmet aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 112",
                Brand = "Hyundai",
                Model = "Tucson",
                Year = 2024,
                Color = "Kurşun Gri",
                VehicleType = "SUV",
                FuelType = "Hibrit",
                CurrentKm = 11500,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 7, 1),
                InspectionExpiryDate = new DateTime(2027, 7, 1),
                InsuranceStartDate = new DateTime(2026, 7, 15),
                InsuranceExpiryDate = new DateTime(2027, 7, 15),
                TireBrand = "Michelin Primacy 4+",
                TireYear = 2024,
                TireSeason = "Yazlık",
                Notes = "Tıp Fakültesi Dekanlığı ve Hastane Başhekimlik aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 113",
                Brand = "Skoda",
                Model = "Octavia",
                Year = 2023,
                Color = "Beyaz",
                VehicleType = "Sedan",
                FuelType = "Benzin",
                CurrentKm = 24500,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 3, 1),
                InspectionExpiryDate = new DateTime(2027, 3, 1),
                InsuranceStartDate = new DateTime(2026, 3, 15),
                InsuranceExpiryDate = new DateTime(2027, 3, 15),
                TireBrand = "Continental EcoContact 6",
                TireYear = 2023,
                TireSeason = "Yazlık",
                Notes = "Rektörlük Protokol ve Dış İlişkiler Birimi hizmet aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 114",
                Brand = "Peugeot",
                Model = "Rifter",
                Year = 2022,
                Color = "Füme",
                VehicleType = "Kombi",
                FuelType = "Dizel",
                CurrentKm = 41200,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 4, 1),
                InspectionExpiryDate = new DateTime(2027, 4, 1),
                InsuranceStartDate = new DateTime(2026, 4, 10),
                InsuranceExpiryDate = new DateTime(2027, 4, 10),
                TireBrand = "Michelin Primacy 4",
                TireYear = 2023,
                TireSeason = "Dört Mevsim",
                Notes = "Sağlık Kültür ve Spor Daire Başkanlığı etkinlik lojistik aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 115",
                Brand = "Toyota",
                Model = "Hilux",
                Year = 2023,
                Color = "Metalik Gri",
                VehicleType = "Pick-up",
                FuelType = "Dizel",
                CurrentKm = 16800,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 5, 10),
                InspectionExpiryDate = new DateTime(2027, 5, 10),
                InsuranceStartDate = new DateTime(2026, 5, 20),
                InsuranceExpiryDate = new DateTime(2027, 5, 20),
                TireBrand = "Bridgestone Dueler A/T",
                TireYear = 2024,
                TireSeason = "Dört Mevsim",
                Notes = "Orman ve Ziraat Fakülteleri arazi araştırma ve numune nakil aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 116",
                Brand = "Otokar",
                Model = "Sultan Maxi",
                Year = 2021,
                Color = "Beyaz",
                VehicleType = "Otobüs",
                FuelType = "Dizel",
                CurrentKm = 88400,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 2, 15),
                InspectionExpiryDate = new DateTime(2027, 2, 15),
                InsuranceStartDate = new DateTime(2026, 2, 25),
                InsuranceExpiryDate = new DateTime(2027, 2, 25),
                TireBrand = "Goodyear Marathon",
                TireYear = 2023,
                TireSeason = "Yazlık",
                Notes = "Öğrenci kulüpleri ve akademik heyetler için 31 kişilik midibüs",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                PlateNumber = "81 DU 117",
                Brand = "Renault",
                Model = "Austral",
                Year = 2024,
                Color = "Yıldız Siyah",
                VehicleType = "SUV",
                FuelType = "Hibrit",
                CurrentKm = 8900,
                Status = VehicleStatus.Idle,
                InspectionStartDate = new DateTime(2026, 8, 1),
                InspectionExpiryDate = new DateTime(2027, 8, 1),
                InsuranceStartDate = new DateTime(2026, 8, 15),
                InsuranceExpiryDate = new DateTime(2027, 8, 15),
                TireBrand = "Michelin e.Primacy",
                TireYear = 2024,
                TireSeason = "Yazlık",
                Notes = "Tıp Fakültesi Dekanlığı ve Hastane Başhekimlik protokol aracı",
                CreatedBy = adminUser?.Id ?? "SystemSeed",
                CreatedAt = DateTime.UtcNow
            }
        };

        bool anyNewVehicleAdded = false;
        foreach (var v in additionalVehicles)
        {
            if (!await context.Vehicles.AnyAsync(x => x.PlateNumber == v.PlateNumber))
            {
                context.Vehicles.Add(v);
                anyNewVehicleAdded = true;
                logger.LogInformation("Ek araç eklendi: {PlateNumber} ({Brand} {Model})", v.PlateNumber, v.Brand, v.Model);
            }
        }

        if (anyNewVehicleAdded)
        {
            await context.SaveChangesAsync();
        }

        // 5. ÖRNEK GÖREVLER (Planned, InProgress, Completed)
        if (!await context.Assignments.AnyAsync())
        {
            var driverAhmet = await context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.User.Email == driver1Email);
            var driverMehmet = await context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.User.Email == driver2Email);
            var vehicleMegane = await context.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == "81 DU 001");
            var vehicleFord = await context.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == "81 DU 102");

            if (driverAhmet != null && driverMehmet != null && vehicleMegane != null && vehicleFord != null)
            {
                var assignments = new List<Assignment>
                {
                    new()
                    {
                        VehicleId = vehicleMegane.Id,
                        DriverId = driverAhmet.Id,
                        RequestedByDepartment = "Mühendislik Fakültesi Dekanlığı",
                        Purpose = "TÜBİTAK Proje Çalıştayı için Ankara ODTÜ Teknokent gidiş-dönüş",
                        PlannedStartTime = DateTime.UtcNow.AddHours(2),
                        PlannedEndTime = DateTime.UtcNow.AddHours(10),
                        Status = AssignmentStatus.Planned,
                        CreatedBy = adminUser?.Id ?? "SystemSeed",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        VehicleId = vehicleFord.Id,
                        DriverId = driverAhmet.Id,
                        RequestedByDepartment = "Rektörlük Özel Kalem",
                        Purpose = "Düzce Valiliği İl Koordinasyon Kurulu toplantısı protokol ulaşımı",
                        PlannedStartTime = DateTime.UtcNow.AddHours(-2),
                        PlannedEndTime = DateTime.UtcNow.AddHours(4),
                        ActualStartTime = DateTime.UtcNow.AddHours(-2),
                        StartKm = 78200,
                        Status = AssignmentStatus.InProgress,
                        CreatedBy = adminUser?.Id ?? "SystemSeed",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        VehicleId = vehicleMegane.Id,
                        DriverId = driverMehmet.Id,
                        RequestedByDepartment = "Bilgi İşlem Daire Başkanlığı",
                        Purpose = "Akçakoca Kampüsü sunucu odası ve ağ switch cihazlarının periyodik bakımı",
                        PlannedStartTime = DateTime.UtcNow.AddDays(-3).AddHours(8),
                        PlannedEndTime = DateTime.UtcNow.AddDays(-3).AddHours(16),
                        ActualStartTime = DateTime.UtcNow.AddDays(-3).AddHours(8),
                        ActualEndTime = DateTime.UtcNow.AddDays(-3).AddHours(15).AddMinutes(40),
                        StartKm = 42350,
                        EndKm = 42500,
                        Status = AssignmentStatus.Completed,
                        DriverReturnNotes = "Araç temiz teslim edildi, yakıt deposu 3/4 seviyesinde bırakıldı.",
                        CreatedBy = adminUser?.Id ?? "SystemSeed",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    }
                };

                context.Assignments.AddRange(assignments);
                await context.SaveChangesAsync();
                logger.LogInformation("3 adet örnek görev (Planned, InProgress, Completed) oluşturuldu.");
            }
        }

        // 6. TUTARSIZLIK GİDERME (Self-Healing): Açık bakım kaydı olmayan ama Status=Maintenance kalan araçları düzelt
        var orphanedMaintenanceVehicles = await context.Vehicles
            .Where(v => !v.IsDeleted && v.Status == VehicleStatus.Maintenance)
            .Where(v => !context.Maintenances.Any(m => !m.IsDeleted && m.VehicleId == v.Id && m.EndDate == null))
            .ToListAsync();

        if (orphanedMaintenanceVehicles.Any())
        {
            foreach (var v in orphanedMaintenanceVehicles)
            {
                v.Status = VehicleStatus.Idle;
                v.Notes = "Periyodik bakımı tamamlandı, araç hizmete hazır.";
                v.UpdatedAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
            logger.LogInformation("{Count} adet açık bakım kaydı bulunmayan yetim araç Boşta (Idle) durumuna getirildi.", orphanedMaintenanceVehicles.Count);
        }

        // 7. ÖRNEK ARAÇ TALEPLERİ (10 Adet Bekleyen Talep)
        var sampleRequests = new List<VehicleRequest>
        {
            new()
            {
                RequesterFullName = "Prof. Dr. Mehmet Emin Aksoy",
                RequesterEmail = "mehmet.aksoy@duzce.edu.tr",
                RequesterPhone = "0532 312 45 67",
                Department = "Tıp Fakültesi Dekanlığı",
                Purpose = "İstanbul Üniversitesi Çapa Tıp Fakültesi ortak kardiyoloji sempozyumu ve vaka değerlendirme toplantısına heyet intikali.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                RequesterFullName = "Doç. Dr. Elif Zeynep Kaya",
                RequesterEmail = "elif.kaya@duzce.edu.tr",
                RequesterPhone = "0533 421 78 90",
                Department = "Orman Fakültesi",
                Purpose = "Yığılca ve Samandere orman alanlarında endemik bitki türleri izleme ve habitat tespiti arazi çalışması.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                RequesterFullName = "Dr. Öğr. Üyesi Barış Çetin",
                RequesterEmail = "baris.cetin@duzce.edu.tr",
                RequesterPhone = "0535 554 12 34",
                Department = "Mühendislik Fakültesi / Bilgisayar Müh.",
                Purpose = "TEKNOFEST Havacılık ve Uzay Festivali yarışma takım ekipmanlarının Kocaeli Bilişim Vadisi'ne sevki ve takım ulaşımı.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new()
            {
                RequesterFullName = "Zeynep Karahan",
                RequesterEmail = "zeynep.karahan@duzce.edu.tr",
                RequesterPhone = "0536 678 90 12",
                Department = "Öğrenci İşleri Daire Başkanlığı",
                Purpose = "YÖK ve ÖSYM arşiv teslimatları ile resmi diploma/evrak tasdik işlemleri için Ankara resmi kurum ziyareti.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new()
            {
                RequesterFullName = "Murat Selim Yıldız",
                RequesterEmail = "murat.yildiz@duzce.edu.tr",
                RequesterPhone = "0537 789 01 23",
                Department = "Yapı İşleri ve Teknik Daire Başkanlığı",
                Purpose = "Gölyaka Meslek Yüksekokulu derslik güçlendirme ve ısı yalıtım inşaat şantiyesinin yerinde statik denetimi.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new()
            {
                RequesterFullName = "Prof. Dr. Serdar Güven",
                RequesterEmail = "serdar.guven@duzce.edu.tr",
                RequesterPhone = "0538 890 12 34",
                Department = "Ziraat Fakültesi",
                Purpose = "Tarımsal Araştırma ve Uygulama Merkezi fındık zararlıları ile biyolojik mücadele deneme parselleri saha incelemesi.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            },
            new()
            {
                RequesterFullName = "Ece Nur Demirtaş",
                RequesterEmail = "ece.demirtas@duzce.edu.tr",
                RequesterPhone = "0539 901 23 45",
                Department = "Sağlık, Kültür ve Spor Daire Başkanlığı",
                Purpose = "Üniversiteler Arası Basketbol Bölge Şampiyonası kafilesinin Bolu Abant İzzet Baysal Üniversitesi tesislerine intikali.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-7)
            },
            new()
            {
                RequesterFullName = "Ali Rıza Keskin",
                RequesterEmail = "aliriza.keskin@duzce.edu.tr",
                RequesterPhone = "0541 123 45 67",
                Department = "Bilgi İşlem Daire Başkanlığı",
                Purpose = "Çilimli MYO yerleşkesi fiber optik omurga hattı sonlandırma ve ana ağ anahtarı montaj çalışması.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-8)
            },
            new()
            {
                RequesterFullName = "Doç. Dr. Hakan Taşçı",
                RequesterEmail = "hakan.tasci@duzce.edu.tr",
                RequesterPhone = "0542 234 56 78",
                Department = "Fen Edebiyat Fakültesi / Kimya Bölümü",
                Purpose = "TÜBİTAK MAM (Marmara Araştırma Merkezi - Gebze) laboratuvarlarında spektroskopik analiz ve test numuneleri transferi.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-9)
            },
            new()
            {
                RequesterFullName = "Selin Arda",
                RequesterEmail = "selin.arda@duzce.edu.tr",
                RequesterPhone = "0543 345 67 89",
                Department = "Dış İlişkiler ve Erasmus Koordinatörlüğü",
                Purpose = "Sabiha Gökçen Havalimanı'na iniş yapacak Erasmus+ konsorsiyumu misafir yabancı akademisyen heyetinin karşılanması.",
                RequestType = VehicleRequestType.OfficialTrip,
                Status = VehicleRequestStatus.Pending,
                CreatedBy = "SystemSeed",
                CreatedAt = DateTime.UtcNow.AddHours(-10)
            }
        };

        bool anyNewRequestAdded = false;
        foreach (var req in sampleRequests)
        {
            if (!await context.VehicleRequests.AnyAsync(r => r.RequesterEmail == req.RequesterEmail && r.Purpose == req.Purpose))
            {
                context.VehicleRequests.Add(req);
                anyNewRequestAdded = true;
                logger.LogInformation("Örnek araç talebi eklendi: {Requester} - {Department}", req.RequesterFullName, req.Department);
            }
        }

        if (anyNewRequestAdded)
        {
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Kriptografik olarak güçlü, rastgele ve güvenli bir parola üretir (AGENTS.md 4.2).
    /// </summary>
    private static string GenerateSecurePassword(int length = 12)
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specials = "!@$?_-*";

        var passwordChars = new List<char>
        {
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            specials[RandomNumberGenerator.GetInt32(specials.Length)]
        };

        var allChars = lower + upper + digits + specials;
        while (passwordChars.Count < length)
        {
            passwordChars.Add(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        // Karakterleri rastgele karıştır
        for (int i = passwordChars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
        }

        return new string(passwordChars.ToArray());
    }
}
