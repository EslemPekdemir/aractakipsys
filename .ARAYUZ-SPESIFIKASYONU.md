# ARAYÜZ (UI) SPESİFİKASYONU — Araç Takip ve Görevlendirme Sistemi

> Bu dosya `AGENTS.md`, `VERI-MODELI.md` ve `ENDPOINT-SPESIFIKASYONU.md` ile birlikte okunmalıdır. Amaç: Razor View'ların hangi bileşenleri, hangi filtreleri, hangi durum renklerini ve responsive standartları içereceğini netleştirmek.

---

## 0. GENEL TASARIM VE MOBİL RESPONSİVE KURALLARI

- **CSS Framework:** Bootstrap 5 (CDN veya yerel wwwroot kütüphanesi).
- **İki Ayrı Düzen (Layout):**
  - `_AdminLayout.cshtml` — Masaüstü öncelikli: sol sabit/katlanabilir sidebar + üst navbar, geniş tablo alanı, `table-responsive` sarmalayıcıları.
  - `_SurucuLayout.cshtml` — **Mobil öncelikli (Mobile-first):** üstte sade başlık, alt kısımda parmakla kolay erişilebilir dokunmatik butonlar ve gezinme barı (Bottom Navigation), tek sütun dikey kart akışı.
- **Dokunmatik Hedef Boyutu Standardı:** Mobil ekranlarda (`_SurucuLayout` ve modal butonları) buton yükseklikleri **en az 48px** (`min-height: 48px`) olmalı, butonlar arası yeterli dokunma boşluğu (`gap-2` / `margin`) bulunmalıdır.
- **Yazı Tipi ve Renk Paleti:** Düzce Üniversitesi kurumsal kimliğine uygun kurumsal lacivert (`#1e3c72`, `#2a5298`) ve nötr gri tonlar.
- **Boş Durum (Empty State):** Her filtre ve listede kayıt yoksa "Henüz kayıt bulunamadı" ikonu + açıklayıcı mesaj gösterilir; boş/çirkin tablo bırakılmaz.
- **Yükleniyor (Loading) Durumu:** AJAX çağrılarında çift tıklamayı önlemek amacıyla buton `disabled` edilir ve spinner ikonu gösterilir.

---

## 1. DURUM RENK VE ETİKET STANDARDI

Tüm listelerde, kartlarda, filtrelerde ve detay sayfalarında aşağıdaki rozet renkleri **istisnasız** aynı kullanılır:

### 1.1 Araç Durumları (VehicleStatus)
| Durum | Etiket (TR) | Bootstrap Rozet (Badge) Sınıfı |
|---|---|---|
| Idle | Boşta | `bg-success` (Yeşil) |
| OnDuty | Görevde | `bg-primary` (Mavi) |
| Maintenance | Bakımda | `bg-warning text-dark` (Sarı) |
| DocumentExpired | Belgesi Geçersiz | `bg-danger` (Kırmızı) — Muayene/sigortası geçen araçlar |
| OutOfService | Hizmet Dışı | `bg-secondary` (Koyu Gri) |

### 1.2 Görev Durumları (AssignmentStatus)
| Durum | Etiket (TR) | Bootstrap Rozet (Badge) Sınıfı |
|---|---|---|
| Planned | Planlandı | `bg-secondary` (Gri) |
| InProgress | Devam Ediyor (Seyirde) | `bg-primary` (Mavi) |
| Completed | Tamamlandı | `bg-success` (Yeşil) |
| Cancelled | İptal Edildi | `bg-danger` (Kırmızı) |

### 1.3 Talep Durumları (VehicleRequestStatus)
| Durum | Etiket (TR) | Bootstrap Rozet (Badge) Sınıfı |
|---|---|---|
| Pending | Beklemede | `bg-warning text-dark` (Sarı) |
| Approved / Fulfilled | Karşılandı (Göreve Dönüştü) | `bg-success` (Yeşil) |
| Rejected | Reddedildi | `bg-danger` (Kırmızı) |

---

## 2. ÜCRET VE GÖREV TERMİNOLOJİSİ STANDARDI

Kurum içi üniversite araç yönetim standartları gereğince sistemdeki tüm finansal ve operasyonel terimler sadeleştirilmiştir:
1. **"Yevmiye" Kaldırıldı → "Görev Ücreti / Sefer Ücreti":** Arayüzlerde, PDF raporlarında ve formlarda eski "Yevmiye" ibareleri yerine doğrudan **"Görev Ücreti"**, **"Saatlik Ücret"** ve **"Harcırah / Konaklama"** terimleri kullanılır.
2. **"Kadrolu Personel" Ayrımı Kaldırıldı:** Sistemdeki tüm sürücüler üniversite bünyesindeki şoför personeli olarak ele alınır; formlarda veya filtrelerde kafa karışıklığı yaratan "Kadrolu Personel" seçeneği veya etiketi bulunmaz.
3. **Şoför Ücret Girişi:** Sabit veya otomatik bir ücret zorlaması yoktur. Şef veya Admin, görevi tamamlarken/onaylarken saatlik ücreti (`HourlyRate`) serbestçe belirler; toplam tutar görev süresiyle çarpılarak hesaplanır.

