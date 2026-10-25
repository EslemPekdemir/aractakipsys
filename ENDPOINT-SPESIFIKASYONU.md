# ENDPOINT / CONTROLLER SPESİFİKASYONU — Araç Takip ve Görevlendirme Sistemi

> Bu dosya `AGENTS.md` ve `VERI-MODELI.md` ile birlikte okunmalıdır. Amaç: hangi Controller'ın hangi Action'ları içereceğini, route'ları, yetki (rol) kurallarını ve tetiklenecek iş mantığını netleştirmek. Ajan bu listeye göre Controller iskeletlerini ve ViewModel'leri oluşturur.

---

## 0. GENEL KURALLAR

- Mimari **ASP.NET Core MVC + Razor Views**'dır, saf REST API değildir. Admin ekranları klasik form (GET/POST) akışıyla çalışır.
- **İstisna:** Şoför mobil ekranındaki "Görevi Başlat" / "Görevi Bitir" gibi anlık aksiyonlar, sayfa yenilemeden çalışması için **AJAX (JSON) uç noktaları** olarak yazılır (`[HttpPost]`, `[Produces("application/json")]`).
- Controller'lar **asla** doğrudan entity döndürmez/almaz; her Action kendi **ViewModel/DTO**'sunu kullanır (bkz. Bölüm 5). Entity ↔ ViewModel dönüşümü Service katmanında veya AutoMapper ile yapılır.
- Tüm veri değiştiren POST action'larında `[ValidateAntiForgeryToken]` zorunludur (AGENTS.md 4.3).
- Her Controller/Action'da `[Authorize(Roles = "...")]` zorunludur (AGENTS.md 3.2). Aşağıdaki tabloda her satırda belirtilmiştir.
- Route şablonu: `/{Alan}/{Action}/{id?}` — örn. `/Vehicle/Edit/5`. Şoför paneli için ayrı alan: `/Surucu/...`.
- Listeleme (Index) action'larında sayfalama (`PageNumber`, `PageSize`) ve temel filtreleme parametreleri standarttır.

---

## 1. ADMIN/KULLANICI — VehicleController (`/Vehicle`)

> **Rol güncellemesi (Faz 1-A):** Sistemde 5 rol (`Admin`, `DaireBaskani`, `GenelSekreter`, `Kullanici`, `Driver`) tanımlıdır. Operasyonel yönetim ekranlarında rol `AllStaffAndManagers` (`Admin,DaireBaskani,GenelSekreter,Kullanici`). `GenelSekreter` salt okunurdur ve `ReadOnlyRoleFilter` filtresi ile korunur. `AuditLogController` `Admin,DaireBaskani,GenelSekreter` rollerine açıktır. `SefController` ise yalnızca `Admin,DaireBaskani` rollerine açıktır (GenelSekreter giremez). Talep Seçenekleri (`/VehicleRequest/Options`) `Admin,DaireBaskani` rollerine açıktır.

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Vehicle` | Admin,Kullanici | Araç listesi. Filtre: `Status`, `VehicleType`, arama (plaka/marka). Sayfalanır. Filtre dropdown'ları **Select2** (aramalı + dikey scroll) standardında. |
| Details | GET | `/Vehicle/Details/{id}` | Admin,Kullanici | Araç detayı + son 5 görev + bakım geçmişi özeti. |
| Create | GET/POST | `/Vehicle/Create` | Admin,Kullanici | Yeni araç ekleme formu. POST'ta `VehicleCreateViewModel` alınır. Plaka tarihsel tekillik kontrolü (VERI-MODELI 2.1). |
| Edit | GET/POST | `/Vehicle/Edit/{id}` | Admin,Kullanici | Araç düzenleme. **`CurrentKm` ve `Status` bu formdan elle değiştirilemez** — salt okunur gösterilir. |
| Delete | POST | `/Vehicle/Delete/{id}` | Admin,Kullanici | Soft delete (`IsDeleted = true`). Aktif/planlı Assignment'ı olan veya devam eden bakımı olan araç silinemez — hata döndür. |
| ExpiringDocumentSummary | GET | `/Vehicle/ExpiringDocumentSummary` | Admin,Kullanici | Navbar bildirim çanı: Süresi dolan veya 30 gün içinde dolacak belgelerin ve lastik değişim eşiklerinin özeti (JSON). |
| Documents | GET | `/Vehicle/Documents` | Admin,Kullanici | Araç Belgeleri ve Lastik yönetim ekranı. Sekmeler: Muayene, Trafik Sigortası, Kasko, Lastik. Seçili aracın tüm geçmişi ve güncel kayıt durumu. |
| AddDocument | POST | `/Vehicle/AddDocument` | Admin,Kullanici | Yeni araç belgesi ekleme. Başlangıç, bitiş, tutar, firma, poliçe no ve notlar kaydedilir. Vehicle tablosu senkronize edilir, AuditLog yazılır. |
| UpdateDocument | POST | `/Vehicle/UpdateDocument/{id}` | Admin,Kullanici | Araç belgesi güncelleme. Vehicle tablosu senkronize edilir, AuditLog yazılır. |
| DeleteDocument | POST | `/Vehicle/DeleteDocument/{id}` | Admin,Kullanici | Araç belgesi silme (soft-delete). Kalan belgelere göre Vehicle tablosu senkronize edilir, AuditLog yazılır. |
| AddTire | POST | `/Vehicle/AddTire` | Admin,Kullanici | Yeni lastik kaydı ekleme (Marka, ebat, DOT, mevsim, takılma tarihi/KM, hedef değişim KM/tarih). Vehicle tablosu senkronize edilir, AuditLog yazılır. |
| UpdateTire | POST | `/Vehicle/UpdateTire/{id}` | Admin,Kullanici | Lastik kaydını düzenleme. Vehicle tablosu senkronize edilir, AuditLog yazılır. |
| DeleteTire | POST | `/Vehicle/DeleteTire/{id}` | Admin,Kullanici | Lastik kaydını soft-delete yapma. Bir aracın tek/son aktif lastik kaydı silinemez. |
| ExportTiresPdf | GET | `/Vehicle/ExportTiresPdf` | Admin,Kullanici | Tüm araçların güncel lastik durumunu, takılma ve kullanım KM'sini içeren Lastik Kullanım Raporu PDF çıktısı. |

---

## 2. ADMIN/KULLANICI — DriverController (`/Driver`)

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Driver` | Admin,Kullanici | Şoför listesi. Filtre: `IsActive`, `Department` (Select2). Ehliyet süresi dolan/30 günden az kalanlar rozetle vurgulanır. |
| Details | GET | `/Driver/Details/{id}` | Admin,Kullanici | Şoför detayı + sahip olduğu tüm aktif ehliyet sınıfları ve geçerlilik tarihleri + aktif/geçmiş görevleri + toplam sürüş istatistiği. |
| Create | GET/POST | `/Driver/Create` | Admin,Kullanici | Yeni şoför ekleme: hem `ApplicationUser` (Identity) hem `Driver` hem de girilen çoklu `DriverLicense` kayıtları **transaction içinde** oluşturulur. Ehliyet no tarihsel tekillik kontrolü (VERI-MODELI 1.2). |
| Edit | GET/POST | `/Driver/Edit/{id}` | Admin,Kullanici | Çoklu ehliyet sınıfları, geçerlilik tarihleri, `IsActive` güncellemesi. **Birim (`Department`) bilgisi yalnızca `Admin` rolü tarafından güncellenebilir**; Şef (`Kullanici`) rolü düzenleyemez (sunucu tarafında yetki kontrolü yapılır). |
| ExpiringLicenseSummary | GET | `/Driver/ExpiringLicenseSummary` | Admin,Kullanici | Ehliyet süresi 30 gün içinde dolacak veya dolmuş şoförlerin JSON özeti (`totalCount`, `expiringDrivers`). Bildirim çanı ve live toast kontrolü için kullanılır. |
| **ToggleStatus** | POST | `/Driver/ToggleStatus/{id}` | Admin,Kullanici | *(Spesifikasyon dışı eklendi — onaylandı)* Tek tıkla Aktif/Pasif değiştirme. Aktif/planlı görevi olan şoförde reddedilir. Şoför pasife alınınca bağlı `ApplicationUser.IsActive = false` da senkronize edilir (giriş yetkisi anında kesilir). |
| Delete | POST | `/Driver/Delete/{id}` | Admin,Kullanici | Soft delete. Aktif/planlı Assignment'ı olan şoför silinemez. |

