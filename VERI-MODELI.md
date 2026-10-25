# VERI MODELİ SPESİFİKASYONU — Araç Takip ve Görevlendirme Sistemi

> Bu dosya `AGENTS.md` ile birlikte okunmalıdır. Amaç: Entity Framework Core (Code-First) ile veritabanı şemasını ve C# entity sınıflarını bu spesifikasyona **birebir uygun** şekilde oluşturmak. Alan adı, tip veya kısıtlama eklemeden/çıkarmadan önce Eslem'e sor.

---

## 0. GENEL KURALLAR

- Tüm Id alanları `int` (Identity, otomatik artan). GUID kullanma.
- Tüm tarih/saat alanları `DateTime` tipinde, UTC olarak saklanır (`DateTime.UtcNow`); arayüzde TR saatine (UTC+3) çevrilerek gösterilir.
- Soft delete kullanılacak: kayıtlar fiziksel silinmez, `IsDeleted` (bool, default false) alanı ile işaretlenir. Global Query Filter ile silinenler sorgulardan otomatik hariç tutulur.
- Her ana tabloda `CreatedAt`, `CreatedBy` (kullanıcı Id'si, string), `UpdatedAt` (nullable), `UpdatedBy` (nullable) alanları standarttır.
- Enum'lar veritabanında **string olarak** saklanır (`HasConversion<string>()`), sadece int olarak değil — okunabilirlik ve ileride sıra değişse bile veri bozulmaması için.

### 0.1 Ortak Taban Sınıf: BaseAuditableEntity
`CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/IsDeleted` alanları her entity'ye tek tek kopyalanmaz; tüm entity sınıfları (`Vehicle`, `Driver`, `Assignment`, `Maintenance`, `AuditLog`) aşağıdaki abstract sınıftan türetilir:

```csharp
// File: Models/Entities/BaseAuditableEntity.cs
public abstract class BaseAuditableEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}
```

> `ApplicationUser`, Identity'nin kendi `IdentityUser` sınıfından türediği için bu base class'a dahil değildir; `IsActive` ile kendi yaşam döngüsünü yönetir.

### 0.2 Global Query Filter — Somut Uygulama
Soft delete filtresi tek tek `Where(x => !x.IsDeleted)` yazılarak değil, `ApplicationDbContext.OnModelCreating` içinde tüm `BaseAuditableEntity` türevlerine tek seferde uygulanır:

```csharp
// File: Data/ApplicationDbContext.cs
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    foreach (var entityType in builder.Model.GetEntityTypes())
    {
        if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
            var condition = Expression.Lambda(Expression.Not(property), parameter);
            builder.Entity(entityType.ClrType).HasQueryFilter(condition);
        }
    }

    // Diğer Fluent API konfigürasyonları (ApplyConfigurationsFromAssembly vb.) burada devam eder.
}
```

---

## 1. KİMLİK / KULLANICI KATMANI

### 1.1 ApplicationUser (IdentityUser'dan türetilir)
| Alan | Tip | Kısıtlama |
|---|---|---|
| Id | string | Identity varsayılanı (GUID string) |
| FullName | string | Required, MaxLength(150) |
| PhoneNumber | string | Identity'nin kendi alanı, opsiyonel |
| IsActive | bool | Default true — pasif kullanıcı giriş yapamaz |
| CreatedAt | DateTime | Required |
| ProfilePictureUrl | string? | MaxLength(500), opsiyonel — özel profil resmi yolu (null ise dinamik rozet avatar kullanılır) |

**Roller (AspNetRoles, seed data ile oluşturulacak):**
- `Admin` — Tam yetki (Vehicle/Driver/Assignment/Maintenance CRUD + AuditLog görüntüleme + kullanıcı yönetimi + Talep Seçenekleri).
- `DaireBaskani` — Yönetim yetkili. Operasyonel tüm ekranları (Vehicle/Driver/Assignment/Maintenance/FaultReport/Report/VehicleRequest Options/AuditLog) görüntüler ve yönetir. Kullanıcı yönetiminde yalnızca `Kullanici` ve `Driver` yönetebilir; `Admin`, `DaireBaskani`, `GenelSekreter` hesaplarına sunucuda dokunamaz.
- `GenelSekreter` — Üst denetim yetkili / Salt Okunur. Tüm ekranları (Dashboard, Takvim, Raporlar, Listeler, Detaylar, PDF İndir/Önizle, AuditLog) görüntüler; veri ekleyemez, değiştiremez veya silemez (`ReadOnlyRoleFilter` ile korunur). Kullanıcı Yönetimi erişimi kapalıdır.
- `Kullanici` — Şoförlerin operasyonel amiri ("Şoför Şefi"). Vehicle/Driver/Assignment/Maintenance CRUD yapabilir. **AuditLog'u ve Talep Seçeneklerini göremez**. Görev Başlat/Bitir işlemlerini yapabilir ve onaylar.
- `Driver` — Sadece kendi görevlerini görüntüler (salt okunur). Görev başlatma/bitirme bilgisini (saat, KM, dönüş notu) kendi girebilir, ama bu girdi Status'u kesinleştirmez — amir onayı gerekir.

> Not: `Kullanici` rolü Admin'den yalnızca 2 şeyde ayrılır: (1) AuditLog'u göremez, (2) sistem yönetimi kapsamındaki başka hiçbir kısıtlaması yoktur — CRUD yetkileri Admin ile aynıdır.

> Not: Şoförle ilgili ek bilgiler (ehliyet no vb.) `ApplicationUser`'a değil, aşağıdaki `Driver` tablosuna konur — böylece Identity tablosu şişmez, iş verisi ayrışır.

### 1.2 Driver (Şoför — ApplicationUser ile 1–1 ilişkili, `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir, ayrıca yazma |
| UserId | string | FK → ApplicationUser.Id, Required, Unique |
| LicenseNumber | string | Required, MaxLength(30) — ehliyet no (tüm sınıflar için şoförde tekil) |
| LicenseClass | string | MaxLength(50), opsiyonel — geriye dönük uyumluluk için senkron tutulur (örn. "B, E") |
| LicenseExpiryDate | DateTime | Geriye dönük uyumluluk için senkron tutulur (en ileri geçerlilik tarihi) |
| Department | string | MaxLength(150) — bağlı olduğu birim (yalnızca Admin rolü güncelleyebilir) |
| IsActive | bool | Default true |

**İlişki:** `Driver.UserId` → `ApplicationUser.Id` (1–1, Restrict delete), `Driver.DriverLicenses` → `DriverLicense` (1–N).

**Tarihsel Ehliyet No Tekilliği (karar: Alternatif B):** `LicenseNumber` normal bir unique index ile korunmaz — silinmiş (soft-delete) şoförler dahil, bir ehliyet numarası sistemde bir kez kullanıldıysa bir daha kullanılamaz. Kontrol Service katmanında `IgnoreQueryFilters()` ile yapılır: `_context.Drivers.IgnoreQueryFilters().AnyAsync(d => d.LicenseNumber == cleanLicense)`. Aynı karar `Vehicle.PlateNumber` için de geçerlidir (bkz. 2.1).

**Eski Kolonlar Güvenlik Stratejisi (Faz 4):** `Driver.LicenseClass` ve `Driver.LicenseExpiryDate` eski kolonları geçiş güvenliği için silinmemiştir; yeni eklenen `DriverLicenses` tablosundaki aktif kayıtların birleşik sınıfı ve en ileri geçerlilik tarihiyle otomatik senkron tutulmaktadır.

**Şoför Birimi Yetki Kuralı (Faz 4):** Şoförün `Department` (Birim) bilgisi sunucu ve istemci tarafında **yalnızca Admin** rolü tarafından düzenlenebilir. Şef (`Kullanici`) ve diğer roller birim bilgisini değiştiremez.

### 1.3 DriverLicense (Çoklu Şoför Ehliyetleri — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| DriverId | int | FK → Driver.Id, Required (Cascade delete) |
| LicenseClass | string | Required, MaxLength(10) — örn. "B", "C", "D", "E" |
| LicenseExpiryDate | DateTime | Required — ehliyet sınıfının son geçerlilik tarihi |

**Ehliyet Süresi ve Görev Ataması (İş Kuralı - Faz 4):** Bir şoföre görev atanırken (`Assignment` oluşturma/düzenleme), şoförün **EN AZ BİR** aktif ehliyetinin geçerlilik tarihi görevin planlanan bitiş tarihini kapsıyorsa (`LicenseExpiryDate.Date >= PlannedEndTime.Date`) şoför uygun kabul edilir. Araç türüne göre ehliyet sınıfı eşleştirmesi yapılmaz. Çoklu şoförlü görevlerde bu kontrol her şoför için bağımsız olarak çalışır.

**Ehliyet Geçerlilik Uyarısı (İş Kuralı - Faz 4):** Geçerlilik süresi 30 gün içinde dolacak veya dolmuş olan ehliyetler; Dashboard'da ayrı uyarı kartında, navbar bildirim çanında, artış olduğunda toast uyarısında ve şoförün kendi mobil görev panelinde listelenir.

---

## 2. ARAÇ KATMANI

### 2.1 Vehicle (Araç — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir, ayrıca yazma |
| PlateNumber | string | Required, MaxLength(15) — **tarihsel tekillik**, bkz. aşağıdaki not |
| Brand | string | Required, MaxLength(50) |
| Model | string | Required, MaxLength(50) |
| Year | int | Required, Range(1990, mevcut yıl) |
| VehicleType | enum `VehicleType` | Required — bkz. 2.2 |
| FuelType | enum `FuelType` | Required — bkz. 2.3 |
| CurrentKm | int | Required, Default 0, ≥ 0 |
| Color | string | Required, MaxLength(30) — otoparkta/personelin hızlı tanıması için |
| Status | enum `VehicleStatus` | Required, Default `Idle` — bkz. 2.4 |
| Notes | string | MaxLength(500), nullable |

**Tarihsel Plaka Tekilliği (karar: Alternatif B, filtrelenmiş/unfiltered index DEĞİL):** `PlateNumber` üzerinde normal (tüm satırları kapsayan) bir unique index vardır, ama bu bilinçli bir tercih: soft-delete edilmiş bir aracın plakası **bir daha asla kullanılamaz** — DB seviyesindeki unique index bunu zaten fiziksel olarak engeller. Service katmanında ayrıca `IgnoreQueryFilters()` ile kontrol edilip kullanıcıya SQL hatası yerine düzgün bir Türkçe mesaj gösterilir ("Bu plaka sistemde daha önce kayıtlı/silinmiş bir araca aittir").

### 2.2 enum VehicleType
`Sedan`, `Minibus`, `Panelvan`, `Kamyonet`, `Otobus`, `Diger`

### 2.3 enum FuelType
`Benzin`, `Dizel`, `LPG`, `Elektrik`, `Hibrit`

### 2.4 enum VehicleStatus (Araç durum makinesi)
`Idle` (Boşta) → `OnDuty` (Görevde) → `Idle`
`Idle` → `Maintenance` (Bakımda) → `Idle`
`Idle` / `OnDuty` → `OutOfService` (Hizmet Dışı, örn. arıza)

> **İş kuralı:** `VehicleStatus` alanı manuel değiştirilmez, `Assignment` ve `Maintenance` kayıtlarının yaşam döngüsüne bağlı olarak Service katmanında otomatik güncellenir. Admin'e "manuel durum değiştir" özelliği istisnai durumlar için ayrıca eklenebilir ama varsayılan akış otomatik olmalı.

---

## 3. GÖREVLENDİRME KATMANI (Çekirdek İş Mantığı)

### 3.1 Assignment (Görev — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir, ayrıca yazma |
| VehicleId | int | FK → Vehicle.Id, Required |
| DriverId | int | FK → Driver.Id, Required |
| RequestedByDepartment | string | MaxLength(150) — görevi talep eden birim |
| Purpose | string | Required, MaxLength(300) — görev amacı/açıklaması |
| PlannedStartTime | DateTime | Required |
| PlannedEndTime | DateTime | Required, **> PlannedStartTime** (validasyon) |
| ActualStartTime | DateTime | Nullable — şoför "görevi başlat" dediğinde dolar |
| ActualEndTime | DateTime | Nullable — şoför "görevi bitir" dediğinde dolar |
| StartKm | int | Nullable — görev başlarken aracın KM'si |
| EndKm | int | Nullable — görev biterken aracın KM'si, **≥ StartKm** |
| Status | enum `AssignmentStatus` | Required, Default `Planned` — bkz. 3.2 |
| CancellationReason | string | MaxLength(300), nullable |
| DriverReturnNotes | string | MaxLength(500), nullable — şoförün teslim sırasında girdiği not (örn. "Sol arka çamurluk çizildi", "Yarım depo teslim edildi") |
| StartReportedBySelf | bool | Default false — Şoför kendi StartAssignment'ı bildirdiyse true. `Kullanici`/`Admin` başlattıysa false (zaten kesinleşmiş sayılır) |
| StartApprovedByUserId | string | Nullable, FK → ApplicationUser.Id — Şoför bildirimini onaylayan Kullanici/Admin. StartReportedBySelf=false ise null (onaya gerek yok) |
| EndReportedBySelf | bool | Default false — Şoför kendi EndAssignment'ı bildirdiyse true |
| EndApprovedByUserId | string | Nullable, FK → ApplicationUser.Id — Bitiş bildirimini onaylayan Kullanici/Admin |

**Onay akışı iş kuralı (kritik):**
- Şoför Başlat/Bitir yaparsa: `Assignment.Status` **DEĞİŞMEZ** (halihazırda Planned ise Planned kalır, InProgress ise InProgress kalır), sadece `ActualStartTime`/`StartKm` (veya `ActualEndTime`/`EndKm`/`DriverReturnNotes`) ile ilgili `*ReportedBySelf = true` set edilir. `Vehicle.Status`/`Vehicle.CurrentKm` de henüz güncellenmez.
- `Kullanici`/`Admin` Başlat/Bitir yaparsa: Mevcut mantık aynen çalışır — `Status` anında değişir, `Vehicle.Status`/`CurrentKm` güncellenir, `*ApprovedByUserId` kendi Id'siyle set edilir.
- `Kullanici`/`Admin`, şoförün bildirdiği (`*ReportedBySelf = true` ama `*ApprovedByUserId = null`) bir kaydı **onayla** dediğinde: O an `Status` gerçekten değişir, `Vehicle` güncellenir, `*ApprovedByUserId` set edilir. Şoförün girdiği saat/KM/not değerleri **korunur**, üzerine yazılmaz (onaylayan sadece doğruluğunu teyit eder).

### 3.2 enum AssignmentStatus
`Planned` (Planlandı) → `InProgress` (Devam Ediyor) → `Completed` (Tamamlandı)
`Planned` → `Cancelled` (İptal Edildi)

**Durum geçiş kuralları (Service katmanında zorunlu):**
1. Yeni görev oluşturulurken **çifte rezervasyon kontrolü**: aynı `VehicleId` için, `PlannedStartTime`–`PlannedEndTime` aralığı çakışan ve `Status` ∈ {Planned, InProgress} olan başka bir Assignment varsa → hata döndür, kayıt oluşturma.
2. Aynı kontrol `DriverId` için de yapılır — bir şoför aynı anda iki göreve atanamaz.
3. `Status = InProgress` olduğunda ilgili `Vehicle.Status = OnDuty` yapılır.
4. `Status = Completed` olduğunda `Vehicle.Status = Idle`, `Vehicle.CurrentKm = EndKm` (EndKm doluysa) güncellenir.
5. `EndKm`, `StartKm`'den küçük olamaz (KM validasyonu).
6. `Cancelled` yapılabilmesi için `Status` hâlâ `Planned` olmalı — `InProgress` bir görev doğrudan iptal edilemez, önce tamamlanmalı.

---

## 4. BAKIM KATMANI

### 4.1 Maintenance (Bakım Kaydı — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir, ayrıca yazma |
| VehicleId | int | FK → Vehicle.Id, Required |
| MaintenanceType | string | Required, MaxLength(100) — örn. "Periyodik Bakım", "Lastik Değişimi" |
| Description | string | MaxLength(500), nullable |
| StartDate | DateTime | Required |
| EndDate | DateTime | Nullable — bakım bitince doldurulur |
| PerformedBy | string | MaxLength(150) — servis/atölye adı |

**İş kuralı:** Bir `Maintenance` kaydı `EndDate = null` (yani devam ediyor) iken, o araca yeni `Assignment` **açılamaz** — Service katmanında bu kontrol de eklenmeli. `Maintenance` başlarken `Vehicle.Status = Maintenance`, `EndDate` girilince `Vehicle.Status = Idle` olur.

---

## 5. DENETİM (AUDIT) KATMANI

### 5.1 AuditLog (`BaseAuditableEntity`'den TÜREMEZ — değiştirilemez/immutable kayıt, kendi Id ve ChangedAt alanına sahiptir)
| Alan | Tip | Kısıtlama |
|---|---|---|
| Id | int | PK |
| EntityName | string | Required, MaxLength(100) — örn. "Vehicle", "Assignment", "VehicleRequest" |
| EntityId | int | Required |
| Action | enum `AuditAction` | Required — `Create`, `Update`, `Delete` |
| ChangedByUserId | string | **Nullable**, FK → ApplicationUser.Id — anonim/kimliksiz işlemlerde (örn. Araç Talebi formu, bkz. Bölüm 5.2) `null` kalır, kim yaptığı bilgisi `NewValues` JSON'ında (örn. RequesterEmail) tutulur |
| ChangedAt | DateTime | Required, Default UtcNow |
| OldValues | string (nvarchar(max)) | Nullable — JSON |
| NewValues | string (nvarchar(max)) | Nullable — JSON |

> Bu tablo, `Vehicle.CurrentKm`, `Assignment.Status` gibi kritik alanlarda değişiklik olduğunda Service katmanından yazılır. Her CRUD işleminde otomatik yazmaya gerek yok — sadece iş kuralı kritik alanlar için (KM güncelleme, görev atama/iptal, araç durum değişimi).

### 5.2 VehicleRequest (Araç Talebi — `BaseAuditableEntity`'den türer)
Üniversitenin diğer birimlerindeki personelin (sisteme kayıtlı olmayan kişilerin), giriş yapmadan araç talep edebildiği anonim form kaydı.

| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir. `CreatedBy` bu tabloda talep edenin e-postası ile doldurulur (kimlik doğrulaması olmadığı için) |
| RequesterFullName | string | Required, MaxLength(150) |
| RequesterEmail | string | Required, MaxLength(150) — **@duzce.edu.tr uzantısı zorunlu** (hem client hem server regex ile doğrulanır). Bildirim göndermek için gereklidir, ama bu kişiler için Identity/AspNetUsers'da hesap AÇILMAZ — sadece bu tabloda bir veri alanı olarak durur. |
| RequesterPhone | string | Required, MaxLength(20) |
| Purpose | string | Required, MaxLength(500) — talep gerekçesi/görev detayı |
| Status | enum `VehicleRequestStatus` | Required, Default `Pending` — `Pending`, `Fulfilled`, `Rejected` |
| FulfilledAssignmentId | int? | Nullable, FK → Assignment.Id, Restrict — talep karşılandığında oluşturulan gerçek görev |
| FulfilledByUserId | string? | Nullable, FK → ApplicationUser.Id, Restrict — karşılayan/reddeden Kullanici/Admin |
| FulfilledAt | DateTime? | Nullable |
| RejectionReason | string? | MaxLength(500), nullable |

**İş kuralı:** `Create` action'ı `[AllowAnonymous]`'dır, kimlik doğrulaması gerektirmez — formda araç/şoför listesi gibi hiçbir iç sistem verisi gösterilmez. `Kullanici`/`Admin`, `Fulfill` işleminde mevcut `AssignmentService.CreateAssignmentAsync`'i çağırarak gerçek bir `Assignment` oluşturur (tüm çakışma/ehliyet/bakım kontrolleri aynen işler), sonra `IEmailService` ile talep sahibine bildirim gönderilir (bkz. Bölüm 5.3).

### 5.3 E-posta Bildirim Altyapısı (IEmailService)
- `IEmailService` arayüzü: `SendEmailAsync`, `SendRequestFulfilledEmailAsync`, `SendRequestRejectedEmailAsync`.
- `ConsoleEmailService` (Development): gerçek mail göndermez, konsola/log'a simülasyon çıktısı yazar.
- `SmtpEmailService` (Production): .NET yerleşik `System.Net.Mail.SmtpClient` kullanır (harici paket yok), ayarlar `appsettings.json > EmailSettings`'ten okunur. Host/kimlik bilgisi boşsa hata fırlatmadan `LogWarning` ile atlanır — mail gönderilemese bile iş akışı (Assignment oluşturma, Status güncelleme) kesintiye uğramaz.

---

## 6. İLİŞKİ ÖZETİ (ER Diyagramı Metinsel)

```
ApplicationUser (1) ──── (1) Driver
Driver (1) ──── (N) Assignment
Vehicle (1) ──── (N) Assignment
Vehicle (1) ──── (N) Maintenance
ApplicationUser (1) ──── (N) AuditLog  (nullable — anonim işlemlerde bağlantısız)
VehicleRequest (0..1) ──── (1) Assignment  (FulfilledAssignmentId, karşılandığında)
ApplicationUser (1) ──── (N) VehicleRequest  (FulfilledByUserId, karşılayan/reddeden)
```

---

## 7. INDEX VE KISITLAMA ÖNERİLERİ

- `Vehicle.PlateNumber` → Unique Index
- `Driver.UserId` → Unique Index
- `Assignment`: `(VehicleId, PlannedStartTime, PlannedEndTime)` üzerine composite index — çakışma sorgusu sık çalışacağı için performans amaçlı.
- `Assignment`: `(DriverId, PlannedStartTime, PlannedEndTime)` üzerine composite index.
- Silme davranışları: `Assignment → Vehicle` ve `Assignment → Driver` ilişkilerinde **Restrict** (geçmiş görev kayıtları asla cascade ile silinmesin).

---

## 8. KLASÖR / PROJE YAPISI ÖNERİSİ

```
/Models
  /Entities        → Vehicle.cs, Driver.cs, Assignment.cs, Maintenance.cs, AuditLog.cs, ApplicationUser.cs
  /Enums           → VehicleType.cs, FuelType.cs, VehicleStatus.cs, AssignmentStatus.cs, AuditAction.cs
/Data
  ApplicationDbContext.cs
  /Configurations   → Fluent API konfigürasyonları (IEntityTypeConfiguration<T>), her entity için ayrı dosya
/Services
  IVehicleService.cs / VehicleService.cs
  IAssignmentService.cs / AssignmentService.cs   → çifte rezervasyon kontrolü burada
  IAuditService.cs / AuditService.cs
/Controllers
  VehicleController.cs, DriverController.cs, AssignmentController.cs (Admin)
  DriverPanelController.cs (Şoför mobil ekranları)
```

---

## 9. MIGRATION SIRASI (ÖNERİ — onay her adımda ayrı ayrı alınacak)

1. Identity tabloları + `Driver` (ApplicationUser'a bağımlı olduğu için Identity kurulumundan sonra)
2. `Vehicle`
3. `Assignment` (Vehicle ve Driver'a bağımlı)
4. `Maintenance` (Vehicle'a bağımlı)
5. `AuditLog` (ApplicationUser'a bağımlı)
6. Seed data: `Admin` ve `Driver` rollerini ve bir test Admin kullanıcısını `OnModelCreating` veya ayrı bir seed sınıfında oluştur.

> Hatırlatma (AGENTS.md Bölüm 2.2 gereği): Her migration adımı için önce `dotnet ef migrations add <İsim>` komutu ve isim önerisi sunulur, onay olmadan çalıştırılmaz. `database update` hiçbir zaman otomatik yapılmaz.

---

# EK GÜNCELLEME SETİ (v2) — Genişletilmiş Modüller

> Bu bölüm, ilk sürümden sonra eklenen ve gerçek kod üzerinde doğrulanmış tüm yeni veri modeli değişikliklerini içerir. Migration geçmişi Bölüm 9'da (Ek) listelenmiştir.

## 6. ROLLER — GÜNCEL DURUM (3 Rol)

| Rol | Kapsam | Varsayılan Yönlendirme |
|---|---|---|
| `Admin` | Tam yetki: Vehicle/Driver/Assignment/Maintenance/VehicleRequest/Report CRUD + **Sef (Kullanici) yönetimi** + **AuditLog** (Admin'e özel). | `/Home` (Dashboard) |
| `Kullanici` (Ulaşım Şefi) | Admin ile aynı CRUD yetkileri, **Sef yönetimi ve AuditLog HARİÇ**. Görev onaylama, doğrudan başlat/bitir, talep karşılama. | `/Home` (Dashboard) |
| `Driver` (Şoför) | Sadece kendi görevlerini görür, başlangıç/bitiş **bildirir** (onaya tabi), mobil şifre değiştirme. Yönetim panellerine erişimi yok. | `/Surucu` |

**Login yönlendirme kuralı (düzeltildi):** `ReturnUrl` parametresi artık kullanıcının rolüyle uyumlu değilse yok sayılır — bir Driver, Admin sayfasına ait bir `ReturnUrl` ile asla yönlendirilmez, direkt `/Surucu`'ya düşer (ve tam tersi).

## 7. Vehicle — GENİŞLETİLMİŞ ALANLAR

`VERI-MODELI.md` Bölüm 2.1'deki temel alanlara ek olarak:

| Alan | Tip | Kısıtlama |
|---|---|---|
| InspectionStartDate | DateTime | Required — Fenni muayene geçerlilik başlangıcı (yeni yapıyla senkron tutulur) |
| InspectionExpiryDate | DateTime | Required — Fenni muayene bitiş tarihi (yeni yapıyla senkron tutulur) |
| InsuranceStartDate | DateTime | Required — Trafik sigortası başlangıcı (yeni yapıyla senkron tutulur) |
| InsuranceExpiryDate | DateTime | Required — Trafik sigortası bitiş tarihi (yeni yapıyla senkron tutulur) |
| TireBrand | string | MaxLength(50), nullable — Lastik marka/model |
| TireYear | int | Nullable — Lastik üretim yılı (DOT) |
| TireSeason | enum `TireSeason` | Nullable — `Summer` (Yazlık) / `Winter` (Kışlık) |
| FineNotes | string | MaxLength(1000), nullable — Ceza/Şef notu (serbest metin, harici sistem entegrasyonu YOK) |

### 7.1 VehicleDocument (Araç Belgeleri Geçmişi — `BaseAuditableEntity`'den türer - Faz 5-A)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| VehicleId | int | FK → Vehicle.Id, Required (Cascade delete) |
| DocumentType | enum `VehicleDocumentType` | Required — `Inspection` (Muayene), `Insurance` (Trafik Sigortası), `Kasko` (Kasko) |
| StartDate | DateTime | Required — Belge / poliçe başlangıç tarihi |
| EndDate | DateTime | Required — Belge / poliçe son geçerlilik tarihi |
| Cost | decimal(18,2) | Default 0 — Muayene / poliçe ödenen tutar (fiyat) |
| PolicyOrStationNumber | string | MaxLength(100), nullable — Poliçe No veya Muayene İstasyon No |
| CompanyName | string | MaxLength(100), nullable — Sigorta şirketi veya Muayene kuruluşu adı |
| Notes | string | MaxLength(500), nullable — Açıklama ve notlar |

**Eski Kolonlar Güvenlik Stratejisi (Faz 5-A):** `Vehicle` tablosundaki `InspectionExpiryDate` ve `InsuranceExpiryDate` eski kolonları geçiş güvenliği için silinmemiştir; yeni eklenen `VehicleDocuments` tablosundaki en güncel aktif kayıtlara göre otomatik senkron tutulmaktadır.

**VehicleStatus enum'una eklenen yeni değer:** `DocumentExpired` (Belgesi Geçersiz) — Muayene veya Sigorta tarihi geçtiğinde otomatik atanır.

**Otomatik durum yönetimi (`VehicleDocumentExpirationWorker` — BackgroundService):**
- Uygulama açılışında ve her 24 saatte bir çalışır.
- Süresi geçmiş (`Idle`/`Maintenance`/`OutOfService` durumundaki) araçları `DocumentExpired`'a çeker. **`OnDuty` (görevdeki) araçlara dokunmaz**, görev bitene kadar bekler.
- **Kurtarma kuralı:** `DocumentExpired` bir aracın tarihleri güncellenip geçerli hale gelirse, otomatik olarak `Idle` (veya açık bakımı varsa `Maintenance`) durumuna geri döner. Bu kural hem arka plan servisinde hem `VehicleService.SyncVehicleLegacyDocumentColumnsAsync` ve `UpdateVehicleAsync`'te çalışır — **sadece `Status == DocumentExpired` olan araçlar için**, diğer durumlara (`OnDuty`, `Maintenance`, `OutOfService`) asla müdahale etmez.
- Her durum değişikliğinde `AuditLog`'a düşer.

**Assignment/Görev ataması sırasında ek kontrol:** `AssignmentService.ValidateVehicleOperationalStatus`, `Status` alanına ek olarak **doğrudan tarihe bakarak** da kontrol yapar (`InspectionExpiryDate`/`InsuranceExpiryDate < bugün ise reddet`) — böylece `Status` alanı henüz güncellenmemiş olsa bile güvenlik boşluğu oluşmaz.

## 8. Assignment — GENİŞLETİLMİŞ ALANLAR

Mevcut alanlara (Bölüm 3.1) ek olarak:

**Konaklama ve Harcırah Modeli (Manuel / Hibrit Hesaplama):**
| Alan | Tip | Açıklama |
|---|---|---|
| AccommodationNights | int, nullable | Otel/konaklama gece sayısı |
| AccommodationCost | decimal(18,2), nullable | Konaklama fatura bedeli |
| PerDiemType | string, MaxLength(20) | "Hourly" (Saatlik) veya "Daily" (Günlük) harcırah türü |
| PerDiemHours | decimal(18,2), nullable | Görev saati |
| PerDiemRate | decimal(18,2), nullable | Saatlik/günlük harcırah birim katsayısı |
| PerDiemDays | int, nullable | Günlük görev gün sayısı |
| PerDiemAmount | decimal(18,2), nullable | Hesaplanan/girilen harcırah tutarı |
| ExpenseNotes | string, MaxLength(500), nullable | Görev harcama notları ve süre uzatma damgaları |

> **Ücret Terminolojisi Standardı:** Sistemde eski "Yevmiye" ve "Kadrolu Personel" ayrımı tamamen kaldırılmıştır. Tüm şoförler üniversitenin resmi personeli olarak ele alınır; yapılan ödemeler arayüz ve raporlarda **"Görev Ücreti"** ve **"Harcırah"** olarak adlandırılır.

**Şoför Görev Ücreti (Kısmen Otomatik Hesaplama):**
| Alan | Tip | Açıklama |
|---|---|---|
| HourlyRate | decimal(18,2), nullable | Görev onayı (`ApproveEnd`) sırasında Şef/Admin **serbestçe elle girer**, sabit/varsayılan değer YOKTUR. |
| CalculatedDutyHours | decimal(18,2), nullable | Otomatik hesaplanır: `(OnaylananBitiş − OnaylananBaşlangıç).TotalHours` |
| TotalDriverFee | decimal(18,2), nullable | Otomatik hesaplanır: `CalculatedDutyHours × HourlyRate` |

> **Harcırah ve Maliyet Revizyonu:** Görev tamamlandıktan (`Status == Completed`) sonra geç intikal eden otel faturası veya harcama belgeleri için Şef/Admin `AssignmentController.UpdateFinancialDetails` üzerinden yalnızca maliyet alanlarını revize edebilir. Kilometre, fiili sefer saati ve görev durumu kilitlidir; her revizyon zorunlu gerekçe (`RevisionReason`) ile `AuditLog`'a işlenir.

**Süre Uzatma Mekanizması (`ExtendDuration` — InProgress Görevler):**
- Seyir halindeki (`InProgress`) veya planlanan görevlerin planlanan dönüş tarihi Şef/Yönetici tarafından ileri bir tarihe uzatılabilir.
- **Çakışma Kontrolü:** Yeni uzatılan tarih aralığında araç veya şoförün başka görevi varsa sistem işlemi reddeder.
- **Veri Damgası:** `Assignment.PlannedEndTime` güncellenir; gerekçe metni `Assignment.ExpenseNotes` alanına `[Süre Uzatma - dd.MM.yyyy HH:mm]: {gerekçe}` formatında eklenir.
- **Denetim İzi:** İşlem `AuditLog` tablosuna `Update` eylemi ve detaylı notla kaydedilir.

**Resmi Belge Alanları:**
| Alan | Tip | Açıklama |
|---|---|---|
| SignedDocumentPath | string, MaxLength(500), nullable | Diskte fiziksel dosya yolu (`App_Data/AssignmentDocuments/` altında, wwwroot dışında) |
| SignedDocumentOriginalFileName | string, MaxLength(255), nullable | Orijinal dosya adı |
| SignedDocumentUploadedAt | DateTime, nullable | Yükleme zaman damgası |
| SignedDocumentUploadedByUserId | string, nullable | FK → ApplicationUser, Restrict |

**Onay akışı:** v1'de tanımlanan `StartReportedBySelf` / `EndReportedBySelf` / `*ApprovedByUserId` alanları aynen geçerlidir.

---

## 9. VehicleRequest — GÜNCEL ALAN (Ek)

`RequestedByDepartment`'a ek olarak Talep formuna **`Department`** alanı eklendi (Fakülte/Birim seçimi, `<datalist>` ile otomatik tamamlama — Düzce Üniversitesi'nin tüm birimlerini içerir).

---

## 10. Sef (Kullanici) Yönetimi — Yeni Modül

Ayrı bir entity YOKTUR — `Kullanici` rolündeki kullanıcılar sadece `ApplicationUser` + rol ataması ile yönetilir (Driver'daki gibi ek bir tablo yok, çünkü Şef'in ehliyet bilgisi gibi ek alanlara ihtiyacı yok).

- Sadece **Admin** yeni Şef oluşturabilir/yönetebilir/pasife alabilir (`[Authorize(Roles = "Admin")]`) — Şef'in başka Şef oluşturması (yetki yükseltme riski) engellenir.
- `@duzce.edu.tr` kurumsal e-posta zorunluluğu.
- **Soft-delete zorunlu** — kalıcı silme (hard delete) denenmiş ama `AuditLog.ChangedByUserId` (Restrict FK) çakışması riski nedeniyle **soft-delete'e (`IsActive = false`) çevrilmiştir.** Bu, sistemin geri kalanıyla (Vehicle/Driver soft-delete) tutarlıdır.
- Admin, kendi hesabını pasife alamaz (kilitlenme riskine karşı).

---

## 11. Kimlik Doğrulama Güvenlik Notları

- **DataProtection anahtarları** artık `App_Data/DataProtection-Keys/` klasöründe kalıcı tutulur — uygulama yeniden başlasa bile kullanıcı oturumları bozulmaz.
- **DbInitializer, var olan kullanıcıların şifresine ASLA dokunmaz** — sadece kullanıcı ilk kez oluşturulurken şifre atanır, sonraki her çalıştırmada sadece eksik rol ataması tamamlanır.
- Admin/Şef, bir şoförün şifresini modal üzerinden sıfırlayabilir (`Driver/Details` ekranından).
- Şoför, kendi şifresini mobil panelden değiştirebilir (`/Surucu/ChangePassword`).
- `SecurityStampValidator.ValidationInterval` 8 saate ayarlandı (gereksiz sık oturum doğrulaması önlendi).

---

## 12. SVG CAPTCHA VE DURUMSUZ (STATELESS) GÜVENLİK MODELİ

- **Veritabanı Tablosu Gerektirmez:** Güvenlik kodu doğrulaması için veritabanında ayrı bir tablo tutulmaz.
- **Şifreli Token Yapısı:** Sunucu tarafında üretilen 5 karakterli güvenlik kodu, `IDataProtector` kullanılarak `CAPTCHA:{kod}:{sonGecerlilikZamani}:{tekilGuvenlikKimligi}` payload formatında şifrelenir ve istemciye `CaptchaToken` gizli alanı olarak iletilir.
- **Zaman Sınırı (TTL):** Her token üretim anından itibaren tam **3 dakika** geçerlidir; süresi geçen token'lar doğrudan geçersiz sayılır.
- **Tek Kullanımlık (Nonce):** Botların ve replay saldırılarının önüne geçmek için her kod rastgele `nonce` ile imzalanır. Form gönderildiğinde token çözülür, girilen kod ile çözülen kod karşılaştırılır.

---

## 13. FAZ 3 GÜNCELLEMELERİ — GÖREV EMRİ, ARAÇ KONTROL TİKLERİ VE ARIZA BİLDİRİMİ

### 13.1 Assignment Tablosu Ek Alanları
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| OffRouteDispatcherName | string? | MaxLength(150), nullable — Acil durumlarda güzergah dışına sevk eden yetkili adı soyadı |
| OffRouteDispatcherTitle | string? | MaxLength(100), nullable — Güzergah dışına sevk eden yetkili unvanı |
| OffRouteReason | string? | MaxLength(500), nullable — Güzergah dışına sevk gerekçesi |
| OffRouteReportedAt | DateTime? | Nullable — Sisteme işlenme tarihi |
| OffRouteReportedByUserId | string? | Nullable, FK → ApplicationUser.Id — Sisteme işleyen Şef/Admin |
| CheckOil | bool | Default false — Yağ kontrolü yapıldı mı? |
| CheckWater | bool | Default false — Su kontrolü yapıldı mı? |
| CheckFuel | bool | Default false — Yakıt kontrolü yapıldı mı? |
| CheckHydraulic | bool | Default false — Hidrolik kontrolü yapıldı mı? |
| CheckTires | bool | Default false — Lastik kontrolü yapıldı mı? |
| ChecksCompletedAt | DateTime? | Nullable — Kontrol tiklerinin şoför tarafından kaydedildiği tarih/saat |

### 13.2 VehicleFaultReport (Araç Arıza Bildirimi — `BaseAuditableEntity`'den türer)
Şoförlerin mobil panelden veya Şeflerin yönetim panelinden bildirdiği araç arıza ve kusur kayıtları.

| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| VehicleId | int | Required, FK → Vehicle.Id (Restrict) |
| DriverId | int | Required, FK → Driver.Id (Restrict) |
| AssignmentId | int? | Nullable, FK → Assignment.Id (Restrict) |
| CurrentKm | int | Required — Bildirim anındaki araç kilometresi |
| Description | string | Required, MaxLength(2000) — Arıza detayı ve şoför açıklaması |
| ReportDate | DateTime | Required, Default UtcNow — Bildirim tarihi |
| Status | enum `FaultReportStatus` | Required, Default `Pending` (`Pending`, `Approved`, `Resolved`, `Rejected`) |
| ReviewerUserId | string? | Nullable, FK → ApplicationUser.Id — İnceleyen/onaylayan Şef/Admin |
| ReviewerNotes | string? | Nullable, MaxLength(1000) — Şef inceleme notu ve sevk açıklaması |
| ReviewedAt | DateTime? | Nullable — İnceleme/onay tarihi |

> **İş Kuralı:** Arıza bildirimi açılması aracın `Vehicle.Status` durumunu **otomatik olarak değiştirmez**. Araç durumu mevcut idari kurallarla (Bakım kaydı açılarak vb.) yönetilir.

---

## 14. FAZ 2 - A KISMI GÜNCELLEMELERİ — ÇOKLU ŞOFÖR, ŞOFÖRSÜZ GÖREV, DIŞ ŞOFÖR VE GÖREV YERİ/GÜZERGAH

### 14.1 Assignment Tablosu Ek ve Güncellenen Alanları
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| `Destination` | string? | MaxLength(250), nullable — Görev Yeri (serbest metin adres). Eski kayıtlar için nullable kalır; yeni kayıtlarda form seviyesinde zorunludur. |
| `Route` | string? | MaxLength(500), nullable — Güzergah (manuel serbest metin). |
| `IsDriverless` | bool | Default false — Şoförsüz görevlendirme (personel kullanımı). |
| `IsExternalDriver` | bool | Default false — Kurum dışı şoför görevlendirme. |
| `ExternalDriverFullName` | string? | MaxLength(150), nullable — Kurum dışı şoför Adı Soyadı. |
| `ExternalDriverDepartment` | string? | MaxLength(150), nullable — Kurum dışı şoförün bağlı olduğu birim/kurum. |
| `DriverId` | int? | **Nullable** — Asil / 1. Şoför referansı. Şoförsüz veya dış şoförlü görevlerde `null` olur. |

### 14.2 AssignmentDriver (Çoklu Şoför Ara Tablosu — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| `AssignmentId` | int | Required, FK → Assignment.Id (Restrict) |
| `DriverId` | int | Required, FK → Driver.Id (Restrict) |
| `IsPrimary` | bool | Default false — 1. (Asil) Şoför mü? |

- **İndeks:** `(AssignmentId, DriverId)` üzerinde filtrelenmiş unique index (`[IsDeleted] = 0`).
- **Üç Şoför Modu (Birbirini Kesin Olarak Dışlar):**
  1. *Kurum Şoförlü:* 1 veya birden fazla kurum şoförü (`AssignmentDrivers.Count >= 1`, `DriverId = ilk şoför`).
  2. *Kurum Dışı Şoför:* Yalnızca dış şoför bilgisi girilir (`IsExternalDriver = true`, `DriverId = null`, `AssignmentDrivers` boş).
  3. *Şoförsüz:* Araç personelde (`IsDriverless = true`, `DriverId = null`, `AssignmentDrivers` boş).

---

## 15. FAZ 2 - B KISMI GÜNCELLEMELERİ — GÜZERGAH DEĞİŞİKLİĞİ VE ŞEF KM DÜZELTMESİ

### 15.1 AssignmentRouteChange (Sefer Sırasında Güzergah Değişikliği — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| `AssignmentId` | int | Required, FK → Assignment.Id (Cascade) |
| `OldRoute` | string? | MaxLength(500), nullable — Değişiklik öncesi güzergah |
| `NewRoute` | string | Required, MaxLength(500) — Şoförün bildirdiği yeni güzergah |
| `Reason` | string | Required, MaxLength(1000) — Değişiklik gerekçesi / açıklaması |
| `ChangedByDriverId` | int? | Nullable, FK → Driver.Id (Restrict) — Güzergahı değiştiren şoför |

- **İndeks:** `AssignmentId` üzerinde yabancı anahtar indeksi.
- **İş Kuralı:** Yalnızca `InProgress` durumundaki görevlerde, göreve atanmış şoförler (asil veya yedek) güzergah değiştirebilir. Atanmamış şoförler için yetkisiz işlem (HTTP 403) uygulanır. Sefer sırasında birden çok kez değiştirilebilir.
- **Raporlama Kısıtı:** Güzergah değişiklik geçmişi kurum PDF çıktılarına basılmaz, yalnızca Admin/Şef detay ekranında görüntülenir.

### 15.2 Şef KM Düzeltmesi ve Doğrulama
- **ViewModel Alanları:** `AssignmentApproveEndViewModel` içerisine `ConfirmedStartKm` (int?) ve `ConfirmedEndKm` (int?) eklenmiştir.
- **İş Kuralı:** Şef görev onaylarken şoförün bildirdiği başlangıç ve/veya bitiş KM'sini düzeltebilir (`ConfirmedEndKm >= ConfirmedStartKm`).
- **Veri Tutarlılığı (KM Güncelleme Kuralı):** `Vehicle.CurrentKm` değeri SADECE şu iki koşul birlikte doğruysa güncellenir: (a) bu görev aracın en son tamamlanan görevi, (b) yeni değer mevcut `CurrentKm`'den büyük. Aksi halde araç KM'sine dokunulmaz.
- **AuditLog:** Eski ve yeni KM değerleri `AuditLog` tablosuna `Action = Update`, `EntityName = Assignment` olarak kaydedilir.
- **Eşik Uyarısı:** Yapılan mesafe (`EndKm - StartKm`) `appsettings.json > VehicleSettings:TripDistanceThreshold` (200 KM) eşiğini aştığında kırmızı ünlem ikonlu uyarı verilir.

---

## 16. FAZ 5 - B KISMI GÜNCELLEMELERİ — LASTİK YÖNETİMİ VE TAKİBİ

### 16.1 VehicleTire (Araç Lastik Kaydı — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| `VehicleId` | int | Required, FK → Vehicle.Id (Cascade) |
| `Brand` | string | Required, MaxLength(100) — Lastik markası (örn: Michelin, Continental, Lassa) |
| `Size` | string? | Nullable (eski kayıtlarda boş, yeni kayıtlarda zorunlu form doğrulaması), MaxLength(50) — Ebat (örn: 205/55 R16) |
| `DotDate` | string? | Nullable, MaxLength(10) — DOT kodu ("52/23" hafta 01-53, yıl iki hane) |
| `ProductionYear` | int? | Nullable — DOT kodundan türetilen 4 haneli üretim yılı |
| `Season` | string | Required, MaxLength(30) — Mevsim (Yaz / Kış / Dört Mevsim) |
| `InstalledDate` | DateTime? | Nullable — Takılma / değişim tarihi |
| `InstalledKm` | int? | Nullable — Takıldığı andaki araç kilometresi |
| `RecommendedReplaceKm` | int? | Nullable — Değiştirilmesi gereken hedef araç KM'si |
| `RecommendedReplaceDate` | DateTime? | Nullable — Değiştirilmesi gereken hedef takvim tarihi |
| `Cost` | decimal | Required, Precision(18,2), Default 0 — Lastik satın alma/değişim maliyeti |
| `Notes` | string? | Nullable, MaxLength(500) — Açıklama ve notlar |

- **İndeks:** `VehicleId` üzerinde yabancı anahtar indeksi.
- **Kullanım KM'si Hesabı:** Kayıt anında dondurulmaz; her görüntülemede `Vehicle.CurrentKm - InstalledKm` formülüyle anlık hesaplanır. `InstalledKm` boş ise arayüzde "Bilinmiyor" olarak gösterilir.
- **Tek Doğru Kaynak ve Senkronizasyon:** `VehicleTires` tablosu lastik yönetimi için tek doğru kaynaktır. `Vehicle.TireBrand`, `TireSeason`, `TireYear` eski kolonları geriye dönük uyumluluk için güncel (en son takılan) lastik kaydıyla senkron tutulur.
- **Silme Kısıtı (Soft Delete):** Bir aracın tek (son) aktif lastik kaydı silinemez.

## 17. FAZ 7 - A GÜNCELLEMELERİ — BAKIM MALİYETİ VE RAPORLAMA

### 17.1 Maintenance (Araç Bakım / Servis Kaydı Güncellemesi)
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| `Cost` | decimal? | Nullable, Precision(18,2) — Bakım / servis işlem maliyeti (₺). Opsiyoneldir. Mevcut kayıtlarda null kalır. |

- **İş Kuralı:** Bakım ekleme ve düzenleme formlarında girilebilir. Raporlarda boş bırakılan maliyetler "-" gösterilir ve rapor sonundaki genel toplam maliyet hesabına katılmaz.
- **Raporlama Bütünlüğü:** Bakım ve Onarım Raporu (`Report/Maintenance`) ve PDF çıktısında satır bazında maliyet ve filtrelenen sonuçların Genel Toplam Bakım Maliyeti gösterilir.
- **Kullanım Raporu:** Satır bazında süre (`DurationHelper`, "2 sa 40 dk"), şoför bazlı gruplu alt toplamlar ("Genel Toplam Süre", "Genel Toplam Mesafe") ve rapor sonunda tüm görevler için tekil genel toplam (çoklu şoförlü görevler yalnızca bir kez sayılır).
- **Sigorta & Muayene Raporu:** `VehicleDocuments` tablosundan ödenen tutarlar (Sigorta, Muayene, Kasko) ve genel toplam belge harcamaları raporlanır. Para birimleri `1.250,00 ₺` formatında gösterilir.

---

## 18. FAZ 6 GÜNCELLEMELERİ — ARAÇ TALEP FORMU VE YÖNETİM SEÇENEKLERİ

### 18.1 RequestDepartment (Talep Birim / Fakülte Seçenekleri — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| `Name` | string | Required, MaxLength(150) — Birim / Fakülte / Daire adı |
| `DisplayOrder` | int | Required, Default 0 — Açılır listedeki sıralama |
| `IsActive` | bool | Required, Default true — Aktiflik durumu (pasife alma desteği) |

### 18.2 RequestVehicleType (Talep Edilebilir Araç Türleri — `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir |
| `Name` | string | Required, MaxLength(100) — Araç türü tanımı (Sedan, Minibüs, Otobüs, Kamyon vb.) |
| `DisplayOrder` | int | Required, Default 0 — Sıralama |
| `IsActive` | bool | Required, Default true — Aktiflik durumu |

- **KARAR:** Mevcut teknik `VehicleType` enum'undan bağımsızdır. Talep eden personelin seçeceği sınıfları temsil eder.

### 18.3 VehicleRequest Tablosu Faz 6 İlave Alanları
| Alan | Tip | Kısıtlama / Açıklama |
|---|---|---|
| `RequestedVehicleType` | string? | Nullable, MaxLength(100) — Formda seçilen araç türü adı. Eski kayıtlarda null kalır. |
| `PassengerCount` | int? | Nullable — Kaç kişi seyahat edileceği (yolcu sayısı). Eski kayıtlarda null kalır. |
| `RequestedStartTime` | DateTime? | Nullable — Talep edilen görev başlangıç tarih ve saati. |
| `RequestedEndTime` | DateTime? | Nullable — Talep edilen görev bitiş tarih ve saati. |
| `ContactUserId` | string? | Nullable, FK → AspNetUsers.Id — Talep onay e-postasında yer alacak iletişim kişisi (Ulaşım Şefi / Şoför). |
| `ContactFullName` | string? | Nullable, MaxLength(150) — İletişim kişisi adı soyadı (kayıt anındaki snapshot). |
| `ContactPhone` | string? | Nullable, MaxLength(30) — İletişim kişisi telefon numarası. |
| `ContactEmail` | string? | Nullable, MaxLength(150) — İletişim kişisi kurumsal e-posta adresi. |

- **E-Posta Gizlilik Kuralı:** Talep onay e-postasında görevlendirilen şoförün yalnızca ADI gösterilir; şoförün telefon ve e-postası gizlenir. Karşı tarafla irtibat için seçilen İletişim Kişisinin ad, telefon ve e-posta bilgileri gösterilir.
- **Geçiş Planı:** Eski talep kayıtlarındaki serbest metin `Department` değerleri korunmuştur (32 kaydın 23'ünde mevcut). Yeni alanlar (`RequestedVehicleType`, `PassengerCount`, `RequestedStartTime`, `RequestedEndTime`) eski kayıtlarda null bırakılmıştır; uydurma değer yazılmamıştır. Form artık sadece admin tanımlı aktif `RequestDepartments` listesinden seçim yaptırır.

---

## 19. FAZ 8 GÜNCELLEMELERİ — PDF ÖNİZLEME MİMARİSİ

- **Veritabanı Etkisi:** Faz 8 kapsamında yeni veritabanı tablosu veya kolonu eklenmemiştir (veri modeli değişmemiştir).
- **HTTP İçerik Dağıtımı (`Content-Disposition`):**
  - Tüm PDF uç noktaları isteğe bağlı `preview=true` parametresi alır.
  - `preview=false` (varsayılan): `Content-Disposition: attachment; filename="..."` başlığıyla tarayıcıya doğrudan dosya indirme yaptırılır.
  - `preview=true`: `Content-Disposition: inline; filename="..."` başlığıyla `application/pdf` gövdesi döndürülür, tarayıcının yerleşik PDF görüntüleyicisi veya modal iframe içinde sayfa yenilenmeden açılması sağlanır.
- **Yetki Bütünlüğü:** Önizleme için ayrı bir endpoint veya anonim erişim açılmaz; mevcut [Authorize] ve yatay yetki kuralları (örn. şoförün yalnızca kendi görev emrine erişebilmesi) aynen korunur.

---

## 20. FAZ 7-B GÜNCELLEMELERİ — TALEP EDEN BİRİMLER BAZINDA ARAÇ TALEPLERİ RAPORU

- **Amaç:** Talep eden birimler bazında araç talep istatistiğinin (Toplam, Karşılanan, Reddedilen, Bekleyen/Diğer) raporlanması ve QuestPDF ile kurumsal PDF dökümü.
- **Veritabanı Etkisi:** Yeni tablo veya kolon eklenmemiştir. `VehicleRequests` tablosundaki mevcut veriler üzerinden dinamik LINQ/bellek projeksiyonu kullanılır.
- **Tarih Filtresi Mantığı:**
  - Eski talep kayıtlarında `RequestedStartTime/RequestedEndTime` alanları null olabildiği için, tarih aralığı filtresi talep oluşturulma tarihi olan `CreatedAt` üzerinden uygulanır (`filter.StartDate <= CreatedAt <= filter.EndDate`).
- **Birim Adı Eşleme & Normalizasyon Kuralları:**
  - Eski kayıtlar serbest metin, yeni kayıtlar ise admin tanımlı listeden seçilen birimdir.
  - Aynı birim adı iki biçimde geçiyorsa (büyük/küçük harf ve baştaki/sondaki boşluklar hariç) `StringComparer.OrdinalIgnoreCase` ve `Trim()` ile tek bir satırda birleştirilir.
  - Birim bilgisi boş (`null`, empty veya whitespace) olan kayıtlar `"Belirtilmemiş"` satırında toplanır.
  - Eşleşmeyen serbest metin birimler ayrı birer satır olarak korunur; kesinlikle uydurma veya otomatik düzeltme yapılmaz.
- **Durum Eşlemesi (VehicleRequestStatus):**
  - **Karşılanan (Fulfilled):** `VehicleRequestStatus.Fulfilled` (Onaylanıp araç/şoför ataması yapılmış ve görevlendirilmiş talepler).
  - **Reddedilen (Rejected):** `VehicleRequestStatus.Rejected` (İptal edilen veya gerekçe belirtilerek reddedilen talepler).
  - **Bekleyen / Diğer (Pending):** `VehicleRequestStatus.Pending` (İncelenmeyi veya onay/ret kararı bekleyen talepler).
- **Rapor Modelleri (ViewModels):**
  - `VehicleRequestReportFilterViewModel`: `StartDate`, `EndDate`
  - `VehicleRequestReportItemViewModel`: `Department`, `TotalCount`, `FulfilledCount`, `RejectedCount`, `PendingCount`
  - `VehicleRequestReportViewModel`: `Filter`, `Items`, `TotalRequests`, `TotalFulfilled`, `TotalRejected`, `TotalPending`, `ReportDate`

---

## 21. FAZ 9 GÜNCELLEMELERİ — DASHBOARD TAKVİMİ (AYLIK ARAÇ DOLULUĞU VE GÜNLÜK DETAY)

- **Veritabanı Etkisi:** Yeni tablo veya kolon eklenmemiştir. Mevcut `Assignments`, `Maintenances`, `Vehicles`, `VehicleDocuments`, `VehicleTires` ve `DriverLicenses` tabloları üzerinde ay bazlı tek seferlik sorgu mantığıyla (N+1 önlenerek) dinamik projeksiyon kullanılır.
- **Takvim Modelleri (DTOs - `DashboardCalendarViewModels.cs`):**
  - `DashboardCalendarDaySummaryDto`: Günlük özet (tarih, aktif araç sayısı, bakımda araç sayısı, biten belge ve ehliyet sayısı).
  - `DashboardCalendarAssignmentItemDto`: Görev detayları (araç plaka/marka, şoförler, güzergah, amaç, birim, saat, durum).
  - `DashboardCalendarMaintenanceItemDto`: Bakım detayları (araç, bakım türü, servis, tarihler).
  - `DashboardCalendarDocumentItemDto`: Muayene, sigorta, kasko ve lastik geçerlilik son günleri.
  - `DashboardCalendarLicenseItemDto`: Ehliyet süresi son günleri.
  - `DashboardCalendarDayDetailDto`: Seçilen günün tüm detay listeleri.
  - `DashboardCalendarMonthResponseDto`: Aylık takvim yanıtı (`Days`, `DetailsByDate`).

---

## 22. FAZ 1-B GÜNCELLEMELERİ — YETKİ DEVRİ (VEKÂLET) VE DENETİM GÜNLÜĞÜ VEKÂLET İZLEME

### 22.1 `RoleDelegations` Tablosu
Ulaşım Şeflerinin görevlerini geçici olarak bir başka Şefe veya Şoföre devretmesini sağlayan kayıt tablosudur. `BaseAuditableEntity` taban sınıfından türer (`Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`).

| Kolon Adı | Veri Tipi | Kısıtlamalar & Açıklama |
|---|---|---|
| `Id` | `int` | Primary Key, Identity. |
| `DelegatorUserId` | `nvarchar(450)` | Asıl yetkili (Ulaşım Şefi) kullanıcı ID'si. `FK -> AspNetUsers.Id` (`DeleteBehavior.Restrict`). |
| `DelegateeUserId` | `nvarchar(450)` | Görevi devralan vekil kullanıcı ID'si. `FK -> AspNetUsers.Id` (`DeleteBehavior.Restrict`). |
| `StartDate` | `datetime2` | Vekâlet başlangıç tarihi ve saati (UTC). |
| `EndDate` | `datetime2` | Vekâlet planlanan bitiş tarihi ve saati (UTC). |
| `Reason` | `nvarchar(500)` | Yetki devri gerekçesi (zorunlu, maks 500 karakter). |
| `OriginalDelegateeRole` | `nvarchar(50)` | Vekil personelin asıl rolü (`Driver` veya `Kullanici`). |
| `DelegatedRole` | `nvarchar(50)` | Devredilen rol (varsayılan `Kullanici` / Şef). |
| `ActualRevokedAt` | `datetime2` (NULL) | Görevin erken geri alınma / sonlandırılma tarihi (UTC). Boş ise vekâlet planlanan süreye kadar geçerlidir. |
| `RevokedByUserId` | `nvarchar(450)` (NULL) | Yetkiyi sonlandıran kullanıcı ID'si. `FK -> AspNetUsers.Id` (`DeleteBehavior.Restrict`). |
| `RevokeReason` | `nvarchar(500)` (NULL) | Yetki devrini erken sonlandırma gerekçesi. |

- **İndeks:**
  - `IX_RoleDelegations_ActiveCheck`: `(DelegateeUserId, StartDate, EndDate, ActualRevokedAt, IsDeleted)` üzerinde hızlı aktif vekâlet ve çakışma kontrolü bileşik indeksi.
- **Etkin Vekâlet Kriteri:**
  - `ActualRevokedAt IS NULL AND IsDeleted = 0 AND StartDate <= DateTime.UtcNow AND DateTime.UtcNow <= EndDate`
- **Çakışma Kontrolünde Bitiş:** `ActualRevokedAt ?? EndDate`

### 22.2 `AuditLogs` Tablosu Genişletmesi
Vekil bir kullanıcı işlem yaptığında "Ahmet Şef adına Ali Demir tarafından vekâleten yapıldı" denetim izini sağlamak amacıyla iki yeni nullable kolon eklenmiştir:

| Kolon Adı | Veri Tipi | Kısıtlamalar & Açıklama |
|---|---|---|
| `OnBehalfOfUserId` | `nvarchar(450)` (NULL) | Adına işlem yapılan asıl yetkili kullanıcı ID'si. `FK -> AspNetUsers.Id` (`DeleteBehavior.Restrict`). |
| `DelegationId` | `int` (NULL) | İşlemin yapıldığı esnada yürürlükte olan yetki devri kaydı ID'si. `FK -> RoleDelegations.Id` (`DeleteBehavior.Restrict`). |