---

## 3. GÜVENLİK VE GİRİŞ — SVG CAPTCHA SİSTEMİ (`/Account/Login`)

- **Harici Bağımlılıksız Yerel SVG Üretimi:** Google reCAPTCHA gibi dış servislere bağımlılık olmaksızın, sunucu tarafında (`CaptchaService`) tamamen dinamik SVG formatında üretilir.
- **Görsel Güvenlik Özellikleri:**
  - 5 karakterli alfasayısal kod (karıştırılabilen `0, O, 1, I, l` karakterleri havuzdan çıkarılmıştır).
  - Harflerin arkasında rastgele gürültü çizgileri (`stroke`), daireler ve her harfe hafif rastgele açı (`transform="rotate(...)"`) verilerek OCR botlarına karşı koruma sağlanır.
  - SVG, `data:image/svg+xml;base64,...` URI formatında doğrudan HTML `<img>` etiketine basılır.
- **Şifreli Token Mekanizması:** Form içerisinde gizli bir `CaptchaToken` input'u bulunur. Token, ASP.NET Core `IDataProtector` ile şifrelenir; veritabanı veya oturum şişirmesi yapmadan durumsuz (stateless) doğrulanır ve **3 dakika** geçerlilik süresine sahiptir.
- **Tek Tıkla Yenileme Butonu:** SVG görselinin hemen yanında yer alan `btn-outline-secondary` dairesel yenile butonu, sayfayı yenilemeden AJAX ile yeni SVG görseli ve yeni token alır.
- **Hata Mesajı:** "Güvenlik kodu hatalı veya süresi dolmuş." uyarısı kırmızı renkli geri bildirimle gösterilir.

---

## 4. İMZALI EVRAK ARŞİVİ VE TAKİBİ (`/Assignment/SignedDocuments`)

Resmi görev emirlerine ait ıslak imzalı taranmış evrakların merkezi arşiv ve denetim ekranıdır:

- **Üst Filtre ve Arama Paneli:**
  - **Araç Seçimi:** Select2 aramalı açılır kutu (Plaka & Marka).
  - **Şoför Seçimi:** Select2 açılır kutu.
  - **Tarih Aralığı:** Başlangıç ve Bitiş tarih seçicileri.
  - **Metin Arama Kutusu:** Görev amacı, birim veya dosya adına göre canlı filtreleme.
  - **İşlem Butonları:** "Filtrele" (`btn-primary`) ve formu sıfırlayan **"Temizle"** (`btn-outline-secondary`) butonu.
- **Arşiv Kart / Tablo Görünümü:**
  - Plaka, Görevli Şoför, Görev Tarihi, Görev Amacı.
  - Dosya Adı ve Boyutu (PDF / Resim rozeti).
  - Evrakı Sisteme Yükleyen Yetkili ve Yükleme Zamanı.
  - **Güvenli İndir Butonu:** `DownloadSignedDocument` uç noktası üzerinden `App_Data/AssignmentDocuments` klasöründen güvenli akışla indirme.
- **Sunucu Taraflı Sayfalama:** Sayfa başına kayıt sınırlaması ve alt sayfalama butonları.

---

## 5. DEVAM EDEN GÖREVLERDE SÜRE UZATMA (`InProgress` Görev Detayı)

Seyir halindeki aracın gecikmesi veya görevin uzaması durumunda idari tutarlılığı sağlayan arayüz bileşenidir:

- **Konum:** `/Assignment/Details/{id}` sayfası, sadece görev `InProgress` (veya `Planned`) durumundayken görünür.
- **Tetikleyici Buton:** Görev detay kartı üzerinde belirgin **"Süreyi Uzat"** (`btn-warning`) butonu.
- **Süre Uzatma Modal Penceresi (`#extendDurationModal`):**
  - **Mevcut Bitiş Tarihi:** Bilgilendirme amaçlı salt okunur gösterilir.
  - **Yeni Planlanan Bitiş:** `datetime-local` seçici (mevcut bitiş saatinden daha ileri bir tarih olmak zorundadır).
  - **Gecikme / Uzatma Gerekçesi (Zorunlu):** Textarea alanı (örn. "Hava muhalefeti ve yol çalışması nedeniyle sefer 3 saat uzadı").