---

## 3. ADMIN/KULLANICI — AssignmentController (`/Assignment`)

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Assignment` | Admin,Kullanici | Görev listesi. Filtre: `Status`, tarih aralığı, `VehicleId`, `DriverId` (Select2). `InProgress` satırlar vurgulu. Onay bekleyen bildirimler turuncu rozetle işaretli (bkz. Bölüm 3.1). |
| Details | GET | `/Assignment/Details/{id}` | Admin,Kullanici | Görev detayı (KM, notlar, zaman damgaları, onay durumu dahil). |
| Create | GET/POST | `/Assignment/Create` | Admin,Kullanici | Yeni görev atama. Araç/Şoför dropdown'ları müsaitlik + ehliyet durumuna göre işaretli (Bölüm 4). Ehliyet süresi görevin bitişini kapsamıyorsa reddedilir (VERI-MODELI 1.2). |
| Cancel | POST | `/Assignment/Cancel/{id}` | Admin,Kullanici | Sadece `Status = Planned` iken. `CancellationReason` zorunlu. |
| Edit | GET/POST | `/Assignment/Edit/{id}` | Admin,Kullanici | Sadece `Status = Planned` iken düzenlenebilir, kendi ID'si çakışma kontrolünden hariç tutulur. |

### 3.1 Onay Akışı Endpoint'leri (VERI-MODELI 3.1'deki `StartReportedBySelf`/`EndReportedBySelf` mekanizmasına bağlı)

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| ApproveStart | POST | `/Assignment/ApproveStart/{id}` | Admin,Kullanici | Şoförün bildirdiği (`StartReportedBySelf=true, StartApprovedByUserId=null`) başlangıcı onaylar. `Status=InProgress`, `Vehicle.Status=OnDuty` olur. Şoförün girdiği saat/KM değerleri korunur, üzerine yazılmaz. |
| ApproveEnd | POST | `/Assignment/ApproveEnd/{id}` | Admin,Kullanici | Aynı mantık, bitiş bildirimi için. `Status=Completed`, `Vehicle.Status=Idle`, `Vehicle.CurrentKm=EndKm`. Eğer Start hâlâ onaysızsa (`StartApprovedByUserId=null`), bu işlem sırasında o da otomatik onaylanır ("yetim onay" kalmasın diye). |
| StartDirect | POST | `/Assignment/StartDirect/{id}` | Admin,Kullanici | Şoför beklemeden, Kullanici/Admin'in kendisinin doğrudan başlatması. `startKm` opsiyonel (verilmezse `Vehicle.CurrentKm` kullanılır). `StartReportedBySelf=false`, `StartApprovedByUserId=<işlemi yapan>` — anında kesinleşir. |
| EndDirect | POST | `/Assignment/EndDirect/{id}` | Admin,Kullanici | Aynı mantık, doğrudan bitirme. `endKm` zorunlu, `driverReturnNotes` opsiyonel. |

---

## 4. YARDIMCI — AvailabilityController (`/Availability`) *(AJAX, Admin/Kullanici panelindeki Create formu için)*

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| CheckVehicleAvailability | GET (AJAX) | `/Availability/CheckVehicleAvailability?vehicleId=&start=&end=&excludeAssignmentId=` | Admin,Kullanici | Verilen aralıkta araç müsait mi? `{ isAvailable, reason, conflictingAssignmentId }` JSON döner. |
| CheckDriverAvailability | GET (AJAX) | `/Availability/CheckDriverAvailability?driverId=&start=&end=&excludeAssignmentId=` | Admin,Kullanici | Aynı mantık + ehliyet süresi kontrolü, şoför için. |
| **GetOptions** | GET (AJAX) | `/Availability/GetOptions?start=&end=&excludeAssignmentId=` | Admin,Kullanici | *(Spesifikasyon dışı eklendi — onaylandı)* Tüm araç/şoför listesini müsaitlik durumuyla birlikte döner (`{ vehicles: [...], drivers: [...] }`), Create/Edit formundaki dropdown'ların tek çağrıyla doldurulmasını sağlar. |

> Bu uçlar Assignment formunda canlı doğrulama için kullanılır — asıl kesin kontrol yine POST sırasında Service katmanında tekrar yapılır (istemci tarafı kontrolüne güvenilmez).

---

## 5. ADMIN/KULLANICI — MaintenanceController (`/Maintenance`)

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Maintenance` | Admin,Kullanici | Bakım kayıtları listesi. Filtre: `VehicleId` (Select2), devam eden/tamamlanan. |
| Details | GET | `/Maintenance/Details/{id}` | Admin,Kullanici | Bakım detay dökümü + araç künyesi. |
| Create | GET/POST | `/Maintenance/Create` | Admin,Kullanici | Yeni bakım kaydı. Aynı araçta ikinci açık bakım veya `OnDuty` araca bakım açılamaz. Kayıt oluşunca `Vehicle.Status = Maintenance`. |
| Edit | GET/POST | `/Maintenance/Edit/{id}` | Admin,Kullanici | Bakım kaydı güncelleme. |
| Complete | POST | `/Maintenance/Complete/{id}` | Admin,Kullanici | `EndDate` girilir. Aracın başka açık bakımı yoksa `Vehicle.Status = Idle` olur. |
| **Delete** | POST | `/Maintenance/Delete/{id}` | Admin,Kullanici | *(Spesifikasyon dışı eklendi — onaylandı, idari hata düzeltme senaryosu için)* Modal onaylı soft delete. Silinen kayıt devam eden bir bakımsa ve aracın başka açık bakımı yoksa `Vehicle.Status = Idle`'a döner. |

