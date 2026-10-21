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

> **Rol güncellemesi:** Sistemde 3. bir rol (`Kullanici` — Şoför Şefi) eklendi. Aşağıdaki tüm yönetim ekranlarında rol artık `Admin,Kullanici` — sadece `AuditLogController` (Bölüm 6) Admin'e özel kalıyor.

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Vehicle` | Admin,Kullanici | Araç listesi. Filtre: `Status`, `VehicleType`, arama (plaka/marka). Sayfalanır. Filtre dropdown'ları **Select2** (aramalı + dikey scroll) standardında. |
| Details | GET | `/Vehicle/Details/{id}` | Admin,Kullanici | Araç detayı + son 5 görev + bakım geçmişi özeti. |
| Create | GET/POST | `/Vehicle/Create` | Admin,Kullanici | Yeni araç ekleme formu. POST'ta `VehicleCreateViewModel` alınır. Plaka tarihsel tekillik kontrolü (VERI-MODELI 2.1). |
| Edit | GET/POST | `/Vehicle/Edit/{id}` | Admin,Kullanici | Araç düzenleme. **`CurrentKm` ve `Status` bu formdan elle değiştirilemez** — salt okunur gösterilir. |
| Delete | POST | `/Vehicle/Delete/{id}` | Admin,Kullanici | Soft delete (`IsDeleted = true`). Aktif/planlı Assignment'ı olan veya devam eden bakımı olan araç silinemez — hata döndür. |

---

## 2. ADMIN/KULLANICI — DriverController (`/Driver`)

| Action | HTTP | Route | Rol | Açıklama |
|---|---|---|---|---|
| Index | GET | `/Driver` | Admin,Kullanici | Şoför listesi. Filtre: `IsActive`, `Department` (Select2). Ehliyet süresi dolan/60 günden az kalanlar rozetle vurgulanır. |
| Details | GET | `/Driver/Details/{id}` | Admin,Kullanici | Şoför detayı + aktif/geçmiş görevleri + toplam sürüş istatistiği. |
| Create | GET/POST | `/Driver/Create` | Admin,Kullanici | Yeni şoför ekleme: hem `ApplicationUser` (Identity) hem `Driver` kaydı **transaction içinde** birlikte oluşturulur. Ehliyet no tarihsel tekillik kontrolü (VERI-MODELI 1.2). |
| Edit | GET/POST | `/Driver/Edit/{id}` | Admin,Kullanici | Ehliyet bilgisi, birim, `IsActive` güncellemesi. Aktif görevi olan şoför pasife alınamaz. |
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
| Index (`/Report`) | GET | Admin,Kullanici | **Sefer & Kullanım Raporu** — Araç/Şoför/Tarih filtresi, sadece `Completed` görevler, PDF export. |
| Maintenance (`/Report/Maintenance`) | GET | Admin,Kullanici | **Bakım & Tamir Dökümü** — Tarih+SAAT filtresi (`datetime-local`), PDF'de de saat bilgisi var. |
| InsuranceInspection (`/Report/InsuranceInspection`) | GET | Admin,Kullanici | **Sigorta & Muayene Durum Raporu** — Tümü/Süresi Geçenler/30 Gün filtresi. |
| ExportPdf / ExportMaintenancePdf / ExportInsuranceInspectionPdf | GET | Admin,Kullanici | Her rapor için ayrı PDF export action'ı, aynı filtrelerle çalışır. |

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