- **Çakışma Kontrolü Uyarısı:** Seçilen yeni bitiş tarihine kadar aracın veya şoförün başka bir planlı görevi varsa, sunucu işlemi reddeder ve kullanıcıya çakışan görevin detayını hata mesajı olarak gösterir.
- **Kayıt İzi:** Uzatma gerekçesi `[Süre Uzatma - dd.MM.yyyy HH:mm]: {gerekçe}` damgasıyla görev notlarına işlenir ve `AuditLog` kaydı oluşturulur.

---

## 6. YÖNETİCİ VE ŞEF PANELİ — DASHBOARD (`/Home/Index`)

- **4 Ana KPI Özet Kartı:**
  - Toplam Araç (Boşta / Görevde / Bakımda / Belgesi Geçersiz kırılımlı).
  - Aktif Görevler (`Status = InProgress`).
  - Bugünkü Planlanan Görevler.
  - Bakımdaki Araçlar.
- **Hızlı Plaka Arama Kutusu (AJAX):** Plaka yazıldığında aracın anlık durumu, sigorta/muayene tarihi, ceza notu ve son görev yapan şoförünü açılır kartta gösterir.
- **Devam Eden ve Planlanan Görevler Kutusu:** Sol tarafta aktif seyir tablosu.
- **Bakımdaki Araçlar Kutusu:** Sağ tarafta servis ve tamirdeki araçlar listesi. Kart yüksekliği alt boşluğa taşmayacak şekilde içerideki 5-6 araç boyutuna göre dinamiktir; solunda kurumsal sarı şerit (`border-left border-warning`) bulunur.
- **Navbar Bildirim Çanları & Toast:**
  - Muayene/sigortası yaklaşan araçlar için uyarı çanı.
  - Bekleyen araç talepleri için AJAX periyodik kontrol çanı (30 sn) + ilk girişte Web Audio bip sesi ve Bootstrap Toast bildirimi.

---

## 7. ARAÇ ENVANTERİ EKRANLARI (`/Vehicle`)

- **Index Sayfası:** Durum (Boşta, Görevde, Bakımda, Belgesi Geçersiz), Tip ve arama filtreleri. Sayfalamalı tablo.
- **Create / Edit Formu:**
  - Sol Sütun: Plaka, Marka, Model, Yıl, Renk.
  - Sağ Sütun: Araç Tipi, Yakıt Tipi, Fenni Muayene Başlangıç-Bitiş Tarihi, Trafik Sigortası Başlangıç-Bitiş Tarihi.
  - Lastik ve Notlar Bölümü: Lastik Sezonu (Yazlık/Kışlık), Lastik Markası, Üretim Yılı (DOT), Trafik Cezası ve Şef Notları.
  - **Edit Koruması:** `CurrentKm` ve `Status` düzenleme ekranından elle değiştirilemez (sistem ve şoför onaylarıyla otomatik yürütülür).

---

## 8. ŞOFÖR MOBİL EKRANLARI (`/Surucu`) — RESPONSIVE STANDARTLAR

Şoförlerin sistemi %90 akıllı telefonlardan kullanacağı varsayımıyla uygulanmış kurallar:

- **Genel Mobil Görünüm (`/Surucu/Index`):**
  - Tablo (Grid) kesinlikle kullanılmaz; dikey dokunmatik kartlar (`card`) kullanılır.
  - En üstte: Varsa devam eden görev (`InProgress`) büyük ve vurgulu mavi kart olarak yer alır.
  - Ortada: Bugünkü planlanan görevler (`Planned`).
  - En altta: Geçmiş tamamlanan görevler (akordeon / collapse, varsayılan kapalı).
- **Görev Detay ve Operasyon Ekranı (`/Surucu/Detay/{id}`):**
  - **Başlangıcı Bildir Butonu:** Durum `Planned` iken ekranın altında tam genişlikte yeşil büyük dokunmatik buton (`min-height: 48px`). Tıklandığında Başlangıç KM modalı açılır.
  - **Bitişi Bildir Butonu:** Durum `InProgress` iken aynı yerde büyük buton. Tıklandığında Bitiş KM + Teslim Notu modalı açılır.
  - **Onay Bekliyor Durumu:** Şoför bildirimi yaptıktan sonra ekranda turuncu rozetle "⏳ Şef Onayı Bekleniyor" mesajı çıkar ve girilen değerler kilitlenir.
- **İlk Giriş Şifre Değiştirme (`/Surucu/ChangePassword`):** Şoför geçici şifreyle ilk girdiğinde zorunlu olarak karşılayan mobil uyumlu parola yenileme formu.