---

## 6. ADMIN — AuditLogController (`/AuditLog`) — Salt Okunur, SADECE Admin

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/AuditLog` | **Admin (sadece)** | Denetim kayıtları listesi. Filtre: `EntityName`, `ChangedByUserId`, tarih aralığı (Select2). `Kullanici` rolü bu ekranı **göremez** — sistemdeki tek "Admin'e özel" istisna budur. Kayıt gösterimi tamamen Türkçe/okunur (enum→etiket, boolean→Evet/Hayır, kullanıcı ID→isim); ham JSON veya teknik alan adı gösterilmez. |

---

## 7. ŞOFÖR PANELİ — SurucuController (`/Surucu`)

> **Güncelleme (Onay Akışı):** Şoförün Start/End işlemleri artık **anında kesinleşmiyor** — sadece bir "bildirim" olarak kaydediliyor, Kullanici/Admin onayı bekliyor (VERI-MODELI 3.1). Bu, "gece Şef yokken şoför göreve çıkıp dönebilsin" senaryosu için eklendi.

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| MyAssignments | GET | `/Surucu` | Driver | Giriş yapan şoförün görevleri (kart tasarımı: aktif, bugünkü, yaklaşan, geçmiş gruplu). Yatay yetki kontrolü zorunlu. |
| AssignmentDetail | GET | `/Surucu/Detay/{id}` | Driver | Görev detayı. Sadece kendi görevi ise erişilebilir. |
| **StartAssignment** (bildirim) | POST (AJAX) | `/Surucu/Baslat/{id}` | Driver | Body: `{ startKm }`. Sadece `Status = Planned` (veya zaten kendi bildirmişse mükerrer engeli). `ActualStartTime`, `StartKm` set edilir, `StartReportedBySelf = true`. **`Status`/`Vehicle` DEĞİŞMEZ.** Yanıt: "Bilgileriniz kaydedildi, Şef onayı bekleniyor." Buton metni arayüzde **"Başlangıcı Bildir"**. |
| **EndAssignment** (bildirim) | POST (AJAX) | `/Surucu/Bitir/{id}` | Driver | Body: `{ endKm, driverReturnNotes }`. Şart: `Status == InProgress` **VEYA** `StartReportedBySelf == true` (Şef henüz onaylamamış olsa bile şoför gece bitişi bildirebilsin diye). `ActualEndTime`, `EndKm`, `DriverReturnNotes` set edilir, `EndReportedBySelf = true`. **`Status`/`Vehicle` DEĞİŞMEZ.** Buton metni **"Bitişi Bildir"**. Ekranda "⏳ Şef onayı bekliyor" bilgisi + girilen not/KM salt okunur gösterilir. |
| **ChangePassword** | GET/POST | `/Surucu/ChangePassword` | Driver | Şoför mobil şifre değiştirme (ilk giriş zorunlu veya isteğe bağlı). |

---

## 8. KİMLİK — AccountController (`/Account`) — Kendi Yazılmış, Identity Scaffold DEĞİL

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Login | GET/POST | `/Account/Login` | Anonim | Kurumsal tasarımlı, bağımsız giriş sayfası. Dinamik SVG Captcha doğrulaması (`CaptchaToken` ve `CaptchaCode`) zorunludur. Kullanıcı adı veya e-posta ile giriş desteklenir. Kilitlenme (5 hatalı deneme → 15 dk), pasif hesap kontrolü. Rol bazlı yönlendirme. |
| GetCaptchaImage | GET (AJAX) | `/Account/GetCaptchaImage` | Anonim | Yeni dinamik SVG görseli ve 3 dakika geçerli şifreli token döner (`{ svgDataUri, token }`). |
| Logout | POST + GET | `/Account/Logout` | Herhangi (giriş yapmış) | Oturumu kapatır, `sessionStorage` bildirim imzasını temizler. |
| AccessDenied | GET | `/Account/AccessDenied` | Anonim | Yetkisiz erişim sayfası (role uygun ana sayfa butonu içerir). |
| Profile | GET/POST | `/Account/Profile` | Herhangi (giriş yapmış) | Profil bilgileri (Ad Soyad, E-posta, Telefon) görüntüleme ve güncelleme. Şifre alanları boş bırakılarak sadece kişisel bilgiler güncellenebilir (bağımsız parola akışı). Profil fotoğrafı yükleme: Magic Number (dosya başlığı baytları) doğrulamalı, max 5MB (JPEG, PNG, WebP). Oturum claim'leri anında yenilenir (`RefreshSignInAsync`). |
| RemoveProfilePhoto | POST | `/Account/RemoveProfilePhoto` | Herhangi (giriş yapmış) | Kullanıcının profil fotoğrafını kaldırır, diskteki fiziksel dosyayı siler, `ProfilePictureUrl = null` yapar ve arayüzü dinamik rozet avatara döndürür. |
| ForgotPassword | GET/POST | `/Account/ForgotPassword` | Anonim | Şifremi unuttum ekranı, e-posta ile güvenli bağlantı gönderimi. |
| ResetPassword | GET/POST | `/Account/ResetPassword` | Anonim | E-posta ile gelen token kullanılarak yeni parola tanımlama. |

> **Register YOK ve asla eklenmeyecek** — yeni kullanıcılar sadece `Admin`/`Kullanici` tarafından `Driver/Create` veya `Sef/Create` üzerinden oluşturulur. `ConfigureApplicationCookie` ile `LoginPath`/`AccessDeniedPath` ayarlandı, kimlik doğrulaması olmayan her istek otomatik `/Account/Login`'e düşer.

---

## 9. PUBLIC (ANONİM) — VehicleRequestController (`/AracTalebi` ve `/VehicleRequest`)

> Üniversitenin diğer birimlerindeki personelin (sisteme kayıtlı olmayan kişilerin) araç talep edebildiği, giriş gerektirmeyen modül. VERI-MODELI Bölüm 5.2 ile birlikte okunmalı.

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Create | GET/POST | `/AracTalebi` | **AllowAnonymous** | Giriş gerektirmez. Form: Ad Soyad, E-posta (`@duzce.edu.tr` zorunlu, çift katmanlı doğrulama), Telefon, Talep Amacı — **başka hiçbir alan yok**, araç/şoför listesi bu sayfaya asla sızmaz. |
| Success | GET | `/VehicleRequest/Success/{id}` | AllowAnonymous | "Talebiniz alındı" onay ekranı. |
| Index | GET | `/VehicleRequest` | Admin,Kullanici | Bekleyen/Karşılanan/Reddedilen talepler, filtre sekmeleri. |
| Fulfill | GET/POST | `/VehicleRequest/Fulfill/{id}` | Admin,Kullanici | Araç/şoför/tarih seçilip mevcut `AssignmentService.CreateAssignmentAsync` çağrılarak gerçek bir görev oluşturulur (tüm çakışma/ehliyet kontrolleri aynen işler). Başarılı olursa `Status=Fulfilled`, talep sahibine bildirim maili gider (Bölüm 5.3, VERI-MODELI). |
| Reject | POST | `/VehicleRequest/Reject/{id}` | Admin,Kullanici | Zorunlu gerekçe alınır, `Status=Rejected`, talep sahibine ret maili gider. |

---

## 10. VIEWMODEL / DTO LİSTESİ (Güncel — Ana Olanlar)

| ViewModel | Kullanıldığı Action | Ana Alanlar |
|---|---|---|
| `VehicleListItemViewModel` | Vehicle/Index | PlateNumber, Brand, Model, Color, Status, CurrentKm |
| `VehicleCreateViewModel` / `VehicleEditViewModel` | Vehicle/Create, Edit | VERI-MODELI 2.1'deki alanlar (Status/CurrentKm salt okunur) |
| `DriverCreateViewModel` | Driver/Create | FullName, Email, PhoneNumber, LicenseNumber, LicenseClass, LicenseExpiryDate, Department |
| `AssignmentCreateViewModel` | Assignment/Create | VehicleId/DriverId (Select2, müsaitlik+ehliyet işaretli), RequestedByDepartment, Purpose, PlannedStartTime, PlannedEndTime |
| `AssignmentListItemViewModel` | Assignment/Index | `IsStartApprovalPending`/`IsEndApprovalPending` hesaplanmış alanlar — `Status` Cancelled/Completed ise her ikisi de `false` döner (bkz. iş kuralı notu) |
| `AssignmentCardViewModel` / `DriverAssignmentListViewModel` | Surucu/MyAssignments | Onay bekliyor durumu dahil, mobil kart için sade alanlar |
| `StartAssignmentRequest` | Surucu/Baslat (AJAX body) | StartKm |
| `EndAssignmentRequest` | Surucu/Bitir (AJAX body) | EndKm, DriverReturnNotes |
| `VehicleRequestCreateViewModel` | VehicleRequest/Create (public) | RequesterFullName, RequesterEmail (regex), RequesterPhone, Purpose |
| `AjaxResultViewModel` | Tüm AJAX action'ların ortak dönüş tipi | `{ success: bool, message: string, data: object? }` |

---

## 11. HATA / YANIT KONVANSİYONU

- **MVC form action'ları** (Create/Edit vb.): Validasyon hatası varsa `ModelState` ile aynı view'a geri dönülür, `ViewModel` doldurulmuş şekilde. Başarılıysa `RedirectToAction(nameof(Index))` + `TempData["Success"]` mesajı.
- **AJAX action'ları** (Availability, Surucu/Baslat, Surucu/Bitir): Her zaman `AjaxResultViewModel` formatında JSON döner, HTTP status her zaman `200` — hata durumu `success: false` ve `message` alanıyla iletilir. İstisna: `[Authorize]` reddi (401/403) ve sunucu hatası (500) doğal HTTP kodlarıyla döner.
- **Sayfalama:** Tüm Index action'larında veritabanı sorgusunda `.Skip((page-1)*pageSize).Take(pageSize)` (server-side paging) kullanılır — asla tüm kayıtlar çekilip tarayıcıda bölünmez. Bu özellikle `AuditLog` gibi büyüyecek tablolarda zorunludur.
- **Filtre dropdown'ları:** Seçenek sayısı çok olan (Araç, Şoför, Kullanıcı gibi) tüm filtrelerde standart `<select>` yerine **Select2** (arama kutulu + 200px dikey scroll) kullanılır — sistem genelinde tutarlı.

---

## 12. GELECEK MODÜL (PLANLANDI) — ReportController

> Henüz geliştirilme aşamasında; tamamlanınca bu bölüm gerçek implementasyona göre güncellenecek.

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Report` | Admin,Kullanici | Araç ve/veya Şoför filtresi (en az biri zorunlu) + tarih aralığı (Bu Ay/Geçen Ay/Bu Yıl/Geçen Yıl/Özel kısayolları) ile sadece `Completed` görevlerden kullanım dökümü. Toplam sefer/KM özeti. |
| ExportPdf | GET | `/Report/ExportPdf?...` | Admin,Kullanici | Aynı filtrelerle PDF çıktısı. Kütüphane seçimi onay bekliyor. |

