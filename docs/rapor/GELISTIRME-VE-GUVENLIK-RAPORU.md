# DÜZCE ÜNİVERSİTESİ ARAÇ TAKİP PANELİ
## KAPSAMLI GELİŞTİRME VE GÜVENLİK ÇALIŞMALARI RAPORU

> **Sistem Adı:** Düzce Üniversitesi Araç Takip Paneli (Araç Takip ve Görevlendirme Sistemi)  
> **Kurum:** Düzce Üniversitesi Bilgi İşlem Daire Başkanlığı  
> **Proje Sahibi:** Eslem Pekdemir  
> **Rapor Tarihi:** 23 Kasım 2026  
> **Toplam Migration Sayısı:** 27  

---

## İçindekiler
1. [Proje Bilgileri](#1-proje-bilgileri)
2. [Geliştirme Aşamaları (Kronolojik Migration Geçmişi)](#2-geliştirme-aşamaları-kronolojik-migration-geçmişi)
3. [Modül Modül Yapılan Çalışmalar](#3-modül-modül-yapılan-çalışmalar)
4. [Hata Düzeltmeleri ve Teknik İyileştirmeler](#4-hata-düzeltmeleri-ve-teknik-iyileştirmeler)
5. [Güvenlik Çalışmaları](#5-güvenlik-çalışmaları)
6. [Veri ve Yedekleme İşlemleri](#6-veri-ve-yedekleme-işlemleri)
7. [Sonuç](#7-sonuç)

---

## 1. Proje Bilgileri

**Düzce Üniversitesi Araç Takip Paneli**, üniversitenin kendi mülkiyetindeki resmi araç filosunun, görevli şoför kadrosunun, resmi görevlendirme süreçlerinin, periyodik bakım/onarım faaliyetlerinin, araç belgelerinin (fenni muayene, trafik sigortası, kasko), lastik envanterinin, personel araç taleplerinin, geçici yetki devri (vekâlet) işlemlerinin ve resmi denetim raporlarının dijital ortamda yönetilmesi amacıyla geliştirilmiş kurumsal bir iç yönetim sistemidir. Sistem ticari bir kiralama platformu olmayıp tamamen üniversite içi resmi taşıt görevlendirme ve filo takip süreçlerini yürütmektedir.

### 1.1 Teknoloji Yığını ve Mimari Özeti

| Bileşen Katmanı | Kullanılan Teknoloji / Kütüphane | Sürüm / Detay |
|---|---|---|
| **Çekirdek Platform** | .NET (C#) / ASP.NET Core MVC | `net9.0` (`Nullable: enable`, `ImplicitUsings: enable`) |
| **Veritabanı & ORM** | MS SQL Server & Entity Framework Core (Code-First) | `Microsoft.EntityFrameworkCore.SqlServer` (9.0.*) |
| **Kimlik Doğrulama & Yetki** | ASP.NET Core Identity & Rol Tabanlı Yetkilendirme (RBAC) | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (9.0.*), 5 Kurumsal Rol + Dinamik Vekâlet (`IClaimsTransformation`) |
| **Resmi PDF Evrak Motoru** | QuestPDF (Community Lisansı) | `QuestPDF` (2026.8.0) — 8 ayrı resmi belge ve rapor şablonu |
| **Oturum ve Şifreleme** | ASP.NET Core DataProtection | `App_Data/DataProtection-Keys` dosya sistemi anahtar kalıcılığı ve Stateless SVG Captcha şifrelemesi |
| **İstek Hız Sınırlandırma** | ASP.NET Core Built-in Rate Limiting | `System.Threading.RateLimiting` (Sliding Window & Fixed Window algoritmaları) |
| **Arka Plan Görevleri** | ASP.NET Core Hosted Service (`BackgroundService`) | `VehicleDocumentExpirationWorker` (24 saatlik periyodik belge geçerlilik denetimi) |
| **E-Posta Altyapısı** | Yerleşik SMTP & Konsol Simülasyon Servisi | `System.Net.Mail.SmtpClient` (`SmtpEmailService` / `ConsoleEmailService`) + `EmailTemplateBuilder` |
| **Arayüz (Frontend)** | Razor Views, Bootstrap, Inspinia Admin Tema, jQuery | Masaüstü yönetim paneli (`_AdminLayout.cshtml`) ve mobil öncelikli şoför portalı (`_SurucuLayout.cshtml`), Select2, Toastr, Chart.js, Web Audio API |
| **Veritabanı Şema Evrimi** | EF Core Migrations | Toplam **27 adet** sıralı migration adımı ve 18 adet Fluent API (`IEntityTypeConfiguration<T>`) sınıfı |

### 1.2 Genel Mimari İstatistikler

- ✅ **Toplam Migration Sayısı:** 27 (`20260831133104_InitialCreate` – `20261120111421_AddAssignmentDocumentsTable`)
- ✅ **Toplam Veritabanı Varlığı (Entity):** 19 (`BaseAuditableEntity` taban sınıfı + 18 tablo varlığı)
- ✅ **Toplam Enum Tanımı:** 12 (`AssignmentStatus`, `AuditAction`, `FaultReportStatus`, `FuelType`, `TireSeason`, `TireStatus`, `VehicleDocumentStatus`, `VehicleDocumentType`, `VehicleRequestStatus`, `VehicleRequestType`, `VehicleStatus`, `VehicleType` ve `EnumExtensions`)
- ✅ **Toplam MVC Controller:** 15 (`Account`, `Assignment`, `AuditLog`, `Availability`, `Driver`, `FaultReport`, `Home`, `Maintenance`, `Report`, `RoleDelegation`, `Sef`, `Surucu`, `SystemDefinition`, `Vehicle`, `VehicleRequest`)
- ✅ **Toplam Servis & PDF Doküman Sınıfı:** 14 Servis Arayüzü (`Interfaces`), 15 Servis Uygulaması, 8 QuestPDF Doküman Sınıfı, 1 Arka Plan Servisi (`BackgroundService`)
- ✅ **Toplam Razor View:** 55 `.cshtml` görünüm dosyası

---

## 2. Geliştirme Aşamaları (Kronolojik Migration Geçmişi)

Projenin veri modeli ve fonksiyonel kapsamı, Code-First yaklaşımıyla toplam **27 migration** adımı üzerinden evrilmiştir. Her bir migration adımında yapılan şema ve veri dönüşümleri aşağıda tarih sırasıyla listelenmiştir:

| Sıra | Migration Kimliği | Tarih | Kapsam ve Eklenen / Değişen Yapılar |
|---|---|---|---|
| **1** | `20260831133104_InitialCreate` | 31.08.2026 | ✅ ASP.NET Core Identity tabloları (`AspNetRoles`, `AspNetUsers`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`) oluşturuldu.<br>✅ `ApplicationUser` üzerine `FullName`, `IsActive`, `CreatedAt` alanları eklendi.<br>✅ Çekirdek iş tabloları (`Vehicles`, `Drivers`, `Assignments`, `Maintenances`, `AuditLogs`) oluşturuldu.<br>✅ `Vehicles.PlateNumber` ve `Drivers.UserId` üzerine tekil (unique) indeksler; `Assignments` üzerine `(VehicleId, PlannedStartTime, PlannedEndTime)` ve `(DriverId, PlannedStartTime, PlannedEndTime)` çakışma kontrolü bileşik indeksleri tanımlandı.<br>✅ İlişkisel veri kaybını önlemek üzere tüm yabancı anahtarlarda `ReferentialAction.Restrict` silme davranışı uygulandı. |
| **2** | `20260902073029_AddAssignmentSelfReportingAndApprovalColumns` | 02.09.2026 | ✅ Şoförlerin saha/gece görev başlangıç ve bitişlerini bildirmesi ve Ulaşım Şefinin onaylaması (3 kademeli onay akışı) için `Assignments` tablosuna `StartReportedBySelf` (`bit`), `StartApprovedByUserId` (`nvarchar(450)`), `EndReportedBySelf` (`bit`) ve `EndApprovedByUserId` (`nvarchar(450)`) kolonları ile `AspNetUsers` yabancı anahtarları eklendi. |
| **3** | `20260902133351_AddProfilePictureToApplicationUser` | 02.09.2026 | ✅ Kullanıcıların özel profil fotoğrafı yükleyebilmesi için `AspNetUsers` tablosuna `ProfilePictureUrl` (`nvarchar(500)`, nullable) kolonu eklendi. |
| **4** | `20260903064727_AddVehicleRequest` | 03.09.2026 | ✅ Üniversite personelinin sisteme giriş yapmadan araç talep edebilmesi için `VehicleRequests` tablosu (`RequesterFullName`, `RequesterEmail`, `RequesterPhone`, `Purpose`, `Status`, `FulfilledAssignmentId`, `FulfilledByUserId`, `FulfilledAt`, `RejectionReason`) ve performans indeksleri oluşturuldu. |
| **5** | `20260903074226_MakeAuditLogUserIdNullable` | 03.09.2026 | ✅ Anonim araç talepleri ve arka plan servisi (`VehicleDocumentExpirationWorker`) işlemlerinin denetim günlüğüne yabancı anahtar hatası vermeden kaydedilebilmesi için `AuditLogs.ChangedByUserId` kolonu `nullable` yapıldı. |
| **6** | `20260903112632_AddRequestTypeToVehicleRequest` | 03.09.2026 | ✅ `VehicleRequests` tablosuna görev talebi türünü belirten `RequestType` (`nvarchar(max)`, varsayılan `"OfficialTrip"`) alanı eklendi. |
| **7** | `20260904084941_AddVehicleInspectionInsuranceAndTireFields` | 04.09.2026 | ✅ `Vehicles` tablosuna fenni muayene bitiş tarihi (`InspectionExpiryDate`), trafik sigortası bitiş tarihi (`InsuranceExpiryDate`), lastik markası (`TireBrand`), lastik mevsimi (`TireSeason`), lastik üretim yılı (`TireYear`) ve trafik cezası/şef notları (`FineNotes`, `nvarchar(1000)`) kolonları eklendi. |
| **8** | `20260907064925_AddAccommodationAndPerDiemToAssignment` | 07.09.2026 | ✅ Görevlendirmelerde konaklama ve harcırah takibi için `Assignments` tablosuna `AccommodationNights` (`int`), `AccommodationCost` (`decimal(18,2)`), `PerDiemDays` (`int`), `PerDiemAmount` (`decimal(18,2)`) ve `ExpenseNotes` (`nvarchar(500)`) kolonları eklendi. |
| **9** | `20260907072457_AddInspectionAndInsuranceStartDatesToVehicle` | 07.09.2026 | ✅ `Vehicles` tablosuna fenni muayene başlangıç (`InspectionStartDate`) ve trafik sigortası başlangıç (`InsuranceStartDate`) tarih kolonları eklendi. |
| **10** | `20260907081212_AddSignedDocumentFieldsToAssignment` | 07.09.2026 | ✅ Islak imzalı görev emirlerinin taranmış kopyalarını saklamak üzere `Assignments` tablosuna `SignedDocumentPath` (`nvarchar(500)`), `SignedDocumentOriginalFileName` (`nvarchar(255)`), `SignedDocumentUploadedAt` (`datetime2`) ve `SignedDocumentUploadedByUserId` (`nvarchar(450)`) kolonları eklendi. |
| **11** | `20260907102316_AddHourlyRateAndDriverFeeToAssignment` | 07.09.2026 | ✅ Görev bitiş onayında fiili çalışma saati ve görev ücreti hesaplaması için `Assignments` tablosuna `HourlyRate` (`decimal(18,2)`), `CalculatedDutyHours` (`decimal(18,2)`) ve `TotalDriverFee` (`decimal(18,2)`) kolonları eklendi. |
| **12** | `20260907133605_AddDepartmentToVehicleRequest` | 07.09.2026 | ✅ `VehicleRequests` tablosuna talep eden personelin fakülte/daire başkanlığı bilgisini tutan `Department` (`nvarchar(150)`) kolonu eklendi. |
| **13** | `20260908065145_UpdateVehicleTypeAndTireSeasonToString` | 08.09.2026 | ✅ `Vehicles.VehicleType` ve `Vehicles.TireSeason` kolonları `nvarchar(50)` tipine dönüştürüldü ve mevcut sayısal/İngilizce enum kayıtları SQL dönüşüm sorgularıyla Türkçe standart değerlere (`Yazlık`, `Kışlık`, `4 Mevsim`, `Sedan`, `Minibüs`, `Panelvan`, `Kamyonet`, `Otobüs`, `Diğer`) normalize edildi. |
| **14** | `20260908071220_UpdateFuelTypeToString` | 08.09.2026 | ✅ `Vehicles.FuelType` kolonu `nvarchar(50)` tipine dönüştürüldü ve mevcut kayıtlar Türkçe metinlere (`Benzin`, `Dizel`, `LPG`, `Elektrik`, `Hibrit`) dönüştürüldü. |
| **15** | `20260909110745_AddPerDiemHybridFieldsToAssignment` | 09.09.2026 | ✅ Saatlik ve günlük hibrit harcırah hesaplama modeli için `Assignments` tablosuna `PerDiemType` (`nvarchar(20)`, varsayılan `"Hourly"`), `PerDiemHours` (`decimal(18,2)`) ve `PerDiemRate` (`decimal(18,2)`) kolonları eklendi. |
| **16** | `20261020213038_AddPhase3AssignmentChecksAndFaultReports` | 20.10.2026 | ✅ **Faz 3:** `Assignments` tablosuna şoför araç kontrol tikleri (`CheckOil`, `CheckWater`, `CheckFuel`, `CheckHydraulic`, `CheckTires`, `ChecksCompletedAt`) ve acil durum güzergah dışı sevk alanları (`OffRouteDispatcherName`, `OffRouteDispatcherTitle`, `OffRouteReason`, `OffRouteReportedAt`, `OffRouteReportedByUserId`) eklendi.<br>✅ Araç arıza bildirimleri için `VehicleFaultReports` tablosu oluşturuldu. |
| **17** | `20261020233958_AddMultiDriverAndDestinationFields` | 20.10.2026 | ✅ **Faz 2-A:** Çoklu şoför, kurum dışı şoför ve şoförsüz görevlendirme desteği için `Assignments.DriverId` nullable yapıldı; `Destination`, `Route`, `IsDriverless`, `IsExternalDriver`, `ExternalDriverFullName`, `ExternalDriverDepartment` kolonları eklendi.<br>✅ `AssignmentDrivers` ara tablosu ve `[IsDeleted] = 0` filtreli tekil indeksi oluşturuldu; mevcut tüm görevlerin şoförleri SQL sorgusuyla veri kaybı olmadan `AssignmentDrivers` tablosuna `IsPrimary = 1` olarak aktarıldı. |
| **18** | `20261024174753_AddAssignmentRouteChange` | 24.10.2026 | ✅ **Faz 2-B:** Sefer esnasında şoförlerin yaptığı güzergah ve hedef değişikliklerini tarihçesiyle kaydetmek ve şef onayına sunmak üzere `AssignmentRouteChanges` tablosu (`OldRoute`, `NewRoute`, `OldDestination`, `NewDestination`, `Reason`, `ChangedByUserId`, `ChangedByFullName`, `IsReviewedByAdmin`, `ReviewedAt`, `ReviewedByUserId`) oluşturuldu. |
| **19** | `20261024184022_AddDriverLicense` | 24.10.2026 | ✅ **Faz 4:** Bir şoförün birden fazla ehliyet sınıfına ve her sınıf için ayrı geçerlilik tarihine sahip olabilmesi için `DriverLicenses` tablosu oluşturuldu. Mevcut şoförlerin ehliyet verileri SQL ile yeni tabloya otomatik taşındı. |
| **20** | `20261024191639_AddVehicleDocument` | 24.10.2026 | ✅ **Faz 5-A:** Araçların Fenni Muayene (`Inspection`), Trafik Sigortası (`Insurance`) ve `Kasko` poliçe geçmişlerini maliyet, şirket ve poliçe numaralarıyla takip etmek üzere `VehicleDocuments` tablosu oluşturuldu. Mevcut araçların muayene ve sigorta tarihleri SQL ile yeni tabloya otomatik aktarıldı. |
| **21** | `20261024201613_AddVehicleTire` | 24.10.2026 | ✅ **Faz 5-B:** Araç lastik geçmişi ve değişim eşikleri takibi için `VehicleTires` tablosu (`Brand`, `Size`, `DotDate`, `ProductionYear`, `Season`, `InstalledDate`, `InstalledKm`, `RecommendedReplaceKm`, `RecommendedReplaceDate`, `Cost`, `Notes`) oluşturuldu ve mevcut araçlardaki lastik bilgileri SQL ile yeni tabloya taşındı. |
| **22** | `20261024210401_AddMaintenanceCost` | 24.10.2026 | ✅ **Faz 7-A:** Bakım ve onarım işlemlerinin maliyetlerini raporlayabilmek amacıyla `Maintenances` tablosuna `Cost` (`decimal(18,2)`, nullable) kolonu eklendi. |
| **23** | `20261024212807_AddFaz6RequestOptionsAndFields` | 24.10.2026 | ✅ **Faz 6:** Araç talep formundaki birim ve araç türü seçeneklerinin yönetim panelinden dinamik yönetilebilmesi için `RequestDepartments` ve `RequestVehicleTypes` tabloları oluşturuldu. `VehicleRequests` tablosuna `RequestedVehicleType`, `PassengerCount`, `RequestedStartTime`, `RequestedEndTime`, `ContactUserId`, `ContactFullName`, `ContactPhone`, `ContactEmail` kolonları eklendi. |
| **24** | `20261025004604_AddRoleDelegationAndAuditOnBehalf` | 25.10.2026 | ✅ **Faz 1-B:** Geçici yetki devri (vekâlet) mekanizması için `RoleDelegations` tablosu ve `IX_RoleDelegations_ActiveCheck` bileşik indeksi oluşturuldu. Vekâleten yapılan işlemlerin denetim izi için `AuditLogs` tablosuna `OnBehalfOfUserId` ve `DelegationId` kolonları eklendi. |
| **25** | `20261026071023_AddSystemDefinitions` | 26.10.2026 | ✅ Sistem genelindeki dinamik seçenek listeleri (`TireSeason`, `FuelType`, `MaintenanceType`, `VehicleType`, `LicenseClass`) için `SystemDefinitions` tablosu ve `[IsDeleted] = 0` filtreli `(CategoryKey, NormalizedName)` benzersiz indeksi oluşturuldu. |
| **26** | `20261030084944_AddRouteToVehicleRequest` | 30.10.2026 | ✅ `VehicleRequests` tablosuna talep edilen görev güzergahını tutan `Route` (`nvarchar(500)`, nullable) kolonu eklendi. |
| **27** | `20261120111421_AddAssignmentDocumentsTable` | 20.11.2026 | ✅ Görev başına birden fazla ve şoför bazlı imzalı görev belgesi yüklenebilmesi için `AssignmentDocuments` tablosu (`AssignmentId`, `DriverId`, `FilePath`, `OriginalFileName`, `FileSizeBytes`, `Description`, `UploadedAt`, `UploadedByUserId`) oluşturuldu. Mevcut `Assignments` tablosundaki tekil imzalı belgeler SQL ile yeni tabloya eksiksiz taşındı. |

---

## 3. Modül Modül Yapılan Çalışmalar

### 3.1 Kimlik Doğrulama, Rol Yapısı (RBAC) ve Kullanıcı Profili Modülü
- **Geliştirilen Özellikler:**
  - ✅ Standart Identity şablonları yerine Düzce Üniversitesi kurumsal kimliğine uygun özel giriş (`Login`), şifremi unuttum (`ForgotPassword`), şifre sıfırlama (`ResetPassword`), yetkisiz erişim (`AccessDenied`) ve kullanıcı profili (`Profile`) ekranları geliştirildi.
  - ✅ Harici servis (Google reCAPTCHA vb.) bağımlılığı bulunmayan, sunucu tarafında `CaptchaService` ile dinamik üretilen 5 karakterli **Stateless SVG Captcha** sistemi entegre edildi.
  - ✅ **5 Kademeli Kurumsal Rol Mimarisi (`AppRoles.cs`)** kurgulandı:
    1. `Admin` (Sistem Yöneticisi): Tüm modüllerde ve üst düzey yönetici hesaplarında tam yetkili.
    2. `DaireBaskani` (Daire Başkanı): Tüm operasyonel ekranlarda, denetim kayıtlarında, sistem tanımlarında ve talep seçeneklerinde tam yetkili; kullanıcı yönetiminde yalnızca `Kullanici` (Şef) ve `Driver` (Şoför) hesaplarını yönetebilir.
    3. `GenelSekreter` (Genel Sekreter): Tüm ekranları, kullanıcı listesini, raporları, PDF önizlemelerini ve denetim kayıtlarını inceleyebilen **Salt Okunur (Read-Only)** üst denetim rolü.
    4. `Kullanici` (Ulaşım / Birim Şefi): Araç, şoför, görev, bakım, arıza ve araç taleplerini yöneten operasyonel birim yöneticisi.
    5. `Driver` (Şoför): Mobil şoför portalı üzerinden yalnızca kendisine atanmış görevleri görüntüleyen, başlangıç/bitiş KM, araç kontrol tikleri, güzergah değişikliği ve arıza bildirimi yapan saha kullanıcısı.
  - ✅ **Dinamik Baş Harf (Rozet) Avatar Sistemi (`AvatarHelper.cs`):** Özel profil fotoğrafı bulunmayan kullanıcılar için ad ve soyadın baş harflerinden (örn. `"Ahmet Yılmaz"` → `AY`) kurumsal renkli dairesel rozet avatar üretildi.
  - ✅ **Bağımsız Profil Güncelleme:** Kullanıcıların şifre değiştirmek zorunda kalmadan yalnızca ad-soyad, telefon ve profil fotoğrafını güncelleyebilmesi, yeni fotoğraf seçildiğinde canlı önizleme yapılması ve mevcut fotoğrafın kaldırılabilmesi (`RemoveProfilePhoto`) sağlandı.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `ApplicationUser` (`AspNetUsers`)
  - *Controller & Servis:* `AccountController`, `CaptchaService`, `SmtpEmailService`, `ConsoleEmailService`
  - *Views:* `Views/Account/Login.cshtml`, `ForgotPassword.cshtml`, `ForgotPasswordConfirmation.cshtml`, `ResetPassword.cshtml`, `Profile.cshtml`, `AccessDenied.cshtml`
- **Önemli İş Kuralları:**
  - ✅ Sisteme dışarıdan kayıt olma (`/Account/Register`) özelliği kesin olarak kapalıdır; hesaplar yalnızca yetkili yöneticiler tarafından açılır.
  - ✅ Giriş sonrasında `ReturnUrl` parametresi kullanıcının rolüyle uyumluysa yönlendirilir (Şoför yalnızca `/surucu` veya `/account` adreslerine, yöneticiler ise yönetim paneline yönlendirilir).

### 3.2 Yönetici Dashboard, Birleşik Bildirim Merkezi ve Aylık Operasyon Takvimi Modülü
- **Geliştirilen Özellikler:**
  - ✅ **KPI Özet Kartları ve Filo Grafiği:** Toplam araç ve 5 durum kırılımı (`Boşta`, `Görevde`, `Bakımda`, `Belgesi Geçersiz`, `Hizmet Dışı`), aktif görevler, bugünkü planlı görevler, devam eden bakımlar ve aktif şoför sayılarını gösteren KPI kartları ile Chart.js tabanlı filo durumu grafiği geliştirildi.
  - ✅ **Hızlı Plaka Arama (`/Home/SearchPlate`):** Boşluklu veya boşluksuz plaka aramasıyla aracın anlık durumu, güncel KM'si, muayene ve sigorta kalan günleri, güncel lastik künyesi, ceza/şef notları ve aracı en son kullanan şoförlerin geçmişi AJAX ile tek kartta sunuldu.
  - ✅ **Aylık Operasyon Takvimi (`/Home/CalendarData` - Faz 9):** Harici takvim kütüphanesi kullanılmadan 7 sütunlu (42 günlük) aylık ızgara takvim geliştirildi. Her gün üzerinde aktif araç doluluğu, bakımdaki araçlar, süresi dolan belgeler ve ehliyetler işaretlendi; güne tıklandığında açılan `#dashboardCalendarDayModal` ile o günün tüm görev, bakım, belge ve ehliyet detayları listelendi.
  - ✅ **Birleşik Bildirim Merkezi (`_AdminLayout.cshtml`):** Üst barda 5 ayrı uyarı kategorisini (`Belge`, `Talep`, `Arıza`, `Güzergah`, `Ehliyet`) tek bir açılır menüde ve kategori filtre çiplerinde (`notify-chip`) birleştiren bildirim merkezi inşa edildi.
  - ✅ **Sesli ve Görsel Uyarı (Web Audio API & Toast):** Sisteme girişte veya 30 saniyelik periyodik kontrollerde yeni bildirim tespit edildiğinde harici ses dosyası kullanmadan Web Audio API ile kurumsal bildirim sesi ve Toast uyarısı üretildi; `sessionStorage` imza kontrolü ile aynı bildirimin tekrar tekrar çıkması engellendi.
- **İlgili Entity ve Ekranlar:**
  - *Controller & Servis:* `HomeController`, `DashboardService`
  - *ViewModels:* `DashboardViewModel`, `DashboardPlateSearchResultViewModel`, `DashboardCalendarViewModels`
  - *Views:* `Views/Home/Index.cshtml`, `Views/Shared/_AdminLayout.cshtml`
- **Önemli İş Kuralları:**
  - ✅ Takvim verileri çekilirken `Assignments`, `Maintenances`, `Vehicles` (belge ve lastikleriyle) ve `DriverLicenses` tabloları ay bazında tekil sorgularla çekilerek N+1 sorgu problemi önlenmiştir.
  - ✅ Belge ve ehliyet uyarı listelerinde en acil kayıtlar (süresi en çok geçmiş veya dolmasına en az gün kalmış olanlar) en üstte sıralanır.

### 3.3 Araç Envanteri Yönetimi Modülü
- **Geliştirilen Özellikler:**
  - ✅ Araç filosunun plaka, marka, model, model yılı, renk, araç türü, yakıt tipi, güncel kilometre, fenni muayene/sigorta başlangıç-bitiş tarihleri, lastik bilgileri ve ceza/şef notlarıyla yönetimi sağlandı.
  - ✅ Durum, araç türü ve plaka/marka/model aramasına göre sunucu taraflı sayfalamalı (`GetVehiclesPagedAsync`) liste ekranı ve Select2 filtreleri oluşturuldu.
  - ✅ Araç detay ekranında (`/Vehicle/Details/{id}`) araç künyesi, güncel lastik kartı, son 5 görevlendirme ve tüm bakım geçmişi bir arada gösterildi.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `Vehicle` (`Vehicles`)
  - *Controller & Servis:* `VehicleController`, `VehicleService`, `VehicleDocumentExpirationWorker`
  - *Views:* `Views/Vehicle/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Tarihsel Plaka Tekilliği:** `IgnoreQueryFilters()` ile silinmiş (soft-delete) araçlar dahil plaka kontrolü yapılır; daha önce kullanılmış bir plaka tekrar açılamaz.
  - ✅ **Salt Okunur KM ve Durum:** Araç düzenleme formunda `CurrentKm` ve `Status` alanları elle değiştirilemez; yalnızca görev, bakım ve belge süreçleriyle otomatik güncellenir.
  - ✅ **Tek Kaynak İlkesi:** Yeni araç eklendiğinde girilen ilk muayene, ilk sigorta ve ilk lastik bilgileri otomatik olarak `VehicleDocuments` ve `VehicleTires` tablolarına da ilk kayıt olarak işlenir.
  - ✅ **Silme Koruması:** Aktif/planlı görevi veya devam eden bakımı olan araçlar silinemez.

### 3.4 Araç Belgeleri (Muayene, Trafik Sigortası, Kasko) ve Lastik Yönetimi Modülü
- **Geliştirilen Özellikler:**
  - ✅ **4 Sekmeli Araç Belgeleri Paneli (`/Vehicle/Documents` - Faz 5-A & Faz 5-B):** Seçili bir araç veya `"Tümü (Tüm Araçlar)"` filtresi üzerinden **Fenni Muayene**, **Trafik Sigortası**, **Kasko** ve **Lastik** kayıtlarının tüm geçmişiyle yönetildiği merkezi ekran geliştirildi.
  - ✅ **Belge Takibi:** Her belge için başlangıç tarihi, bitiş tarihi, ödenen tutar (`Cost`), firma/istasyon adı (`CompanyName`), poliçe/istasyon numarası (`PolicyOrStationNumber`) ve notlar kaydedilir. En güncel kayıt `"GÜNCEL GEÇERLİ"` rozetiyle vurgulanır.
  - ✅ **Otomatik Belge Geçerlilik Takip Servisi (`VehicleDocumentExpirationWorker`):** Uygulama açılışında ve her 24 saatte bir çalışarak fenni muayene veya trafik sigortası süresi dolmuş araçları (seyirdeki `OnDuty` araçlar hariç) otomatik olarak `DocumentExpired` (`Belgesi Geçersiz`) durumuna çeker; belgeleri yenilenen araçları ise tekrar `Idle` (veya açık bakımı varsa `Maintenance`) durumuna döndürür ve her geçişi `AuditLog`'a yazar.
  - ✅ **Lastik Envanter ve Eşikleri:** Lastik markası, ebat (`Size`), DOT üretim tarihi (`52/23` formatından 4 haneli `ProductionYear` çıkarımı), mevsim, takılma tarihi, takılma KM'si, hedef değişim KM'si, hedef değişim tarihi ve maliyet alanları yönetilir.
  - ✅ **Dinamik Kullanım KM'si:** Lastiğin yaptığı mesafe sabit kaydedilmez; `Vehicle.CurrentKm - InstalledKm` formülüyle anlık hesaplanır (eski taşınan kayıtlarda `InstalledKm` boş ise `"Bilinmiyor"` gösterilir).
  - ✅ **Lastik Kullanım Raporu PDF (`/Vehicle/ExportTiresPdf`):** Tek araç (tüm geçmişiyle) veya tüm filo (güncel lastikleriyle) için QuestPDF tabanlı A4 yatay rapor ve önizleme üretilir.
- **İlgili Entity ve Ekranlar:**
  - *Entities:* `VehicleDocument` (`VehicleDocuments`), `VehicleTire` (`VehicleTires`)
  - *Controller & Servis:* `VehicleController`, `VehicleService`, `VehicleTiresReportPdfDocument`
  - *Views:* `Views/Vehicle/Documents.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Geriye Dönük Senkronizasyon:** Belge veya lastik eklendiğinde, güncellendiğinde ya da silindiğinde `SyncVehicleLegacyDocumentColumnsAsync` ve `SyncVehicleLegacyTireColumnsAsync` metotları çalışarak `Vehicles` tablosundaki özet kolonları ve aracın `DocumentExpired` durumunu otomatik senkronize eder.
  - ✅ **Son Kayıt Silme Koruması:** Bir aracın son aktif Muayene, son aktif Trafik Sigortası veya son aktif Lastik kaydı silinemez (Kasko opsiyonel olduğu için silinebilir).
  - ✅ **Uyarı Ayrımı:** Kasko ve Lastik değişim uyarıları (30 gün veya 1000 KM eşiği) bildirim merkezinde uyarı üretir ancak aracın operasyonel durumunu `DocumentExpired` yapmaz.

### 3.5 Şoför Yönetimi ve Çoklu Ehliyet Takibi Modülü
- **Geliştirilen Özellikler:**
  - ✅ **Atomik Şoför Hesabı Oluşturma:** Yeni şoför kaydedilirken `ApplicationUser` (Identity hesabı), `Driver` profili ve çoklu `DriverLicense` satırları tek bir veritabanı transaction'ı içinde oluşturulur.
  - ✅ **Çoklu Ehliyet Desteği (Faz 4):** Şoför ekleme ve düzenleme ekranlarında `LicenseNumber` tekil tutulurken, şoförün sahip olduğu birden fazla ehliyet sınıfı (`B`, `C`, `D`, `E`, `CE` vb.) ve her birinin ayrı geçerlilik tarihi dinamik satırlarla (`+ Ehliyet Ekle`) yönetilir.
  - ✅ **Hızlı Aktif/Pasif (`ToggleStatus`) ve Yönetici Şifre Sıfırlama (`ResetPassword`):** Şoför tek tıkla pasife alınabilir veya detay sayfasındaki modal üzerinden şifresi yönetici tarafından sıfırlanabilir (kilitlenmiş hesapların kilidi de otomatik açılır).
  - ✅ **Ehliyet Geçerlilik Uyarıları (`/Driver/ExpiringLicenseSummary`):** Süresi dolmuş veya 30 gün içinde dolacak ehliyetler hem yöneticilere (Dashboard, bildirim merkezi, sidebar rozeti) hem de şoförün kendi mobil ekranına uyarı olarak yansıtılır.
- **İlgili Entity ve Ekranlar:**
  - *Entities:* `Driver` (`Drivers`), `DriverLicense` (`DriverLicenses`), `ApplicationUser`
  - *Controller & Servis:* `DriverController`, `DriverService`
  - *Views:* `Views/Driver/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Tarihsel Ehliyet No Tekilliği:** `IgnoreQueryFilters()` ile silinmiş şoförler dahil aynı ehliyet numarasının ikinci kez kullanılması engellenir.
  - ✅ **Birim (`Department`) Düzenleme Kısıtı:** Şoförün bağlı olduğu birim bilgisi hem arayüzde hem sunucu katmanında (`isAdmin` parametresiyle) yalnızca `Admin` ve `DaireBaskani` tarafından değiştirilebilir; Ulaşım Şefi (`Kullanici`) birim alanını değiştiremez.
  - ✅ **Aktif Görev ve Vekâlet Koruması:** Planlı veya devam eden görevi olan şoför pasife alınamaz ve silinemez. Şoför pasife alındığında veya silindiğinde bağlı `ApplicationUser.IsActive = false` yapılır ve aktif vekâletleri otomatik iptal edilir.

### 3.6 Görevlendirme, 3 Kademeli Onay, Süre Uzatma ve İmzalı Evrak Arşivi Modülü
- **Geliştirilen Özellikler:**
  - ✅ **3 Farklı Şoför Modu (Faz 2-A):** Görev oluşturma ve düzenleme formlarında birbirini kesin olarak dışlayan 3 mod desteklenir:
    1. *Kurum Şoförlü:* 1 asil + istenirse birden fazla yedek/ek kurum şoförü (`AssignmentDrivers`).
    2. *Kurum Dışı Şoför (`IsExternalDriver`):* Dış görevli şoför adı-soyadı ve kurumu.
    3. *Şoförsüz Görevlendirme (`IsDriverless`):* Aracın doğrudan görevli personele tahsisi.
  - ✅ **Görev Yeri ve Güzergah:** Görevin konusu (`Purpose`), gidilecek yer (`Destination`) ve güzergah (`Route`) ayrı alanlar olarak yönetilir.
  - ✅ **Canlı Müsaitlik Servisi (`AvailabilityController`):** Tarih aralığı seçildiğinde `/Availability/GetOptions`, `CheckVehicleAvailability` ve `CheckDriverAvailability` AJAX uç noktalarıyla araç ve şoförlerin görev çakışması, bakım durumu, belge geçerliliği, ehliyet süresi ve aktif vekâlet durumu anlık kontrol edilir.
  - ✅ **3 Kademeli Onay Akışı ve Doğrudan Başlat/Bitir:**
    - Şoför mobil panelden başlangıç/bitiş bildirdiğinde görev durumu hemen değişmez; yönetim panelinde turuncu `"Onay Bekliyor"` rozetiyle listelenir.
    - Şef/Admin `ApproveStart` ve `ApproveEnd` işlemleriyle bildirimi onaylar.
    - Şoförsüz, dış şoförlü veya şoförün bildirim yapamadığı durumlarda Şef/Admin `StartDirect` ve `EndDirect` ile görevi doğrudan başlatıp bitirebilir.
  - ✅ **Şef KM Düzeltmesi ve 200 KM / 1000 KM Eşik Kontrolleri (`KmValidationService` - Faz 2-B):**
    - Şef bitiş onay modalında şoförün bildirdiği başlangıç (`ConfirmedStartKm`) ve bitiş (`ConfirmedEndKm`) kilometrelerini revize edebilir.
    - Yapılan sefer mesafesi 200 KM'yi (`TripDistanceThreshold`) aştığında kırmızı ünlem ikonlu dikkat uyarısı gösterilir; 1000 KM (`KmDiscrepancyThreshold`) üzerindeki farklarda açık onay (`confirmKmDifference`) istenir.
  - ✅ **Hibrit Görev Ücreti, Konaklama, Harcırah ve Sonradan Maliyet Revizyonu:**
    - Bitiş onayında Saatlik (`Hourly`) veya Günlük (`Daily`) harcırah/görev ücreti hesaplaması, konaklama gün sayısı (`AccommodationNights`) ve konaklama bedeli (`AccommodationCost`) kaydedilir.
    - Görev tamamlandıktan sonra gelen faturalar için `UpdateFinancialDetails` aksiyonu ile KM ve süreye dokunulmadan, zorunlu revizyon gerekçesiyle (`[Revizyon dd.MM.yyyy HH:mm]: ...`) maliyet alanları güncellenebilir.
  - ✅ **Seyir Halinde Süre Uzatma (`ExtendDuration`):** Planlı veya devam eden görevlerin bitiş zamanı, yeni zaman aralığında araç ve şoför çakışması kontrol edilerek ve zorunlu gerekçe `ExpenseNotes` alanına `[Süre Uzatma - dd.MM.yyyy HH:mm]: ...` şeklinde damgalanarak uzatılabilir.
  - ✅ **Güzergah Dışı Sevk (`SaveOffRouteDispatch`) ve Şoför Güzergah Değişikliği Onayı (`ApproveRouteChange`):** Acil durumlarda aracı güzergah dışına sevk eden yetkilinin adı, unvanı ve gerekçesi sisteme işlenebilir; şoförün yoldayken bildirdiği güzergah değişiklikleri Şef tarafından incelenip onaylandığında resmi görev emrinin sevk kutusuna aktarılır.
  - ✅ **Resmi Taşıt Görev Emri PDF (`AssignmentOrderDocument` - Faz 3):** QuestPDF ile tek sayfa A4 dikey matbu formatta; üniversite logosu, sayı/tarih, görevlendiren/sevk eden imza kutuları, görev yeri, orijinal güzergah, güzergah dışı sevk kutusu, araç/şoför bilgileri, çıkış/giriş KM ve saatleri, 3 maddelik resmi yönerge ve 5'li araç kontrol kutuları (`[X]` / `[ ]`) + Paraf alanıyla üretilir. Hem **"Dolu"**, hem **"Boş Çıktı"** (`emptyValues=true`), hem de çoklu şoförde **şoföre özel** (`driverId`) PDF alınabilir.
  - ✅ **Çoklu İmzalı Evrak Yükleme ve Merkezi Evrak Arşivi (`/Assignment/SignedDocuments`):** Görev detayından şoför seçimi ve açıklamayla birden fazla imzalı/taranmış belge (`AssignmentDocuments`) yüklenebilir, önizlenebilir (`ViewSignedDocument`), indirilebilir (`DownloadSignedDocument`), yumuşak silinebilir (`DeleteSignedDocument`) ve merkezi arşiv sayfasından plaka, şoför, tarih ve metin aramasıyla takip edilebilir.
- **İlgili Entity ve Ekranlar:**
  - *Entities:* `Assignment`, `AssignmentDriver`, `AssignmentRouteChange`, `AssignmentDocument`
  - *Controllers & Servisler:* `AssignmentController`, `AvailabilityController`, `AssignmentService`, `KmValidationService`, `AssignmentOrderDocument`
  - *Views:* `Views/Assignment/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `SignedDocuments.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Çifte Rezervasyon ve Aktif Sefer Engeli:** Aynı saat aralığında başka bir planlı/aktif görevi olan araca veya şoföre görev atanamaz. Ayrıca aracın veya şoförün üzerinde hâlihazırda tamamlanmamış (`InProgress`) bir görev varsa ikinci bir görev başlatılamaz.
  - ✅ **Ehliyet Kapsama Kuralı:** Atanan her kurum şoförünün aktif ehliyetlerinden **en az birinin** son geçerlilik tarihi, görevin planlanan bitiş tarihini (`LicenseExpiryDate.Date >= PlannedEndTime.Date`) kapsamak zorundadır.
  - ✅ **Akıllı Araç KM Güncelleme Kuralı:** Görev bitişi onaylandığında `Vehicle.CurrentKm` yalnızca bu görev aracın **en son tamamlanan göreviyse** ve yeni bitiş KM'si aracın mevcut KM'sinden **büyükse** güncellenir; geçmiş tarihli bir görevin KM revizyonu aracın güncel KM'sini geriye düşürmez.

### 3.7 Şoför Mobil Portalı Modülü
- **Geliştirilen Özellikler:**
  - ✅ Şoförlerin %90 oranında akıllı telefondan kullanacağı dikkate alınarak özel `_SurucuLayout.cshtml` üzerinde tablo içermeyen, %100 dokunmatik kart (`card-custom`) ve en az 50px yüksekliğinde (`btn-touch`) buton tasarımına sahip mobil portal geliştirildi.
  - ✅ Görevler; **Aktif Sürüş (`ActiveAssignment`)**, **Onay Bekleyenler (`PendingApprovalAssignments`)**, **Bugünkü Planlı Görevler (`TodayPlannedAssignments`)**, **Yaklaşan Görevler (`UpcomingAssignments`)** ve **Geçmiş Görevler (`PastAssignments`)** olarak gruplandı.
  - ✅ **AJAX Operasyonları:** Sayfa yenilenmeden Başlangıç KM bildirme (`/Surucu/Baslat/{id}`), Bitiş KM + Teslim Notu + Araç Kontrolleri bildirme (`/Surucu/Bitir/{id}`), 5'li Araç Kontrol Tiklerini (Yağ, Su, Yakıt, Hidrolik, Lastik) kaydetme (`/Surucu/SaveVehicleChecks/{id}`), seferdeyken Güzergah ve Hedef değiştirme (`/Surucu/GuzergahDegistir/{id}`) ve Arıza Bildirimi açma (`/Surucu/ReportFault`) işlevleri sağlandı.
  - ✅ Şoförün kendi adına düzenlenmiş Taşıt Görev Emri PDF belgesini mobil modalda önizlemesi ve indirmesi (`/Surucu/GorevEmri/{id}`) ile kendi şifresini değiştirmesi (`/Surucu/ChangePassword`) sağlandı.
- **İlgili Entity ve Ekranlar:**
  - *Controller & Servis:* `SurucuController`, `DriverService`, `FaultReportService`
  - *Views:* `Views/Shared/_SurucuLayout.cshtml`, `Views/Surucu/Index.cshtml`, `Detay.cshtml`, `ChangePassword.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Yatay Yetki İzolasyonu (IDOR Koruması):** Şoför yalnızca `a.DriverId == driver.Id` veya `a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id)` koşulunu sağlayan kendi görevlerini görebilir, işlem yapabilir ve PDF indirebilir; başka şoförün görevine erişim `403 Forbid` ile reddedilir.
  - ✅ **Vekil Şoför Saha Kısıtı:** Aktif bir yetki devriyle geçici Ulaşım Şefi vekili olan şoförün mobil panelde üstte bilgi kartı ve `"Yönetim Paneline Geçiş Yap"` butonu gösterilir; vekâlet süresince saha görev başlatma, bitirme ve güzergah değiştirme uç noktaları sunucu tarafında engellenir.
  - ✅ **Gece Vardiyası / Kendi Kendine Görev Açma Yasağı:** Şoförlerin otoparktan araç seçip kendi kendilerine görev başlatma özelliği tamamen kaldırılmıştır; şoförler yalnızca yetkili şefin atadığı görevleri işleyebilir.

### 3.8 Bakım ve Onarım Yönetimi Modülü
- **Geliştirilen Özellikler:**
  - ✅ Araçların periyodik bakım, arıza onarım, lastik/rot-balans, muayene hazırlık gibi servis süreçlerinin başlangıç tarihi, bitiş tarihi, işlemi yapan servis/firma (`PerformedBy`), işlem maliyeti (`Cost`) ve açıklamasıyla yönetimi sağlandı.
  - ✅ Devam eden bakımların tek tıkla tamamlanması (`Complete`) ve idari hata durumlarında modal onaylı yumuşak silinmesi (`Delete`) geliştirildi.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `Maintenance` (`Maintenances`)
  - *Controller & Servis:* `MaintenanceController`, `MaintenanceService`
  - *Views:* `Views/Maintenance/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Details.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Durum Makinesi Entegrasyonu:** Bitiş tarihi boş (devam eden) bir bakım açıldığında aracın durumu otomatik `VehicleStatus.Maintenance` yapılır; bakım tamamlandığında veya açık bakım silindiğinde (aracın başka açık bakımı yoksa) durum otomatik `VehicleStatus.Idle` (`Boşta`) durumuna döndürülür.
  - ✅ **Çakışma Koruması:** Aktif görevdeki (`OnDuty`) bir araca açık bakım kaydı açılamaz; halihazırda açık bakımı olan bir araca ikinci bir açık bakım açılamaz ve bakımdaki araca görev atanamaz.

### 3.9 Araç Arıza Bildirimi Modülü
- **Geliştirilen Özellikler:**
  - ✅ Şoförlerin mobil panelden veya görev detayından araç kilometresi ve arıza açıklamasıyla bildirim oluşturması (`CreateFaultReportAsync`), oluşturulan bildirimlerin üst bildirim merkezinde ve sol menüde kırmızı sayaç rozetiyle (`#sidebarFaultBadge`) Ulaşım Şefliğine anlık düşmesi sağlandı.
  - ✅ Şef/Admin için durum (`Pending`, `Approved`, `Resolved`, `Rejected`) ve araç filtreli liste ekranı (`/FaultReport`) ile bildirim detay/inceleme ekranı (`/FaultReport/Detail/{id}`) geliştirildi.
  - ✅ Şefin arıza kaydındaki KM ve açıklamayı düzenleyerek inceleme/sevk notuyla onaylaması (`Approve`) veya durumunu güncellemesi (`UpdateStatus`) sağlandı.
  - ✅ **Resmi Araç Arıza Bildirim Formu PDF (`FaultReportDocument`):** QuestPDF ile hem kayıt verileriyle dolu (`/FaultReport/DownloadPdf/{id}`) hem de sahada elle doldurulmak üzere boş matbu form (`/FaultReport/DownloadBlankPdf`) olarak A4 dikey PDF üretimi ve önizlemesi yapıldı.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `VehicleFaultReport` (`VehicleFaultReports`)
  - *Controller & Servis:* `FaultReportController`, `FaultReportService`, `FaultReportDocument`
  - *Views:* `Views/FaultReport/Index.cshtml`, `Detail.cshtml`
- **Önemli İş Kuralları:**
  - ✅ Arıza bildirimi açılması veya onaylanması aracın `Vehicle.Status` değerini otomatik olarak `Maintenance` veya `OutOfService` yapmaz; araç durumu idari kararla (bakım kaydı açılarak) yönetilir.
  - ✅ Resmi PDF çıktısına kurum dışı veya EBYS üst veri damgası basılmaz; imza kutularında GUID yerine yalnızca gerçek yönetici/şoför adı soyadı yer alır.

### 3.10 Personel Araç Talebi ve Talep Seçenekleri Modülü
- **Geliştirilen Özellikler:**
  - ✅ **Girişsiz (Anonim) Kurumsal Talep Formu (`/AracTalebi` - Faz 6):** Üniversite personelinin hesap açmadan; Ad Soyad, `@duzce.edu.tr` kurumsal e-posta, cep telefonu, birim/fakülte (yönetici tanımlı açılır liste), istenen araç türü (yönetici tanımlı açılır liste), yolcu sayısı (`1-200`), başlangıç/bitiş tarih ve saati, gidilecek güzergah (`Route`) ve görev amacıyla başvuru yapabildiği ekran geliştirildi. Formda *"Yalnızca il içi görevlendirme yapılır."* uyarısı yer alır.
  - ✅ **Talep Yönetim Havuzu (`/VehicleRequest`):** Bekleyen, Karşılanan ve Reddedilen taleplerin sayaçlı sekmeler, birim, tarih ve arama filtreleriyle listelenmesi sağlandı.
  - ✅ **Talebi Göreve Dönüştürme (`/VehicleRequest/Fulfill/{id}`):** Şef/Admin talebi incelerken talep edilen tarih aralığına göre müsait araç ve şoförleri görür, koordinasyon için bir **İletişim Kişisi** (Ulaşım Şefi veya Şoför) seçer, varsa **Şeflik Notu** ekler ve tek tıkla resmi `Assignment` oluşturur.
  - ✅ **Kurumsal HTML E-Posta Bildirimleri (`EmailTemplateBuilder.cs`):** Talep karşılandığında yeşil onay temalı, reddedildiğinde kırmızı ret gerekçeli kurumsal HTML e-posta şablonu üretilerek talep sahibine gönderilir.
  - ✅ **Talep Seçenekleri Yönetimi (`/VehicleRequest/Options`):** `Admin` ve `DaireBaskani` rollerinin talep formunda çıkan Birim (`RequestDepartments`) ve Araç Türü (`RequestVehicleTypes`) listelerine yeni kayıt ekleyebildiği, sıralamasını düzenleyebildiği ve aktif/pasif yapabildiği ekran geliştirildi.
- **İlgili Entity ve Ekranlar:**
  - *Entities:* `VehicleRequest`, `RequestDepartment`, `RequestVehicleType`
  - *Controller & Servis:* `VehicleRequestController`, `VehicleRequestService`, `EmailTemplateBuilder`
  - *Views:* `Views/VehicleRequest/Create.cshtml`, `Success.cshtml`, `Index.cshtml`, `Fulfill.cshtml`, `Options.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Çift Katmanlı Kurumsal E-Posta ve Telefon Doğrulaması:** Yalnızca `@duzce.edu.tr` uzantılı e-postalar ve `5xx xxx xx xx` formatındaki 10/11 haneli geçerli cep telefonları kabul edilir.
  - ✅ **Şoför Kişisel Veri Gizliliği Kuralı:** Talep sahibine gönderilen onay e-postasında görevli şoförün **yalnızca Adı Soyadı** yer alır; şoförün telefon numarası ve e-posta adresi gizlenir. Koordinasyon için seçilen İletişim Kişisinin (Şef/İrtibat) iletişim bilgileri paylaşılır ve görev amacındaki dahili `[Talep #...]` önekleri temizlenir.
  - ✅ **E-Posta Hata Toleransı:** SMTP sunucusuna ulaşılamasa bile hata yakalanıp loglanır; görevlendirme ve talep onay işlemi kesintiye uğramaz.

### 3.11 Kullanıcı Yönetimi (Ulaşım Şefleri ve Üst Yöneticiler) Modülü
- **Geliştirilen Özellikler:**
  - ✅ Sistemdeki `Kullanici` (Ulaşım Şefi), `DaireBaskani` (Daire Başkanı) ve `GenelSekreter` (Genel Sekreter) hesaplarının listelendiği, yeni yetkili hesabının açıldığı, bilgilerinin güncellendiği, dondurulduğu (`ToggleStatus`), şifresinin sıfırlandığı (`ResetPassword`) ve yumuşak silindiği (`Delete`) yönetim modülü geliştirildi.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `ApplicationUser` (`AspNetUsers`, `AspNetUserRoles`)
  - *Controller:* `SefController`
  - *Views:* `Views/Sef/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Hiyerarşik Rol Güvenliği:** `Admin`, `DaireBaskani` ve `GenelSekreter` rollerine sahip hesapları yalnızca saf `Admin` oluşturabilir, düzenleyebilir, şifresini sıfırlayabilir, dondurabilir veya silebilir. `DaireBaskani` rolü yalnızca `Kullanici` (Şef) hesapları üzerinde işlem yapabilir. `GenelSekreter` ise bu ekranı yalnızca salt okunur olarak izleyebilir.
  - ✅ **Kendi Hesabını Koruma:** Hiçbir yönetici kendi hesabını bu ekrandan düzenleyemez, donduramaz veya silemez.
  - ✅ **Kullanıcı Yumuşak Silme (Soft-Delete) Protokolü:** Silinen kullanıcı veritabanından fiziksel olarak silinmez (`AuditLog` ve geçmiş görev/vekâlet yabancı anahtarlarını bozmamak için); `IsActive = false` yapılır, `LockoutEnabled = true` ve `LockoutEnd = DateTimeOffset.MaxValue` ile kilitlenir, tüm rollerden çıkarılır ve aktif vekâletleri otomatik sonlandırılır.

### 3.12 Geçici Yetki Devri (Vekâlet) Modülü
- **Geliştirilen Özellikler:**
  - ✅ **Vekâlet Yönetimi (`/RoleDelegation` - Faz 1-B):** Ulaşım Şeflerinin izin, hastalık veya görevlendirme durumlarında yetkilerini belirli bir tarih-saat aralığı için başka bir aktif Ulaşım Şefine veya Şoföre devredebilmesi sağlandı.
  - ✅ **Dinamik Rol Dönüşümü (`DelegationClaimsTransformation.cs`):** Vekâlet süresi aktif olduğu müddetçe, vekil personelin veritabanındaki kalıcı rolü değiştirilmeden her HTTP isteğinde bellekte (`15 saniye` süreli `IMemoryCache` destekli) dinamik olarak `ClaimTypes.Role = "Kullanici"`, `DelegatorUserId` ve `DelegationId` claim'leri eklenir.
  - ✅ **Erken Sonlandırma (`Revoke`) ve Üst Bant Bildirimi:** Hem asıl şefin hem de vekil personelin ekranının üst kısmında aktif vekâlet bilgi bandı gösterilir; asıl şef göreve erken döndüğünde tek tıkla yetki devrini sonlandırabilir ve önbellek anında temizlenir (`InvalidateUserCache`).
  - ✅ **Resmi Yetki Devri ve Vekâlet Belgesi PDF (`RoleDelegationPdfDocument`):** Belge No (`B.30.2.DZC.0.73.00/YD-00001`), düzenleme tarihi, dayanak satırı, devreden/devralan bilgileri, vekâlet süresi, uygulama esasları ve ıslak imza kutularını içeren A4 resmi PDF belgesi ve önizlemesi üretilir.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `RoleDelegation` (`RoleDelegations`)
  - *Controller & Servis:* `RoleDelegationController`, `RoleDelegationService`, `DelegationClaimsTransformation`, `RoleDelegationPdfDocument`
  - *Views:* `Views/RoleDelegation/Index.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Zincirleme ve Çakışan Devir Yasağı:** Vekil konumundaki bir personel devraldığı yetkiyi başkasına devredemez (zincirleme devir engeli). Aynı tarih aralığında çakışan ikinci bir vekâlet oluşturulamaz.
  - ✅ **Şoför Görev Çakışması ve Saha Yasağı:** Görevi devralacak kişi bir Şoför ise, vekâlet tarih aralığında planlı veya devam eden bir sürüş görevi varsa sistem çakışan görevleri kırmızı listeyle göstererek vekâleti reddeder. Vekâlet süresince vekil şoför yeni görev atamalarında müsaitlik listesinde kapalı görünür ve mobil saha başlat/bitir işlemlerini kullanamaz.

### 3.13 Sistem Tanımları (Dinamik Seçenek Listeleri) Modülü
- **Geliştirilen Özellikler:**
  - ✅ **5 Sabit Kategori Yönetimi (`/SystemDefinition`):** Kodda güvenli beyaz liste (`SystemDefinitionCategories.cs`) olarak tanımlanan 5 kategorideki (`TireSeason` - Lastik Mevsimi, `FuelType` - Yakıt Tipi, `MaintenanceType` - Bakım/Servis Türü, `VehicleType` - Araç Türü, `LicenseClass` - Ehliyet Sınıfı) açılır liste seçeneklerinin merkezi yönetimi sağlandı.
  - ✅ Her seçenek için gerçek zamanlı kullanım sayısı (`"N kayıtta kullanılıyor"`), sistem varsayılanı rozeti (`IsSystemDefault`), sıralama (`DisplayOrder`), yeni seçenek ekleme, isim değiştirme, aktif/pasif yapma ve kullanılmayan özel tanımları silme özellikleri geliştirildi.
  - ✅ Dinamik seçenekleri form ekranlarına sağlayan `/SystemDefinition/GetActiveOptions` JSON uç noktası eklendi.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `SystemDefinition` (`SystemDefinitions`)
  - *Controller & Servis:* `SystemDefinitionController`, `SystemDefinitionService`, `SystemDefinitionCategories`
  - *Views:* `Views/SystemDefinition/Index.cshtml`
- **Önemli İş Kuralları:**
  - ✅ **Atomik Kaskat İsim Güncelleme (Cascade Rename):** Özel tanımların (`IsSystemDefault == false`) adı değiştirildiğinde; ilişkili tüm tablolardaki (`Vehicles`, `VehicleTires`, `Maintenances`, `DriverLicenses`, `Drivers`) kayıtlar tek bir veritabanı transaction'ı içinde otomatik olarak yeni adla güncellenir. Sistem varsayılanı (`IsSystemDefault == true`) olan çekirdek tanımların adı kilitlidir.
  - ✅ **Sunucu Taraflı Seçenek Doğrulaması (`ValidateOptionAsync`):** Araç, lastik, bakım ve şoför formlarında gönderilen değerlerin ilgili kategoride aktif bir seçenek olduğu sunucuda doğrulanır; düzenleme formlarında kaydın mevcut eski değeri pasife alınmış olsa dahi `(Pasif)` etiketiyle korunur.
  - ✅ **Türkçe Normalizasyon ve Son Aktif Seçenek Koruması:** İsim tekilliği `ToUpper(tr-TR)` (`NormalizedName`) üzerinden kontrol edilir. Bir kategorideki son aktif seçenek pasife alınamaz ve silinemez.

### 3.14 Resmi Rapor Merkezi ve Ortak PDF Önizleme Modülü
- **Geliştirilen Özellikler:**
  - ✅ **5 Sekmeli Kurumsal Rapor Merkezi (`/Report` - Faz 7-A, Faz 7-B, Faz 8, Faz 10):** Tüm rapor sayfalarında ortak sekme çubuğu, KPI özet kartları, hızlı tarih kısayolları (`Bu Ay`, `Geçen Ay`, `Bu Yıl`, `Geçen Yıl`, `Tümü`), sunucu taraflı 10'arlı sayfalama ve QuestPDF tabanlı A4 Yatay (Landscape) resmi PDF çıktıları geliştirildi:
    1. **Sefer & Kullanım Raporu (`/Report/Index` & `ExportPdf`):** Tamamlanmış görevlerin şoför bazlı gruplanmış dökümü, her satırda `"X sa Y dk"` formatında görev süresi (`DurationHelper`), şoför alt toplamları ve çoklu şoförlü görevlerin çifte sayılmasını engelleyen tekil (`DistinctAssignments`) Rapor Genel Toplamı.
    2. **Araç Bakım & Tamir Dökümü (`/Report/Maintenance` & `ExportMaintenancePdf`):** Araç, bakım türü, durum (devam eden/tamamlanan) ve tarih-saat filtreli bakım geçmişi, serviste geçen toplam gün, satır bazlı maliyet ve Genel Toplam Bakım Maliyeti.
    3. **Araç Belge Durum Raporu (`/Report/InsuranceInspection` & `ExportInsuranceInspectionPdf`):** Kapsam (`Tümü`, `Trafik Sigortası`, `Fenni Muayene`, `Kasko`), durum (`Tümü`, `Süresi Geçenler`, `30 Gün İçinde Dolacaklar`, `Özel Tarih`) ve tek araç filtreli birleşik 9 sütunlu belge ve maliyet dökümü.
    4. **Araç Lastik Raporu (`/Report/Tires` & `ExportTiresPdf`):** Tek araç seçildiğinde aracın tüm lastik geçmişini, tüm filo seçildiğinde her aracın güncel lastiğini durum filtreleriyle (`Normal`, `Yaklaşıyor`, `Değişim Gerekli`, `Veri Eksik`) listeleyen rapor.
    5. **Araç Talepleri Raporu (`/Report/VehicleRequests` & `ExportVehicleRequestsPdf`):** Talep eden birimler bazında toplam talep, karşılanan, reddedilen ve bekleyen talep istatistiklerini ve genel toplamları sunan rapor.
  - ✅ **Ortak PDF Önizleme Modalı (`_PdfPreviewModal.cshtml` - Faz 8):** Sistemdeki tüm PDF çıktılarının (`preview=true` parametresi ve `Content-Disposition: inline` başlığıyla) sayfa yenilenmeden ve dosya indirmeye zorlamadan modal içindeki `<iframe>` üzerinden anlık önizlenmesi, modal içinden tek tıkla indirilmesi veya mobil cihazlarda yeni sekmede açılması sağlandı.
- **İlgili Entity ve Ekranlar:**
  - *Controller & Servis:* `ReportController`, `ReportService`, `UsageReportPdfDocument`, `MaintenanceReportPdfDocument`, `InsuranceInspectionReportPdfDocument`, `VehicleTiresReportPdfDocument`, `VehicleRequestReportPdfDocument`, `FileDownloadHelper`, `DurationHelper`
  - *Views:* `Views/Report/Index.cshtml`, `Maintenance.cshtml`, `InsuranceInspection.cshtml`, `Tires.cshtml`, `VehicleRequests.cshtml`, `Views/Shared/_PdfPreviewModal.cshtml`

### 3.15 Denetim Günlüğü (AuditLog) Modülü
- **Geliştirilen Özellikler:**
  - ✅ Sistemdeki tüm kritik veri oluşturma (`Create`), güncelleme (`Update`) ve yumuşak silme (`Delete`) işlemlerinin eski (`OldValues`) ve yeni (`NewValues`) durumlarını JSON olarak saklayan değiştirilemez (immutable) denetim günlüğü geliştirildi.
  - ✅ **Vekâlet İzleme (Faz 1-B):** `IHttpContextAccessor` üzerinden aktif kullanıcının `DelegatorUserId` ve `DelegationId` claim'leri otomatik okunarak vekâleten yapılan her işlemde `OnBehalfOfUserId` ve `DelegationId` alanları dolduruldu; arayüzde `"Ali Demir (Ahmet Şef adına vekâleten [YD-00001])"` şeklinde mavi rozetle gösterildi.
  - ✅ **Türkçe Okunabilir İnceleme Modalı:** Ham JSON veya teknik alan adları yerine tüm alan ve enum değerlerini Türkçe etiketlerle ve kullanıcı ID'lerini `UserMap` üzerinden gerçek ad-soyad/unvan bilgileriyle çözümleyen karşılaştırmalı detay modalı oluşturuldu.
- **İlgili Entity ve Ekranlar:**
  - *Entity:* `AuditLog` (`AuditLogs` — `BaseAuditableEntity`'den türemez, silinemez ve güncellenemez)
  - *Controller & Servis:* `AuditLogController`, `AuditLogService`
  - *Views:* `Views/AuditLog/Index.cshtml`

---

## 4. Hata Düzeltmeleri ve Teknik İyileştirmeler

Kod tabanında, servislerde ve arayüz katmanında tespit edilip kalıcı olarak çözülen teknik sorunlar ve iyileştirmeler aşağıda tablo halinde sunulmuştur:

| No | Sorun / İhtiyaç Tanımı | Uygulanan Çözüm | Etkilenen Dosya(lar) |
|---|---|---|---|
| **1** | **Türkçe ve İngilizce Ondalık Ayraç (`1.250,50` vs `5.0`) Model Binding Hatası:** HTML5 number input'larından veya Türkçe klavyeden girilen noktalı/virgüllü `decimal` tutarların yanlış çözümlenmesi (`5.0` değerinin `50` olması veya validasyon hatası vermesi). | Hem nokta (`.`) hem virgül (`,`) ayracını birlikte veya tekil olarak akıllıca analiz edip `CultureInfo.InvariantCulture` ile çözümleyen özel `InvariantDecimalModelBinder` ve `InvariantDecimalModelBinderProvider` yazılarak MVC pipeline'ının en başına (`Insert(0, ...)`) eklendi. | `Common/ModelBinders/InvariantDecimalModelBinder.cs`, `InvariantDecimalModelBinderProvider.cs`, `Program.cs` |
| **2** | **HTTP `Content-Disposition` Başlığında Türkçe Karakter Çökmesi (RFC 7230):** PDF indirme ve önizleme yanıtlarında dosya adında geçen `ı, İ, ğ, Ğ, ü, Ü, ş, Ş, ö, Ö, ç, Ç` karakterlerinin Kestrel sunucusunda HTTP header hatası fırlatması. | `FileDownloadHelper.ToSafeAsciiFileName` ve `SetContentDisposition` yardımcı metotları yazılarak tüm PDF dosya adları güvenli ASCII karşılıklarına dönüştürüldü ve `inline`/`attachment` başlıkları standartlaştırıldı. | `Common/Helpers/FileDownloadHelper.cs`, `AssignmentController.cs`, `ReportController.cs`, `FaultReportController.cs`, `VehicleController.cs`, `RoleDelegationController.cs`, `SurucuController.cs` |
| **3** | **Uygulama Yeniden Başladığında Oturumların Düşmesi ve Şifrelerin Sıfırlanması:** Her uygulama başlangıcında DataProtection anahtarlarının yenilenmesi ve Seed işleminin mevcut kullanıcı şifrelerini ezmesi nedeniyle oturumların kopması. | DataProtection anahtarları `App_Data/DataProtection-Keys` dizininde kalıcı hale getirildi (`PersistKeysToFileSystem`), `SecurityStampValidatorOptions.ValidationInterval` 8 saate çıkarıldı ve `DbInitializer.SeedAsync` içinde mevcut kullanıcıların şifrelerine dokunmama kuralı uygulandı. | `Program.cs`, `Data/DbInitializer.cs` |
| **4** | **Anonim Araç Taleplerinde ve Arka Plan Servisinde `AuditLog` Yabancı Anahtar (FK) Hatası:** Giriş yapmamış personelin oluşturduğu taleplerde veya sistem worker'ı çalışırken `ChangedByUserId` alanının `AspNetUsers` FK kısıtına takılması. | `MakeAuditLogUserIdNullable` migration'ı ile `AuditLogs.ChangedByUserId` kolonu `nullable` yapıldı; anonim ve sistem işlemlerinde `null` geçilerek kullanıcı bilgisi JSON içeriğinde tutuldu. | `Migrations/20260903074226_MakeAuditLogUserIdNullable.cs`, `Services/Implementations/VehicleRequestService.cs`, `VehicleDocumentExpirationWorker.cs` |
| **5** | **Kullanıcı (Şef) Silindiğinde `AuditLog` ve Görev Geçmişi FK Çakışması:** Daha önce işlem yapmış bir Ulaşım Şefinin fiziksel (`DELETE`) silinmesi halinde `AuditLogs` ve `Assignments` tablolarındaki `Restrict` yabancı anahtarların hata vermesi. | `SefController.Delete` aksiyonu fiziksel silme yerine kapsamlı **Soft-Delete** protokolüne çevrildi (`IsActive = false`, `LockoutEnd = DateTimeOffset.MaxValue`, tüm rollerin kaldırılması ve aktif vekâletlerin otomatik iptali). | `Controllers/SefController.cs` |
| **6** | **Açık Bakım Kaydı Olmadığı Halde `Bakımda` (`Maintenance`) Kalan Yetim Araçlar:** Bakım kaydı silinen veya tamamlanan bazı araçların durumunun `Maintenance` olarak asılı kalması. | `DbInitializer.SeedAsync` içerisine **Self-Healing (Kendi Kendini Onarma)** bloğu ve `MaintenanceService.DeleteMaintenanceAsync` / `CompleteMaintenanceAsync` içerisine başka açık bakım yoksa aracı otomatik `Idle` (`Boşta`) durumuna döndüren kontrol eklendi. | `Data/DbInitializer.cs`, `Services/Implementations/MaintenanceService.cs` |
| **7** | **Çoklu Şoförlü Görevlerde Kullanım Raporu Çifte Sayım (Double Counting) Sorunu:** Çoklu şoför atanan bir görevin raporda her şoförün grubunda listelenirken rapor sonundaki genel toplam mesafeyi ve sefer sayısını iki kez artırması. | Şoför bazlı alt gruplarda her şoförün kendi seferi gösterilirken, raporun KPI kartlarında ve dip genel toplamında `AssignmentId` bazında tekilleştirme (`distinctAssignments = reportItems.GroupBy(x => x.AssignmentId).Select(g => g.First())`) uygulandı. | `Services/Implementations/ReportService.cs`, `UsageReportPdfDocument.cs` |
| **8** | **Geçmiş Tarihli Görev Onayında Şef KM Düzeltmesinin Aracın Güncel KM'sini Düşürmesi:** Aracın daha yeni bir görevi tamamlanmışken eski bir görevin bitiş KM'si onaylandığında `Vehicle.CurrentKm` değerinin yanlışlıkla eski/küçük değere çekilmesi. | `ApproveEndAsync` içerisine `hasLaterCompletedAssignment` kontrolü eklendi; `Vehicle.CurrentKm` yalnızca onaylanan görev aracın **en son tamamlanan göreviyse** ve yeni KM mevcut KM'den **büyükse** güncellenir. | `Services/Implementations/AssignmentService.cs` |
| **9** | **Vekâlet Sonlandırıldığında Yetkinin Önbellekte Asılı Kalması ve Mükerrer Claim Eklenmesi:** `IClaimsTransformation` birden çok kez çalıştığında aynı rol/claim'in mükerrer eklenmesi ve vekâlet iptal edildiğinde eski yetkinin devam etmesi. | `DelegationClaimsTransformation.TransformAsync` tamamen **idempotent** hale getirildi (`!principal.IsInRole`, `!identity.HasClaim`), vekâlet bittiğinde mevcut vekâlet claim'lerini temizleyen `RemoveClaim` bloğu eklendi ve `RoleDelegationService` içinde 15 saniyelik `IMemoryCache` + işlem anında `InvalidateUserCache` uygulandı. | `Common/DelegationClaimsTransformation.cs`, `Services/Implementations/RoleDelegationService.cs` |
| **10** | **İç İçe (Nested) Bootstrap Modallarında Backdrop Donması ve PDF Yükleme Spinner'ının Takılı Kalması:** Kart veya tablo içine gömülen modalların arka planı kilitlemesi ve bazı tarayıcılarda PDF `iframe` `load` olayının tetiklenmemesi. | Tüm modallar (`_PdfPreviewModal`, `#modalCreateDelegation`, `#createOptionModal`, `#changeRouteModal` vb.) kartların dışına, `</body>` öncesine taşındı. PDF önizleme modalında `.is-hidden` sınıfı, modal kapandığında `src="about:blank"` sıfırlaması ve **3000 ms güvenlik zaman aşımı (fallback timeout)** uygulandı. | `Views/Shared/_PdfPreviewModal.cshtml`, `Views/RoleDelegation/Index.cshtml`, `Views/SystemDefinition/Index.cshtml`, `Views/Surucu/Detay.cshtml` |
| **11** | **Resmi PDF Belgelerinde GUID Basılması ve Dahili Şef Notlarının Sızması:** Kullanıcının tam adı bulunamadığında imza alanlarına kullanıcı ID'sinin (GUID) basılması ve görev emrinde/e-postada dahili `(Şef Notu: ...)` veya `[Talep #...]` metinlerinin görünmesi. | `AssignmentOrderDocument.FilterPersonName` ve `FaultReportDocument` içinde `Guid.TryParse` ve regex filtreleri ile GUID basılması kesin olarak engellendi; `CleanPurpose` ve `EmailTemplateBuilder` içinde regex ile şef notları ve talep önekleri temizlendi. | `Services/Implementations/AssignmentOrderDocument.cs`, `FaultReportDocument.cs`, `Common/Helpers/EmailTemplateBuilder.cs` |
| **12** | **Geliştirme Ortamı Captcha Sabit Kod (`12345` / `TEST`) ve Boş Token Bypass'ının Kaldırılması:** Test kolaylığı için eklenen geliştirme ortamı Captcha geçişlerinin gerçek kullanıcı akışıyla birebir aynı olması ihtiyacı. | `CaptchaService.ValidateCaptcha` içindeki `"12345"` / `"TEST"` geçişi ve `AccountController.Login` içindeki `isDevScriptBypass` kontrolü kaldırılarak her ortamda gerçek dinamik SVG Captcha doğrulaması zorunlu kılındı. | `Services/Implementations/CaptchaService.cs`, `Controllers/AccountController.cs` |
| **13** | **Sabit/Varsayılan Saatlik Ücret ve Eski "Yevmiye / Kadrolu Personel" Terminolojisi:** İlk sürümlerde kod içine gömülü varsayılan saatlik ücret bulunması ve kurum yapısına uymayan "Yevmiye / Kadrolu Personel" ifadelerinin yer alması. | Sabit ücret kaldırılarak `HourlyRate` / `PerDiemRate` şefin serbest girişine bırakıldı; tüm kod tabanı, arayüz ve PDF şablonlarında terminoloji **"Görev Ücreti"**, **"Saatlik Ücret"** ve **"Harcırah"** olarak standartlaştırıldı. | `Services/Implementations/AssignmentService.cs`, `Views/Assignment/Details.cshtml`, `Index.cshtml` |
| **14** | **Mobil Cihazlarda Önbellekten (Cache) Eski Sayfa Gösterimi:** Mobil tarayıcılarda geri tuşu veya sekme geçişlerinde eski görev durumlarının önbellekten gelmesi. | `Program.cs` içerisine HTML yanıtları ve operasyonel rotalar (`/Home`, `/Report`, `/Vehicle`, `/Assignment`, `/Maintenance`, `/Driver`, `/Surucu`) için `Cache-Control: no-cache, no-store, must-revalidate`, `Pragma: no-cache`, `Expires: 0` başlıklarını ekleyen ara yazılım (middleware) eklendi. | `Program.cs` |

---

## 5. Güvenlik Çalışmaları

Projede kurumsal bilgi güvenliği standartları doğrultusunda hem uygulama kodunda hem de `.agent/skills/security-audit` yetkinliği ve `C:\Users\Dell\audits\arac-takip` denetim mimarisi kapsamında doğrulanan güvenlik kontrolleri aşağıda detaylandırılmıştır:

### 5.1 Kimlik Doğrulama, Parola Politikası ve Oturum Güvenliği
- ✅ **Güçlü Parola Karmaşıklığı (`Program.cs`):** ASP.NET Core Identity üzerinde en az **10 karakter** uzunluk (`RequiredLength = 10`), en az 1 rakam (`RequireDigit`), en az 1 küçük harf (`RequireLowercase`), en az 1 büyük harf (`RequireUppercase`), en az 1 özel karakter (`RequireNonAlphanumeric`) ve en az 3 farklı karakter (`RequiredUniqueChars = 3`) zorunluluğu tanımlanmıştır.
- ✅ **Kriptografik Rastgele Parola Üretimi (`DbInitializer.GenerateSecurePassword`):** Sistem tarafından üretilen parolalarda `System.Security.Cryptography.RandomNumberGenerator` kullanılarak tahmin edilemez parolalar oluşturulmaktadır.
- ✅ **Kaba Kuvvet (Brute-Force) Hesap Kilitleme (Lockout):** Art arda **5 hatalı giriş** denemesinde (`MaxFailedAccessAttempts = 5`) kullanıcı hesabı otomatik olarak **15 dakika** süreyle (`DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`) kilitlenir.
- ✅ **Çerez (Cookie) ve Oturum Sertleştirmesi:** Kimlik doğrulama çerezlerinde `HttpOnly = true` (XSS üzerinden çerez çalınmasını engeller), `SameSite = SameSiteMode.Lax` (CSRF koruması), `SlidingExpiration = true`, 7 günlük geçerlilik süresi ve `App_Data/DataProtection-Keys` üzerinde kalıcı şifreleme anahtarları yapılandırılmıştır.
- ✅ **Kullanıcı İfşası (User Enumeration) Koruması:** `ForgotPassword` ve `ResetPassword` uç noktalarında girilen e-posta/kullanıcı adı sistemde bulunmasa dahi saldırgana ipucu vermemek için standart başarı mesajı döndürülür.

### 5.2 Durumsuz (Stateless) Şifreli SVG Captcha Koruması
- ✅ **Kaba Kuvvet Öncesi Doğrulama:** `AccountController.Login` POST işleminde veritabanına kullanıcı/şifre sorgusu yapılmadan **önce** Captcha doğrulanır.
- ✅ **DataProtection Şifreli Token (`CaptchaService.cs`):** Üretilen 5 karakterli güvenlik kodu (`0, O, 1, I, l` gibi karıştırılan karakterler hariç tutularak), `IDataProtector` (`aractakipsys.CaptchaProtection.v1`) ile `CAPTCHA:{code}:{expiryUnixTime}:{nonce}` formatında şifrelenir. Her token yalnızca **3 dakika** geçerlidir.
- ✅ **OCR Bot Direnci:** SVG görselinde 25 adet rastgele gürültü noktası, 4 adet Bezier eğrisi (`<path>`) ve her karakter için `-18°` ile `+19°` arasında rastgele döndürme (`transform="rotate(...)"`) uygulanır.

### 5.3 İstek Hız Sınırlandırması (IP Bazlı Rate Limiting)
`Program.cs` içerisinde `AddRateLimiter` ve `app.UseRateLimiter()` ile 6 katmanlı hız sınırlandırma politikası aktif edilmiştir. Limit aşıldığında `429 Too Many Requests` durum koduyla AJAX isteklerine JSON, tarayıcı isteklerine kurumsal Türkçe HTML uyarı sayfası döndürülür:

| Politika Adı | Uygulandığı Uç Nokta | Algoritma | Limit ve Pencere Süresi |
|---|---|---|---|
| **`GlobalLimiter`** | Tüm Sistem (Global Kalkan) | Sliding Window (6 Segment) | IP başına dakikada **150 istek** |
| **`login-limit`** | `POST /Account/Login` | Sliding Window (6 Segment) | IP başına dakikada **5 deneme** |
| **`forgot-password-limit`** | `POST /Account/ForgotPassword` | Fixed Window | IP başına 5 dakikada **3 istek** |
| **`reset-password-limit`** | `POST /Account/ResetPassword` | Fixed Window | IP başına 5 dakikada **5 istek** |
| **`captcha-limit`** | `GET /Account/GetNewCaptcha` | Fixed Window | IP başına dakikada **15 istek** |
| **`vehicle-request-limit`** | `POST /AracTalebi` (`/VehicleRequest/Create`) | Sliding Window (6 Segment) | IP başına dakikada **5 talep** |

### 5.4 Rol Tabanlı Yetkilendirme (RBAC), Salt Okunur Kalkanı ve Yatay Yetki İzolasyonu
- ✅ **Merkezi Rol Sabitleri (`AppRoles.cs`):** Tüm Controller ve Action'larda `[Authorize(Roles = ...)]` nitelikleri merkezi sabitlerle (`SuperAdminOnly`, `AdminOrDaireBaskani`, `ManagementAndAudit`, `AllStaffAndManagers`, `OperationalStaff`) korunmuştur.
- ✅ **Merkezi Salt Okunur Güvenlik Kalkanı (`ReadOnlyRoleFilter.cs`):** Global MVC filtresi olarak çalışan `ReadOnlyRoleFilter`, `GenelSekreter` rolündeki kullanıcıların `GET`, `HEAD`, `OPTIONS` dışındaki tüm veri değiştirici (`POST`, `PUT`, `DELETE`, `PATCH`) isteklerini (`AccountController` üzerindeki `Login`, `Logout`, `Profile`, `ChangePassword`, `ForgotPassword`, `ResetPassword` istisnaları hariç) sunucu katmanında `403 Forbidden` (AJAX) veya uyarı mesajlı yönlendirme ile kesin olarak engeller. Ayrıca `_AdminLayout.cshtml` içinde CSS katmanında da tüm değiştirici buton ve modallar gizlenir.
- ✅ **Dikey Yetki Yükseltme (Privilege Escalation) Koruması (`SefController.cs`):** `DaireBaskani` rolünün `Admin`, `DaireBaskani` veya `GenelSekreter` rolünde yeni kullanıcı açması, mevcut bir kullanıcıyı bu rollere yükseltmesi veya bu rollerdeki hesapların şifresini sıfırlaması/silmesi/dondurması sunucu tarafında `User.IsAdmin()` kontrolüyle engellenir ve güvenlik loguna yazılır.
- ✅ **Yatay Yetki Kontrolü (IDOR Koruması):**
  - `SurucuController` ve `DriverService` içindeki tüm sorgularda `Where(a => a.DriverId == driver.Id || a.AssignmentDrivers.Any(ad => !ad.IsDeleted && ad.DriverId == driver.Id))` filtresi zorunlu tutulmuştur.
  - `RoleDelegationService` içinde Şef rolündeki kullanıcıların yalnızca kendilerinin devrettiği veya kendilerine devredilen vekâlet kayıtlarını ve PDF belgelerini görebilmesi (`!isElevatedAdmin` kontrolü) sağlanmıştır.

### 5.5 Dosya Yükleme Güvenliği (Magic Number / Dosya Başlığı Bayt Doğrulaması)
Sisteme yüklenen profil fotoğraflarında (`AccountController.IsValidImageFile`) ve imzalı görev belgelerinde (`AssignmentService.IsValidDocumentFile`) istemcinin gönderdiği `Content-Type` başlığına veya dosya uzantısına güvenilmez; dosyanın ilk baytları (**Magic Bytes**) okunarak zararlı script/kabuk (Web Shell / RCE) yüklenmesi engellenir:
- ✅ **JPEG / JPG (`.jpg`, `.jpeg`):** İlk 3 bayt `0xFF, 0xD8, 0xFF` doğrulaması.
- ✅ **PNG (`.png`):** İlk 8 bayt `0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A` doğrulaması.
- ✅ **WebP (`.webp` - Profil Fotoğrafı):** İlk 4 bayt `0x52, 0x49, 0x46, 0x46` (`RIFF`) ve 8-11. baytlar `0x57, 0x45, 0x42, 0x50` (`WEBP`) doğrulaması.
- ✅ **PDF (`.pdf` - İmzalı Görev Belgesi):** İlk 4 bayt `0x25, 0x50, 0x44, 0x46` (`%PDF`) doğrulaması.
- ✅ **Boyut ve Kestrel Sınırları:** Profil fotoğrafları **5 MB**, imzalı görev belgeleri ise hem uygulama hem Kestrel seviyesinde (`[RequestSizeLimit(10 * 1024 * 1024)]` ve `[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]`) **10 MB** ile sınırlandırılmıştır.
- ✅ **Path Traversal Önleme ve Güvenli Dizin (`App_Data`):** Yüklenen dosya adları diske asla orijinal adıyla yazılmaz; `{assignmentId}_{Guid.NewGuid():N}{ext}` formatında yeniden adlandırılır. İmzalı resmi görev belgeleri doğrudan web erişimine açık olan `wwwroot` altında değil, dışarıdan erişilemeyen `App_Data/AssignmentDocuments` dizininde saklanır ve yalnızca yetki kontrolünden geçen `DownloadSignedDocument` / `ViewSignedDocument` aksiyonları üzerinden akış (stream) olarak sunulur.

### 5.6 SQL Injection, XSS, CSRF ve Açık Yönlendirme (Open Redirect) Korumaları
- ✅ **SQL Injection Koruması:** Veri erişim katmanının tamamında parametreli Entity Framework Core LINQ sorguları kullanılmıştır; kod tabanında kullanıcı girdisi birleştiren hiçbir ham SQL (`FromSqlRaw` / `ExecuteSqlRaw`) sorgusu bulunmamaktadır.
- ✅ **CSRF (Cross-Site Request Forgery) Koruması:** Veri değiştiren tüm HTTP `POST` aksiyonlarında `[ValidateAntiForgeryToken]` niteliği ve Razor formlarında `@Html.AntiForgeryToken()` doğrulaması uygulanmıştır.
- ✅ **XSS ve HTML E-Posta Kodlama:** Razor görünümlerinde otomatik HTML encoding kullanılmış; `EmailTemplateBuilder` içinde dış kullanıcıdan gelen tüm metin alanları (`RequesterFullName`, `Department`, `Purpose`, `RejectionReason`, `ChiefNote`) `WebUtility.HtmlEncode` ile temizlenmiştir.
- ✅ **Open Redirect Koruması:** Giriş ve işlem sonrası yönlendirmelerde `Url.IsLocalUrl(returnUrl)` kontrolü uygulanarak harici zararlı sitelere yönlendirme engellenmiştir.

### 5.7 Güvenlik Denetimi Altyapısı (`.agent/skills/security-audit` & `C:\Users\Dell\audits\arac-takip`)
- ✅ Proje içerisinde `.agent/skills/security-audit` altında kapsamlı güvenlik denetimi beceri seti (`SKILL.md`, `WEB-PROTOCOL-AND-AUTH.md`, `CLIENT-SIDE.md`, `DATA-ISOLATION-AND-LIFECYCLE.md`, `RESOURCE-EXHAUSTION-AND-AVAILABILITY.md`, `ATTACK-CLASSES.md`, `HUNTING.md`, `RECONNAISSANCE.md`, `VALIDATION-AND-REPORTING.md` ve doğrulama scriptleri) yapılandırılmıştır.
- ✅ `C:\Users\Dell\audits\arac-takip` dizininde `arac-takip-20261123-01` denetim çalışması kapsamında sistemin mimari güvenlik haritası (`architecture.md`), kapsam defteri (`coverage-ledger.json`) ve çalışma üst verileri (`run-metadata.json`) oluşturulmuştur.

---

## 6. Veri ve Yedekleme İşlemleri

Kod tabanında, migration dosyalarında, `DbInitializer` sınıfında ve `scratch/` dizinindeki bakım/doğrulama betiklerinde kayıt altına alınan veri yönetimi ve yedekleme çalışmaları şunlardır:

- ✅ **Veritabanı Tam Yedekleme (`scratch/take_backup.ps1`):** Kritik şema ve Sistem Tanımları geçişleri öncesinde MS SQL Server üzerindeki veritabanının `C:\Yedek\aractakip_db_sistemtanimlari_oncesi.bak` dosyasına tam yedeğini (`BACKUP DATABASE ... WITH INIT, STATS = 10`) alan yedekleme betiği oluşturulmuş ve çalıştırılmıştır.
- ✅ **Migration İçi Otomatik Veri Taşıma (Zero Data Loss SQL Migrations):**
  1. *Enum → Türkçe Metin Normalizasyonu (`20260908065145` & `20260908071220`):* `Vehicles` tablosundaki eski sayısal/İngilizce araç türü, lastik mevsimi ve yakıt tipi değerleri SQL `UPDATE` sorgularıyla Türkçe karşılıklarına dönüştürülmüştür.
  2. *Çoklu Şoför Veri Aktarımı (`20261020233958_AddMultiDriverAndDestinationFields`):* Mevcut tüm görevlerin `DriverId` kayıtları yeni `AssignmentDrivers` ara tablosuna `IsPrimary = 1` olarak aktarılmıştır.
  3. *Çoklu Ehliyet Veri Aktarımı (`20261024184022_AddDriverLicense`):* Mevcut şoförlerin `LicenseClass` ve `LicenseExpiryDate` alanları yeni `DriverLicenses` tablosuna taşınmıştır.
  4. *Araç Belgeleri Veri Aktarımı (`20261024191639_AddVehicleDocument`):* Mevcut araçların muayene ve sigorta başlangıç/bitiş tarihleri `VehicleDocuments` tablosuna `Inspection` ve `Insurance` kayıtları olarak aktarılmıştır.
  5. *Araç Lastikleri Veri Aktarımı (`20261024201613_AddVehicleTire`):* Mevcut araçların `TireBrand`, `TireYear`, `TireSeason` verileri `VehicleTires` tablosuna ilk lastik kaydı olarak taşınmıştır (`InstalledKm` bilinmediği için uydurma veri yazılmayıp `NULL` bırakılmıştır).
  6. *Çoklu İmzalı Görev Belgesi Veri Aktarımı (`20261120111421_AddAssignmentDocumentsTable`):* `Assignments` tablosunda bulunan tekil `SignedDocumentPath` kayıtları veri kaybı olmaksızın yeni `AssignmentDocuments` tablosuna aktarılmıştır.
- ✅ **Canlı Veriden Sistem Tanımlarını İçe Aktarma (`DbInitializer.SeedSystemDefinitionsAsync`):** 5 sabit kategorideki (`TireSeason`, `FuelType`, `VehicleType`, `MaintenanceType`, `LicenseClass`) 35 adet çekirdek sistem varsayılanına (`IsSystemDefault = true`) ek olarak, mevcut veritabanı tablolarında fiilen kullanılmakta olan tüm farklı değerler otomatik taranıp `CreatedBy = "DataImport"` ile `SystemDefinitions` tablosuna kazandırılmıştır.
- ✅ **Kurumsal Başlangıç Verileri (`DbInitializer.SeedAsync`):** 5 sistem rolü, varsayılan yönetici/şef hesapları, 12 örnek şoför, 17 örnek üniversite aracı (`81 DU 001` – `81 DU 310`), örnek görevler ve 10 fakülte/daire başkanlığından gelen örnek araç talepleri yalnızca ilk kurulumda ("yoksa ekle" prensibiyle ve mevcut kullanıcı şifrelerine dokunmadan) oluşturulmaktadır.
- ✅ **Otomasyon ve Doğrulama Betikleri (`scratch/`):** Sistemin uçtan uca iş kurallarını, rol izinlerini (`GenelSekreter`, `DaireBaskani`, `Sef`), kullanıcı yumuşak silme akışını, vekâlet kurallarını, PDF uç noktalarını ve sistem tanımlarını doğrulayan PowerShell test betikleri (`comprehensive_qa_test.ps1`, `run_checklist_tests.ps1`, `run_precise_faz1b_tests.ps1`, `test_system_definitions.ps1`, `test_user_delete_suite.ps1`, `test_genel_sekreter_sef_complete.ps1`, `test_pdf_endpoints.ps1` vb.) geliştirilmiştir.

---

## 7. Sonuç

**Düzce Üniversitesi Araç Takip Paneli**, 27 aşamalı veritabanı evrimi, 15 MVC kontrolcüsü, 19 veri varlığı ve 8 resmi QuestPDF evrak/rapor şablonu ile üniversitenin tüm taşıt görevlendirme, filo belge/lastik/bakım takibi, anonim personel araç talebi ve geçici yetki devri süreçlerini uçtan uca dijitalleştirmiştir. Rol tabanlı yetkilendirme, merkezi salt okunur denetim kalkanı, durumsuz şifreli SVG Captcha, IP bazlı hız sınırlandırma, dosya başlığı (Magic Byte) doğrulaması ve vekâlet izli denetim günlüğü (`AuditLog`) mekanizmalarıyla kurumsal güvenlik ve veri bütünlüğü standartlarını eksiksiz karşılayan, masaüstü ve mobil uyumlu bir sistem olarak tamamlanmıştır.
