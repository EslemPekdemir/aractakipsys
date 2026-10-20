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
- `Admin` — Tam yetki (Vehicle/Driver/Assignment/Maintenance CRUD + AuditLog görüntüleme + kullanıcı yönetimi).
- `Kullanici` — Şoförlerin operasyonel amiri ("Şoför Şefi"). Vehicle/Driver/Assignment/Maintenance CRUD yapabilir (Driver CRUD dahil, yeni şoför hesabı açabilir). **AuditLog'u göremez** — bu sadece Admin'e özel kalır. Görev Başlat/Bitir işlemlerini yapabilir ve şoförün kendi bildirdiği başlat/bitir kayıtlarını **onaylar** (bkz. 3.1, `StartReportedBySelf`/`EndReportedBySelf`).
- `Driver` — Sadece kendi görevlerini görüntüler (salt okunur). Görev başlatma/bitirme bilgisini (saat, KM, dönüş notu) kendi girebilir, ama bu girdi Status'u kesinleştirmez — `Kullanici` veya `Admin` onayı gerekir.

> Not: `Kullanici` rolü Admin'den yalnızca 2 şeyde ayrılır: (1) AuditLog'u göremez, (2) sistem yönetimi kapsamındaki başka hiçbir kısıtlaması yoktur — CRUD yetkileri Admin ile aynıdır.

> Not: Şoförle ilgili ek bilgiler (ehliyet no vb.) `ApplicationUser`'a değil, aşağıdaki `Driver` tablosuna konur — böylece Identity tablosu şişmez, iş verisi ayrışır.

### 1.2 Driver (Şoför — ApplicationUser ile 1–1 ilişkili, `BaseAuditableEntity`'den türer)
| Alan | Tip | Kısıtlama |
|---|---|---|
| *(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)* | — | `BaseAuditableEntity`'den gelir, ayrıca yazma |
| UserId | string | FK → ApplicationUser.Id, Required, Unique |
| LicenseNumber | string | Required, MaxLength(30) — ehliyet no |
| LicenseClass | string | MaxLength(10) — örn. "B", "C", "D" |
| LicenseExpiryDate | DateTime | Required |
| Department | string | MaxLength(150) — bağlı olduğu birim (opsiyonel) |
| IsActive | bool | Default true |

**İlişki:** `Driver.UserId` → `ApplicationUser.Id` (1–1, Restrict delete — kullanıcı silinirse şoför kaydı elde kalmasın diye önce bunu ele almak gerekir, direkt cascade delete verme).

**Tarihsel Ehliyet No Tekilliği (karar: Alternatif B):** `LicenseNumber` normal bir unique index ile korunmaz — silinmiş (soft-delete) şoförler dahil, bir ehliyet numarası sistemde bir kez kullanıldıysa bir daha kullanılamaz. Kontrol Service katmanında `IgnoreQueryFilters()` ile yapılır: `_context.Drivers.IgnoreQueryFilters().AnyAsync(d => d.LicenseNumber == cleanLicense)`. Aynı karar `Vehicle.PlateNumber` için de geçerlidir (bkz. 2.1).

**Ehliyet Süresi ve Görev Ataması (iş kuralı):** Bir şoföre görev atanırken (`Assignment` oluşturma/düzenleme), `Driver.LicenseExpiryDate`, görevin `PlannedEndTime`'ını kapsamıyorsa (`LicenseExpiryDate.Date < PlannedEndTime.Date`) atama **reddedilir**. Kontrol `AssignmentService.CheckDriverAvailabilityAsync` içinde, çifte rezervasyon kontrolüyle birlikte yapılır (bkz. 3.2).

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
| InspectionStartDate | DateTime | Required — Fenni muayene geçerlilik başlangıcı |
| InspectionExpiryDate | DateTime | Required — Fenni muayene bitiş tarihi |
| InsuranceStartDate | DateTime | Required — Trafik sigortası başlangıcı |
| InsuranceExpiryDate | DateTime | Required — Trafik sigortası bitiş tarihi |
| TireBrand | string | MaxLength(50), nullable — Lastik marka/model |
| TireYear | int | Nullable — Lastik üretim yılı (DOT) |
| TireSeason | enum `TireSeason` | Nullable — `Summer` (Yazlık) / `Winter` (Kışlık) |
| FineNotes | string | MaxLength(1000), nullable — Ceza/Şef notu (serbest metin, harici sistem entegrasyonu YOK) |

**VehicleStatus enum'una eklenen yeni değer:** `DocumentExpired` (Belgesi Geçersiz) — Muayene veya Sigorta tarihi geçtiğinde otomatik atanır.

**Otomatik durum yönetimi (`VehicleDocumentExpirationWorker` — BackgroundService):**
- Uygulama açılışında ve her 24 saatte bir çalışır.
- Süresi geçmiş (`Idle`/`Maintenance`/`OutOfService` durumundaki) araçları `DocumentExpired`'a çeker. **`OnDuty` (görevdeki) araçlara dokunmaz**, görev bitene kadar bekler.
- **Kurtarma kuralı:** `DocumentExpired` bir aracın tarihleri güncellenip geçerli hale gelirse, otomatik olarak `Idle` (veya açık bakımı varsa `Maintenance`) durumuna geri döner. Bu kural hem arka plan servisinde hem `VehicleService.UpdateVehicleAsync`'te (form üzerinden manuel güncellemede) çalışır — **sadece `Status == DocumentExpired` olan araçlar için**, diğer durumlara (`OnDuty`, `Maintenance`, `OutOfService`) asla müdahale etmez.
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