---

## 13. SONRAKİ ADIM ÖNERİSİ (Geçmiş referans — tüm adımlar tamamlandı)

1. ~~`BaseAuditableEntity` + tüm entity sınıfları + enum'lar~~ ✅
2. ~~`ApplicationDbContext` + Fluent API konfigürasyonları + Global Query Filter~~ ✅
3. ~~İlk migration~~ ✅
4. ~~Service katmanı~~ ✅
5. ~~Controller'lar + ViewModel'ler~~ ✅
6. ~~Razor View'lar~~ ✅
7. ~~3. rol (Kullanici) + onay akışı~~ ✅
8. ~~VehicleRequest modülü + e-posta bildirimi~~ ✅
9. 🔲 Report modülü (Bölüm 12) — devam ediyor

---

# EK GÜNCELLEME SETİ (v2) — Yeni Controller ve Endpoint'ler

## 14. ADMIN ÖZEL — SefController (`/Sef`)

| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| Index | GET | **Admin (sadece)** | Şef listesi, aktif/pasif filtre. |
| Create | GET/POST | Admin | Yeni Şef hesabı (`@duzce.edu.tr` zorunlu). |
| Edit | GET/POST | Admin | Bilgi güncelleme. |
| ToggleStatus | POST | Admin | Hızlı aktif/pasif. |
| ResetPassword | POST | Admin | Şef'in şifresini sıfırlar. |
| Delete | POST | Admin | **Soft-delete** (`IsActive = false`) — kalıcı silme YOK, AuditLog FK çakışması riski nedeniyle bilerek bu şekilde tasarlandı. Kendi hesabını pasife alma engellenir. |