---

## 9. RESMİ RAPOR MERKEZİ VE PDF ÇIKTILARI (`/Report`)

Yöneticilerin resmi denetim ve kurum içi raporlama yapmasını sağlayan 3 sekmeli modül:

1. **Sefer & Kullanım Raporu (`/Report/Index`):** Tamamlanmış (`Completed`) görevlerin araç ve şoför bazlı sefer sayısı, yapılan KM ve görev ücreti dökümü. "PDF İndir" butonu ile resmi Görev Kullanım Dökümü çıktısı üretir.
2. **Bakım & Maliyet Raporu (`/Report/Maintenance`):** Araçların serviste kaldığı süreler, yapılan işlemler, tarih-saat dökümü ve bakım maliyeti raporu + PDF çıktısı.
3. **Muayene & Sigorta Takip Raporu (`/Report/InsuranceInspection`):** Filodaki tüm araçların fenni muayene ve sigorta geçerlilik tarihleri, 30 gün kalanlar ve süresi geçenler renkli dökümü + PDF çıktısı.

---

## 10. PERSONEL ARAÇ TALEP MODÜLÜ (`/VehicleRequest` & `/AracTalebi`)

- **Dış / Anonim Başvuru Formu (`/AracTalebi`):** Üniversite personeli giriş yapmadan Ad, Soyad, `@duzce.edu.tr` uzantılı e-posta, Telefon, Birim/Fakülte (`<datalist>` otomatik tamamlama) ve Talep Gerekçesi ile araç talep eder.
- **Yönetici Talep Havuzu (`/VehicleRequest/Index`):** Bekleyen, Karşılanan ve Reddedilen sekmeleri.
- **Talebi Göreve Dönüştürme (`/VehicleRequest/Fulfill/{id}`):** Talebi onaylayan Şef/Admin, uygun araç ve şoförü seçerek tek tıkla resmi bir `Assignment` (Görev) kaydına dönüştürür; talep sahibine otomatik e-posta bildirimi gönderilir.

---

## 11. KULLANICI PROFİLİ VE DİNAMİK ROZET (BAŞ HARF) AVATAR SİSTEMİ (`/Account/Profile` ve `_AdminLayout`)

Kullanıcı deneyimini kurumsallaştırmak ve genel stok resim (`profile_small.jpg`) kirliliğini önlemek amacıyla uygulanan profil ve avatar mimarisi:

- **Dinamik Baş Harf (Rozet) Avatarı (`AvatarHelper.cs`):**
  - Profil fotoğrafı yüklememiş veya fotoğrafını kaldırmış tüm kullanıcılar (Admin, Şef/Kullanıcı, Şoför) için Ad ve Soyadın baş harflerinden (`GetInitials(FullName, UserName)`) 2 karakterlik kurumsal rozet üretilir (Örn: "Ahmet Yılmaz" → **AY**, "Sistem Yöneticisi" → **SY**, "Eslem Şef" → **EŞ**).
  - Düzce Üniversitesi kurumsal lacivert degrade tonlarında dairesel rozet (`linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)`) olarak çizilir.
- **Sidebar Entegrasyonu (`_AdminLayout.cshtml`):**
  - Sol menü kullanıcı özet alanında, kullanıcının özel bir fotoğrafı yoksa 42x42px boyutunda beyaz harfli rozet avatar gösterilir.
- **Profil Sayfası Kartı (`/Account/Profile`):**
  - Sayfa başında 120x120px büyük profil görseli veya rozet avatar alanı yer alır.
  - **Canlı Dosya Önizlemesi:** Kullanıcı yeni bir resim dosyası seçtiğinde JavaScript anında devreye girerek rozeti gizler ve seçilen fotoğrafı önizleme kutusuna yansıtır.
  - **Koşullu "Fotoğrafı Kaldır" Butonu:** Yalnızca veritabanında özel bir `ProfilePictureUrl` kaydı bulunan kullanıcılara kırmızı `Fotoğrafı Kaldır` butonu gösterilir; stok/rozet modundaki kullanıcılara bu buton gösterilmez.
- **Bağımsız Profil Güncelleme ve Tarayıcı Güvenliği:**
  - `autocomplete="new-password"` ile tarayıcıların otomatik doldurma (autofill) mekanizması kilitlenmiştir.
  - Kullanıcı şifresini değiştirmek zorunda olmadan yalnızca Ad Soyad, E-posta, Telefon ve Fotoğrafını güncelleyebilir. Şifre alanları sadece yeni şifre belirlenmek istendiğinde doğrulanır.