## 15. ADMIN/KULLANICI — VehicleController Ek Endpoint

| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| ExpiringDocumentSummary | GET (AJAX) | Admin,Kullanici | Navbar bildirim çanı için: 30 gün içinde dolacak/dolmuş muayene-sigorta özeti döner. |

## 16. ADMIN/KULLANICI — HomeController (Dashboard) Ek Endpoint

| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| SearchPlate | GET (AJAX) | Admin,Kullanici | Dashboard plaka arama — araç bilgisi, sigorta/muayene durumu, ceza notu, kullanan şoförler geçmişi (Completed kayıtlar) döner. |

## 17. ADMIN/KULLANICI — AssignmentController Ek Endpoint'ler

| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| SignedDocuments | GET | Admin,Kullanici | **İmzalı Evrak Arşivi ve Takibi:** Tüm görevlere ait yüklenmiş ıslak imzalı taranmış evrakları arama, araç/şoför/tarih filtresi ve sayfalama ile listeler. |
| ExtendDuration/{id} | POST | Admin,Kullanici | **InProgress Görev Süresi Uzatma:** Seyir halindeki (`InProgress`) veya planlanan görevlerin `PlannedEndTime` süresini çakışma kontrollü olarak ileri bir tarihe uzatır. Zorunlu gerekçe (`reason`) alır, `ExpenseNotes` alanına `[Süre Uzatma - dd.MM.yyyy HH:mm]: {reason}` damgalar ve `AuditLog` oluşturur. |
| DownloadOrderDocument/{id} | GET | Admin,Kullanici | Resmi Görevlendirme Belgesi PDF'ini üretip indirir (QuestPDF, imza kutulu resmi evrak). |
| UploadSignedDocument/{id} | POST | Admin,Kullanici | İmzalı/taranmış belge yükler (PDF/JPEG/PNG, max 10MB, magic number doğrulamalı, `[RequestSizeLimit]` + `[RequestFormLimits]` ile Kestrel seviyesinde de sınırlı). |
| DownloadSignedDocument/{id} | GET | Admin,Kullanici | Yüklenen imzalı belgeyi güvenli şekilde indirir (dosya `wwwroot` dışında, sadece bu action üzerinden erişilebilir). |
| ApproveEnd/{id} (güncellendi) | POST | Admin,Kullanici | Artık `HourlyRate` alanını da alır (zorunlu, varsayılan değer yok), `CalculatedDutyHours`/`TotalDriverFee` otomatik hesaplanır. |
| UpdateFinancialDetails/{id} | POST | Admin,Kullanici | **Maliyet ve Harcırah Revizyonu:** Tamamlanmış (`Completed`) görevlerin sonradan ibraz edilen fatura/belgelerine istinaden `AccommodationCost`, `AccommodationNights`, `PerDiem*`, `HourlyRate` alanlarını günceller. Kilometre, fiili saat veya görev durumuna dokunmaz. Zorunlu gerekçe (`RevisionReason`) gerektirir, işlem `AuditLog`'a kaydedilir. |

## 18. RAPOR MERKEZİ — ReportController (Tam Liste, 3 Sekmeli)

| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| Index (`/Report`) | GET | Admin,Kullanici | **Sefer & Kullanım Raporu** — Araç/Şoför/Tarih filtresi, sadece `Completed` görevler, satır bazında süre (`DurationHelper`, "2 sa 40 dk"), şoför bazlı gruplar ve alt toplamlar, tekil genel toplam mesafe ve süre, PDF export. |
| Maintenance (`/Report/Maintenance`) | GET | Admin,Kullanici | **Bakım & Tamir Dökümü** — Araç/Tür/Durum/Tarih filtresi, her satırda Maliyet (boşsa -), rapor sonunda ve KPI kartında Genel Toplam Bakım Maliyeti, PDF export. |
| InsuranceInspection (`/Report/InsuranceInspection`) | GET | Admin,Kullanici | **Sigorta & Muayene Durum Raporu** — Tümü/Süresi Geçenler/30 Gün filtresi; `VehicleDocuments` üzerinden Trafik Sigortası, Fenni Muayene ve Kasko ödenen tutarları ve toplam belge harcamaları. |
| ExportPdf / ExportMaintenancePdf / ExportInsuranceInspectionPdf | GET | Admin,Kullanici | Her rapor için ayrı PDF export action'ı, aynı filtrelerle çalışır; kurumsal QuestPDF formatında maliyet ve süre detaylarını döker. |

> Üç rapor sayfası da aynı sekme çubuğunu paylaşır, birbirine tek tıkla geçiş sağlar (Bölüm 0'daki genel kural: tutarlı navigasyon).

## 19. VehicleRequestController — Güncel Tam Liste

| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| Create (`/AracTalebi`) | GET/POST | **AllowAnonymous** | Public form — Ad Soyad, E-posta (@duzce.edu.tr), Telefon, **Birim/Fakülte (datalist)**, Talep Amacı. |
| Success | GET | AllowAnonymous | Onay ekranı. |
| Index | GET | Admin,Kullanici | Bekleyen/Karşılanan/Reddedilen filtre sekmeleri. |
| Fulfill | GET/POST | Admin,Kullanici | Araç+şoför seçip gerçek Assignment oluşturur, mail bildirimi gider. |
| Reject | POST | Admin,Kullanici | Gerekçeli ret, mail bildirimi gider. |

## 20. Dashboard Bildirim Sistemi — Teknik Özet

- **Navbar bildirim çanları:** (1) Muayene&Sigorta uyarı çanı, (2) Araç Talebi çanı — her ikisi de AJAX ile periyodik kontrol eder (Talep: 30 saniyede bir; Belge uyarısı: sadece sayfa yüklenince, çünkü gün bazlı veri saniyelik kontrol gerektirmez).
- **Otomatik Toast Bildirimi:** Dashboard'a her girişte, eğer bekleyen talep veya belge uyarısı varsa, native Bootstrap Toast otomatik açılır + Web Audio API ile üretilen kurumsal bip sesi çalar (harici ses dosyası/kütüphane yok).
- **Tekrar gösterme mantığı (`sessionStorage` + imza):** Bildirim içeriğinin bir "imzası" (`user:{userId}|pending:{sayı}[{id listesi}]|docs:{sayı}[{id listesi}]`) hesaplanır. Aynı imza aynı oturumda bir daha gösterilmez (spam önlenir), ama imza değişirse (yeni talep/uyarı) tekrar gösterilir. Kullanıcı elle kapatırsa o imza bir daha çıkmaz. Login/Logout'ta `sessionStorage` temizlenir (farklı kullanıcıların bildirimleri karışmasın diye).

## 21. Login Yönlendirme Düzeltmesi

`AccountController.Login` POST metodu artık `ReturnUrl`'i **kullanıcının rolüyle uyumluysa** kullanır: Driver için sadece `/surucu` veya `/account` ile başlayan URL'ler kabul edilir, Admin/Kullanici için `/surucu` ile başlayan URL'ler reddedilir. Uyumsuzsa rol bazlı varsayılan sayfaya (Dashboard veya `/Surucu`) düşülür. `AccessDenied` sayfasındaki "Ana Sayfa" butonu da role göre doğru adrese gider.

---

## 22. ÜCRET VE GÖREV TERMİNOLOJİSİ STANDARDI

- **"Yevmiye" ve "Kadrolu Personel" İbarelerinin Kaldırılması:** Tüm kod tabanı, controller parametreleri, View ve PDF çıktılarında eski "Yevmiye" terimi yerine **"Görev Ücreti"**, **"Saatlik Ücret"** ve **"Harcırah / Konaklama Bedeli"** terimleri standartlaştırılmıştır.
- **Kadrolu Personel Ayrımı:** Üniversite bünyesindeki tüm şoförler kurumsal havuzda tek bir personel yapısıyla ele alınır, harici ticari kiralama veya kadrolu/sözleşmeli ayrımı formlardan tamamen kaldırılmıştır.
- **Ücret Parametreleri:** Görev onayında (`ApproveEnd`) `HourlyRate` alanı serbestçe girilir; `CalculatedDutyHours` (fiili görev saati) ile çarpılarak `TotalDriverFee` hesaplanır.

---

## 23. SVG CAPTCHA VE GÜVENLİK ALTYAPISI

- **Sıfır Dış Bağımlılık:** Google reCAPTCHA gibi harici kütüphaneler yerine yerel `CaptchaService` üzerinden dinamik SVG üretilir (`ICaptchaService`).
- **DataProtection Güvenliği:** Token mekanizması ASP.NET Core DataProtection ile şifrelenir (`aractakipsys.CaptchaProtection.v1`). Veritabanı tablosu veya server session yükü oluşturmaz (stateless).
- **Zaman Aşımı ve Tek Kullanımlık Nonce:** Her token üretildikten itibaren 3 dakika geçerlidir; doğrulama sonrası tekrar kullanılamaz. OCR botlarına karşı harf rotasyonu ve SVG gürültü çizgileri içerir.

---

## 24. DOSYA YÜKLEME VE PROFİL FOTOĞRAFI GÜVENLİĞİ (MAGIC NUMBER DOĞRULAMASI)

Sisteme yüklenen tüm ikili dosyalarda (İmzalı Görev Belgeleri ve Profil Fotoğrafları) istemci beyanına (`Content-Type` veya dosya uzantısı) güvenilmez; sunucu seviyesinde **Magic Number (Dosya Başlığı Baytları)** doğrulaması yapılır:

- **Amaç:** Saldırganların çalıştırılabilir zararlı scriptleri (PHP, ASPX, EXE, Shell vb.) dosya uzantısını `.png` veya `.jpg` yaparak sisteme yüklemesini ve web sunucusu üzerinde yetkisiz kod çalıştırmasını (Remote Code Execution / RCE) kesin olarak engellemek.
- **Doğrulanan Dosya Başlık İmzaları (`AccountController.IsValidImageFile`):**
  - **JPEG / JPG:** İlk 3 bayt `0xFF, 0xD8, 0xFF`
  - **PNG:** İlk 8 bayt `0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A`
  - **WebP:** İlk 4 bayt `0x52, 0x49, 0x46, 0x46` ("RIFF") ve 8-11. baytlar `0x57, 0x45, 0x42, 0x50` ("WEBP")
- **Kapsamlı Güvenlik Zinciri:**
  1. **Boyut Sınırı:** Profil fotoğrafları için en fazla 5 MB (`file.Length <= 5 * 1024 * 1024`), imzalı görev evrakları için en fazla 10 MB (`[RequestSizeLimit(10485760)]`).
  2. **Güvenli İsimlendirme:** Orijinal kullanıcı dosya adı asla doğrudan disk yolunda kullanılmaz; `Guid.NewGuid() + ext` formatında rastgele benzersiz isim türetilir (Path Traversal önleme).
  3. **Yetim Dosya Temizliği:** Profil güncellemesinde form validasyonu veya Identity DB işlemi hata verirse, az önce diske yazılan geçici dosya derhal silinir. Başarılı güncellemede ise eski profil fotoğrafı diskten temizlenerek sunucuda atıl dosya birikmesi engellenir.

---

## 25. FAZ 3 ENDPOINT'LERİ — TAŞIT GÖREV EMRİ, KONTROL TİKLERİ VE ARIZA BİLDİRİMİ

### 25.1 Arıza Bildirimi — FaultReportController (`/FaultReport`)
| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| Index | GET | Admin,Kullanici | Tüm arıza bildirimlerini durum filtresi (`Pending`, `Approved`, `Resolved`, `Rejected`) ile listeler. |
| Detail/{id} | GET | Admin,Kullanici | Bildirim detayını gösterir; Şef inceleme ve onay formu içerir. |
| Approve | POST | Admin,Kullanici | Şefin arıza kaydını onaylayıp not düşmesini sağlar. İlgili `AuditLog` kaydı üretilir. |
| UpdateStatus | POST | Admin,Kullanici | Durumu doğrudan güncelleme (`Resolved`, `Rejected` vb.). |
| DownloadPdf/{id} | GET | Admin,Kullanici | Onaylanmış veya doldurulmuş resmi "ARAÇ ARIZA BİLDİRİM FORMU" PDF'ini üretir (QuestPDF). |
| DownloadBlankPdf | GET | Admin,Kullanici | Elle doldurulmak üzere boş "ARAÇ ARIZA BİLDİRİM FORMU" PDF'ini üretir. |
| PendingNotificationSummary | GET (AJAX) | Admin,Kullanici | Navbar bildirim çanı için bekleyen arıza sayısını ve son kayıtları döner (30s periyodik kontrol). |

### 25.2 Taşıt Görev Emri ve Güzergah Dışı Sevk — AssignmentController Ekleri
| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| DownloadOrderDocument/{id} | GET | Admin,Kullanici | Resmi Taşıt Görev Emri PDF'i üretir. `?emptyValues=true` parametresi verilirse çıkış/giriş saat ve KM alanları elle doldurulmak üzere boş basılır. Belge tek sayfa A4 olarak üretilir. |
| SaveOffRouteDispatch | POST | Admin,Kullanici | Acil durumlarda güzergah dışına sevk eden yetkili bilgilerini (`OffRouteDispatcherName`, `OffRouteDispatcherTitle`, `OffRouteReason`) sisteme işler ve `AuditLog` oluşturur. |

### 25.3 Şoför Paneli Ekleri — SurucuController
| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| GorevEmri/{id} | GET | Driver | Şoförün yalnızca kendisine atanmış göreve ait Taşıt Görev Emri PDF'ini indirmesini sağlar. **Yatay Yetki:** Başka şoförün görev ID'si istenirse `Forbid()` (403) döner. |
| SaveVehicleChecks | POST (AJAX/Form) | Driver | Görev detayında şoförün Yağ, Su, Yakıt, Hidrolik ve Lastik kontrol tiklerini kaydeder. `Assignment` üzerine işlenir ve `AuditLog` yazılır. |
| ReportFault | POST (AJAX/Form) | Driver | Şoförün araç arıza bildirimi açmasını sağlar. Kayıt `Pending` durumunda oluşur, Ulaşım Şefliği bildirim çanına anında düşer. `AuditLog` yazılır. |

---

## 26. FAZ 2 - A KISMI ENDPOINT GÜNCELLEMELERİ

### 26.1 AssignmentController Güncellemeleri
- `Create` (POST): `AssignmentCreateViewModel` içinde `List<int> DriverIds` (çoklu kurum şoförü), `IsDriverless`, `IsExternalDriver`, `ExternalDriverFullName`, `ExternalDriverDepartment`, `Destination`, `Route` alanlarını alır.
- `Edit` (GET/POST): Çoklu şoför ve yeni alanları destekler.
- `DownloadOrderDocument/{id}` (GET): `driverId` (int?) opsiyonel parametresi alır. Görevde birden fazla şoför varsa belirli şoförün adına görev emri basılmasını sağlar. Şoförsüz görevde şoför hanesi "Şoförsüz" basılır. Dış şoförde dış şoför adı ve birimi basılır.

### 26.2 SurucuController Güncellemeleri (Yatay Yetki ve Çoklu Şoför)
- `MyAssignments` & `AssignmentDetail`: `a.AssignmentDrivers.Any(ad => ad.Driver.UserId == currentUserId)` ile filtrelenir.
- `StartAssignment`, `EndAssignment`, `SaveVehicleChecks`, `ReportFault`: Göreve atanmış HERHANGİ bir şoför işlem yapabilir; yapan şoförün adı notlara/kayda düşülür.
- `GorevEmri/{id}`: Giriş yapan şoförün `driverId`'si ile kendi adına olan görev emri PDF'i üretilir (yatay yetki kontrolü).

---

## 27. FAZ 2 - B KISMI ENDPOINT GÜNCELLEMELERİ

### 27.1 SurucuController — Güzergah Değiştirme
| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
| `GuzergahDegistir/{id:int}` | POST (AJAX) | Driver | Seferdeki (`InProgress`) görev için şoförün yeni güzergah ve gerekçe girmesini sağlar. `[ValidateAntiForgeryToken]` zorunludur.<br>**Yatay Yetki:** Göreve atanmış (asil veya yedek) herhangi bir şoför yapabilir. Atanmamış şoför talepte bulunursa HTTP 403 `Forbid` döner.<br>**Veri:** `AssignmentRouteChange` tablosuna zaman, değiştiren şoför, eski ve yeni güzergah, gerekçe kaydedilir. Ayrıca görev ana kaydındaki `Assignment.Route` güncellenir. |

### 27.2 AssignmentController — Bitiş Onayı, Şef KM Düzeltmesi ve Güzergah Bildirimi
| Action | HTTP | Rol | Açıklama |
|---|---|---|---|
---

## 31. FAZ 6 ENDPOINT GÜNCELLEMELERİ — ARAÇ TALEP FORMU VE YÖNETİM SEÇENEKLERİ

### 31.1 VehicleRequestController (`/VehicleRequest` & `/AracTalebi`)
| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Create | GET | `/AracTalebi`, `/VehicleRequest/Create` | Anonim (`[AllowAnonymous]`) | Girişsiz araç talep formu. Admin tanımlı aktif `RequestDepartments` ve `RequestVehicleTypes` açılır listeleri doldurulur. Formda *"Yalnızca il içi görevlendirme yapılır."* uyarısı gösterilir. |
| Create | POST | `/AracTalebi`, `/VehicleRequest/Create` | Anonim (`[AllowAnonymous]`, RateLimiting) | Talep gönderimi. `@duzce.edu.tr` kurumsal e-posta, geçerli telefon, birim açılır listesi, istenen araç türü, yolcu sayısı (`PassengerCount >= 1`), görev başlangıç ve bitiş tarih-saat zorunluluğu doğrulanır. |
| Success | GET | `/AracTalebi/Success/{id}` | Anonim (`[AllowAnonymous]`) | Talep onay/bilgi ekranı. |
| Index | GET | `/VehicleRequest`, `/VehicleRequest/Index` | Admin, Kullanici | Gelen talepler tablosu. İstenen araç türü ve kişi sayısı rozetleri listelenir. Üst kısımda Admin'e özel "Talep Seçenekleri" yönetim butonu yer alır. |
| Fulfill | GET | `/VehicleRequest/Fulfill/{id}` | Admin, Kullanici | Talebi karşılama mini-formu. Talep sahibinin istediği araç türü, kişi sayısı ve tarihleri sol kartta gösterilir. Görevlendirilecek araç ve şoförün yanı sıra Ulaşım Şefleri ve Şoförler arasından **İletişim Kişisi** seçilir. |
| Fulfill | POST | `/VehicleRequest/Fulfill/{id}` | Admin, Kullanici | Talebi karşılayıp `Assignment` oluşturur. Talep onay e-postasında **görevli şoförün yalnızca ADI** gösterilir (şoförün telefonu/e-postası gizlenir). Talep sahibinin irtibat kurması için seçilen iletişim kişisinin adı, telefonu ve e-postası e-postaya eklenir. Kurum e-posta ayarı yoksa veya gönderim hatası olursa talep akışı kesilmez, loglanır. |
| Reject | POST | `/VehicleRequest/Reject/{id}` | Admin, Kullanici | Talebi gerekçesiyle reddeder ve ret e-postası gönderir. |
| Options | GET | `/VehicleRequest/Options` | **Admin (sadece)** | Birim listesi ve Araç türü listesini yönetme ekranı. Admin dışındaki roller erişemez (`[Authorize(Roles = "Admin")]`). |
| SaveDepartment | POST | `/VehicleRequest/SaveDepartment` | **Admin (sadece)** | Yeni birim ekleme veya mevcut birimi düzenleme. `[ValidateAntiForgeryToken]` zorunlu. |
| ToggleDepartment | POST | `/VehicleRequest/ToggleDepartment/{id}` | **Admin (sadece)** | Birimi aktif/pasif yapma (fiziksel silme yok, geçmiş talepler korunur). |
| SaveVehicleType | POST | `/VehicleRequest/SaveVehicleType` | **Admin (sadece)** | Yeni araç türü ekleme veya düzenleme. `[ValidateAntiForgeryToken]` zorunlu. |
| ToggleVehicleType | POST | `/VehicleRequest/ToggleVehicleType/{id}` | **Admin (sadece)** | Araç türünü aktif/pasif yapma. |

---

## 32. FAZ 8 ENDPOINT GÜNCELLEMELERİ — PDF ÖNİZLEME UÇ NOKTALARI

Tüm PDF uç noktaları aynı URL ve yetki kurallarıyla çalışır; `preview=true` parametresi verildiğinde `Content-Disposition: inline` başlığı ile döner. Yeni/yetkisiz bir uç nokta açılmamıştır.

| Controller | Action | HTTP | Parametreler | İzinli Roller / Yatay Yetki | Açıklama |
|---|---|---|---|---|---|
| `AssignmentController` | `DownloadOrderDocument/{id}` | GET | `emptyValues` (bool), `driverId` (int?), `preview` (bool) | Admin, Kullanici | Taşıt Görev Emri (dolu, boş veya belirli şoföre özel). `preview=true` ile modalda önizleme. |
| `SurucuController` | `GorevEmri/{id}` | GET | `emptyValues` (bool), `preview` (bool) | Driver (Şoför) | Şoförün kendi görev emri belgesi. **Yatay yetki korumalı:** Şoför sadece kendisine atanmış görevi önizleyebilir/indirebilir. |
| `FaultReportController` | `DownloadPdf/{id}` | GET | `isBlank` (bool), `preview` (bool) | Admin, Kullanici, Driver | Araç Arıza Bildirim Formu (dolu veya boş). `preview=true` ile önizleme. |
| `FaultReportController` | `DownloadBlankPdf` | GET | `preview` (bool) | Admin, Kullanici, Driver | Boş Matbu Arıza Bildirim Formu. `preview=true` ile önizleme. |
| `ReportController` | `ExportPdf` | GET | `ReportFilterViewModel`, `preview` (bool) | Admin, Kullanici | Araç ve Şoför Kullanım Raporu PDF çıktısı. `preview=true` ile önizleme. |
| `ReportController` | `ExportMaintenancePdf` | GET | `MaintenanceReportFilterViewModel`, `preview` (bool) | Admin, Kullanici | Araç Bakım ve Maliyet Raporu PDF çıktısı. `preview=true` ile önizleme. |
| `VehicleController` | `ExportTiresPdf` | GET | `preview` (bool) | Admin, Kullanici | Araç Filosu Lastik Kullanım ve Durum Raporu PDF çıktısı. `preview=true` ile önizleme. |

---

## 33. FAZ 7-B ENDPOINT GÜNCELLEMELERİ — ARAÇ TALEPLERİ RAPORU VE PDF ÇIKTISI

| Controller | Action | HTTP | Parametreler | İzinli Roller | Açıklama |
|---|---|---|---|---|---|
| `ReportController` | `VehicleRequests` | GET | `VehicleRequestReportFilterViewModel` (`StartDate`, `EndDate`) | Admin, Kullanici | Talep eden birimler bazında araç talep istatistiği HTML ekranı. Tarih filtresi `CreatedAt` üzerinden çalışır. Tablo, özet KPI kartları ve dip toplam satırı içerir. |
| `ReportController` | `ExportVehicleRequestsPdf` | GET | `VehicleRequestReportFilterViewModel` (`StartDate`, `EndDate`), `preview` (bool) | Admin, Kullanici | Talep eden birimler bazında araç talep istatistiği QuestPDF dökümü. `preview=true` parametresiyle `Content-Disposition: inline` dönerek ortak `_PdfPreviewModal` üzerinden sayfa yenilenmeden önizlenir. Sunucu tarafında `[Authorize(Roles = "Admin,Kullanici")]` ile korunur. |

---

## 34. FAZ 9 ENDPOINT GÜNCELLEMELERİ — DASHBOARD TAKVİMİ VERİ UÇ NOKTASI

| Controller | Action | HTTP | Parametreler | İzinli Roller | Açıklama |
|---|---|---|---|---|---|
| `HomeController` | `CalendarData` | GET | `year` (int?), `month` (int?) | Admin, Kullanici | Dashboard aylık araç doluluğu ve gün detaylarını tek seferlik sorgu mantığıyla (N+1 önlenerek) JSON olarak döndüren uç nokta. Şoförler erişemez; yetki sunucuda kontrol edilir. |

---

## 35. FAZ 1-B ENDPOINT GÜNCELLEMELERİ — YETKİ DEVRİ (VEKÂLET) UÇ NOKTALARI

| Controller | Action | HTTP | Parametreler | İzinli Roller | Açıklama |
|---|---|---|---|---|---|
| `RoleDelegationController` | `Index` | GET | - | Admin, DaireBaskani, GenelSekreter, Kullanici | Vekâlet Yönetimi ve Geçmiş Listesi ekranı. Genel Sekreter salt okunur izler. Şefler yalnızca taraf oldukları vekâletleri; Admin ve Daire Başkanı tüm vekâletleri görür. |
| `RoleDelegationController` | `GetCreateModal` | GET | - | Admin, DaireBaskani, Kullanici | Yetki Devri Oluşturma Modal formunu ve uygun asıl/vekil personel listesini JSON olarak döndürür. |
| `RoleDelegationController` | `Create` | POST | `[FromBody] RoleDelegationCreateInputModel` | Admin, DaireBaskani, Kullanici | Yeni yetki devri kaydı oluşturur (`[ValidateAntiForgeryToken]`). Şef için formdan asıl yetkili alınmaz (kendi kimliği kullanılır); Admin/Daire Başkanı asıl şefi seçebilir. Çakışma ve zincirleme kontrolleri yapılır. |
| `RoleDelegationController` | `Revoke` | POST | `[FromBody] RoleDelegationRevokeInputModel` | Admin, DaireBaskani, Kullanici | Aktif bir yetki devrini erken sonlandırır (`ActualRevokedAt = DateTime.UtcNow`). Yetkiyi devreden Şef veya Admin/Daire Başkanı sonlandırabilir. Önbellek derhal temizlenir. |
| `RoleDelegationController` | `Print/{id}` | GET | `id` (int), `preview` (bool) | Admin, DaireBaskani, GenelSekreter, Kullanici | Yetki Devri (Vekâlet) Belgesi PDF dökümü. `preview=true` ile `inline` önizleme, parametresiz veya false ile doğrudan dosya indirme (`attachment`) sağlar. |

