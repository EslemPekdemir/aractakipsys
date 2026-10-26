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
- **Rol Bazlı Görünürlük ve Salt Okunur Modu (Faz 1-A):**
  - **GenelSekreter (Salt Okunur):** Tüm dashboard, takvim, raporlar, listeler, detay sayfaları ve AuditLog açıktır. Ancak veritabanında değişiklik yapabilecek form submit butonları, ekle/düzenle/sil bağlantıları ve aksiyon modalları CSS seviyesinde gizlenir; GET'li filtre/arama formları, sayfalama, "Önizle", "İndir" butonları gizlenmez ve tam çalışır. Kullanıcı Yönetimi (`/Sef`) menüde görünmez.
  - **DaireBaskani:** Admin ile aynı butonlara ve ekranlara sahiptir (Talep Seçenekleri dahil). Kullanıcı Yönetimi (`/Sef`) tablosunda Admin, DaireBaskani ve GenelSekreter hesaplarının düzenle/şifre/dondur/sil butonları sunucu korumasına ek olarak arayüzde gizlenir ve "Yetki Dışı" gösterilir; yeni şef eklerken rol seçimi gizlidir (yalnızca Şef oluşturabilir).

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

---

## 12. TAŞIT GÖREV EMRİ (RESMİ BELGE FORMU — QuestPDF)

- **Biçim ve Sayfa Yapısı:** A4 dikey (Portrait), kurumsal ve matbu form düzeninde. Sol üstte kurum logosu (`/images/logo.png` veya vektörel yer tutucu), üst başlık: *"T.C. DÜZCE ÜNİVERSİTESİ REKTÖRLÜĞÜ — TAŞIT GÖREV EMRİ"*.
- **Tek Nüsha Standardı:** Belge **tek sayfa (1 tam A4)** olarak üretilir; sayfa üzerinde herhangi bir nüsha etiketi yer almaz.
- **Dolu / Boş Çıktı Seçeneği:** Görev detay ekranındaki indirme butonunda bölünmüş açılır menü (Split Dropdown) yer alır:
  - *"Taşıt Görev Emri (Dolu)"*: Sistemdeki çıkış/giriş saat ve KM bilgileriyle üretilir.
  - *"Taşıt Görev Emri (Boş Çıktı)"*: Çıkış/giriş saat ve KM alanları elle doldurulmak üzere boş çizgilerle basılır.
- **Güzergah Dışı Sevk Alanı:** Belge üzerinde her zaman boş basılır (yetkililer tarafından sahada elle doldurulması için). Doldurulan bilgiler sonradan Şef/Admin tarafından sistemdeki *"Güzergah Dışı Sevk"* modalı ile sisteme işlenebilir.
- **Kontrol Kutuları ve Paraf:** Yağ, Su, Yakıt, Hidrolik ve Lastik kontrol kutuları; şoför onaylamışsa `[X]`, onaylamamışsa `[ ]` kutucukları ve şoför paraf alanı basılır.
- **Yetkilendirme:** Şoförler mobil panelden yalnızca kendi görev emrini indirebilir (yatay yetki kontrolü ile korunmuştur).

---

## 13. ŞOFÖR ARAÇ KONTROL TİKLERİ (`/Surucu/Detay/{id}`)

- **Mobil Kart ve Modal:** Şoför görev detay ekranında *"Araç Kontrolleri"* kartı yer alır.
- **5 Zorunlu Kontrol:** Yağ Seviyesi, Radyatör Suyu, Yakıt Seviyesi, Hidrolik Yağları ve Lastik Basınç/Durumu.
- **Kayıt ve Yetki:** Şoför sürüş öncesinde veya sürüşü tamamlarken kontrol tiklerini işaretleyip tek tıkla kaydedebilir. İşaretlenen kontroller yeşil onay rozetleriyle sabitlenir ve `AuditLog`'a yazılır. Şef ve Admin görev detayında kontrol durumunu görebilir.

---

## 14. ARAÇ ARIZA BİLDİRİM FORMU VE ŞEF BİLDİRİM ÇANI (`/FaultReport`)

- **Şoför Mobil Bildirimi:** Şoför mobil panelinde bulunan kırmızı *"Arıza Bildir"* butonu ile arıza açıklamasını yazıp gönderebilir.
- **Şef Navbar Bildirim Çanı (`#navFaultDropdown`):** Bekleyen arıza bildirimleri için üst gezinti çubuğunda kırmızı sayaç rozetli çan simgesi yer alır. Her 30 saniyede bir AJAX ile arka planda kontrol edilir; yeni arıza tespit edildiğinde sesli uyarı ve toast bildirimi verilir.
- **Şef İnceleme ve Düzenleme Ekranı (`/FaultReport/Detail/{id}`):** Şef bildirimi inceleyip plaka, şoför, tarih, KM ve arıza açıklamasını düzenleyebilir, servis sevk notu düşerek onaylayabilir.
- **Resmi Arıza Bildirim Formu PDF'i:** QuestPDF ile hazırlanan resmi form; logo, başlık, araç künyesi, çok satırlı arıza açıklaması ve alt imza bloklarını (Ulaştırma Amiri ve Şoför) içerir. Şef hem onaylanmış formu hem de sahada elle doldurulmak üzere *"Boş Form"* indirebilir.
- **EBYS Damgası Kuralı:** Matbu formdaki EBYS üst veri damgası sistemsel olarak resmi PDF çıktısına **kesinlikle basılmaz**.
- **Araç Durumu Koruması:** Arıza bildirimi açılması aracın operasyonel durumunu otomatik olarak değiştirmez.

---

## 15. FAZ 2 - A KISMI ARAYÜZ STANDARTLARI (ÇOKLU ŞOFÖR, ŞOFÖRSÜZ, DIŞ ŞOFÖR VE GÖREV YERİ/GÜZERGAH)

- **Görev Yeri, Amaç ve Güzergah Alanları:**
  - `Create` ve `Edit` formlarında "Görev Yeri" (`Destination`), "Görevin Konusu" (`Purpose`) ve "Güzergah" (`Route`) 3 ayrı serbest metin kutusu olarak konumlandırılır.
- **Üç Şoför Modu Geçişi (Form):**
  - "Kurum Şoförü", "Kurum Dışı Şoför" ve "Şoförsüz Görevlendirme" seçenekleri radyo/toggle şeklinde birbirini dışlar.
  - "Ek Şoför Ekle" butonu: Kurum şoföründe tıklandığında dinamik Select2 satırı açar, her satır için canlı müsaitlik ve ehliyet uyarısı gösterir.
- **Detay Ekranı (`/Assignment/Details/{id}`):**
  - Çoklu şoför durumunda tüm şoförler listelenir.
  - "Görev Emri (PDF)" butonunda her şoför için ayrı indirme bağlantısı içeren açılır menü sunulur.
  - Şoförsüz ve dış şoförlü görevlerde Şef/Admin için doğrudan Başlat (`StartDirect`) ve Bitir (`EndDirect`) butonları aktiftir.
- **Taşıt Görev Emri (PDF):**
  - "Gidilen Yer" ile "Görevin Konusu" arasına tek satırlık "Güzergah" satırı eklenir (tek sayfa A4 matbu düzeni bozulmaz).
  - Şoför kutusunda yalnızca Ad Soyad basılır (dış şoförde Ad Soyad + Birim; şoförsüz görevde "Şoförsüz"). TC, sicil veya ehliyet basılmaz.

---

## 16. FAZ 2 - B KISMI ARAYÜZ STANDARTLARI (SÜRE FORMATI, KM UYARISI, ŞEF DÜZELTMESİ VE GÜZERGAH DEĞİŞTİRME)

- **16.1 Süre Gösterimi Standardı ("X sa Y dk"):**
  - Tüm ekranlarda (Admin/Şef listesi, Görev Detay, Şoför Mobil ekranları) ve PDF raporlarında görev süreleri `"2 sa 40 dk"` formatında (saat ve 0-59 dakika) gösterilir.
  - Ekranda ve PDF'lerde kesinlikle ondalık saat (örn. "2.67 saat") gösterilmez.
  - Ortak yardımcı sınıf `DurationHelper` (`.ToFormattedDuration()`, `.FormatDuration(...)`) üzerinden tek merkezden işletilir. Ücret hesaplama matematiği (saat x ücret) korunur.
- **16.2 200 KM Mesafesi Uyarısı ve Kırmızı Ünlem İkonu:**
  - Şoför görevi bitirirken (`Bitir` modalı) veya Şef görevi onaylarken (`approveEndCalculationModal`), yapılan mesafe (`EndKm - StartKm`) 200 KM'yi aştığında kırmızı ünlem ikonlu (`fa-exclamation-circle text-danger`) dikkat uyarısı gösterilir.
  - Eşik değeri `appsettings.json > VehicleSettings:TripDistanceThreshold` üzerinden yapılandırılır ve `KmValidationService` ile doğrulanır.
- **16.3 Şef KM Düzeltmesi (Onay Modalı):**
  - Şef onay modalında Başlangıç KM (`ConfirmedStartKm`) ve Bitiş KM (`ConfirmedEndKm`) sayısal giriş alanları yer alır.
  - Şoförün bildirdiği orijinal değerler referans olarak gösterilir; Şef değerleri düzelttiğinde eski/yeni değerler `AuditLog`'a işlenir ve `Vehicle.CurrentKm` güncel kalır.
- **16.4 Seferde Güzergah Değiştirme (Şoför Mobil Paneli):**
  - Seferdeki (`InProgress`) görev kartında "Güzergah Değiştir" butonu yer alır.
  - Tıklandığında `#changeRouteModal` açılır; yeni güzergah ve değişiklik gerekçesi girilerek kaydedilir.
  - Modal tanımı HTML hiyerarşisinde kart veya diğer modalların içine gömülmeyip `</body>` etiketi öncesinde konumlandırılmıştır.
  - Göreve atanmış tüm kurum şoförleri sefer esnasında birden fazla kez güzergah güncelleyebilir.
  - Admin ve Şef görev detay sayfasında *"Güzergah Değişiklikleri Tarihçesi"* kartında bu değişiklikler kronolojik olarak listelenir. Bu geçmiş bilgisi resmi PDF çıktılarına basılmaz.
- **16.5 Şef/Admin Güzergah Değişikliği Bildirim Çanı (`#navRouteDropdown`):**
  - Üst gezinim çubuğunda (navbar) `FaultReport` bildirim yapısıyla birebir aynı desende mavi yol simgeli (`fa-road text-info`) ve dinamik sayaç rozetli (`#navRouteBadge`) bildirim alanı eklenmiştir.
  - Her 30 saniyede bir `/Assignment/PendingRouteChangeNotificationSummary` sorgulanır; yeni güzergah bildirimi geldiğinde sağ üstte toast uyarısı (`toastr.warning`) gösterilir ve açılır menüde plaka, şoför, yeni güzergah, gerekçe ve zaman bilgisi listelenir. Tıklandığında ilgili görevin detay ekranına yönlendirir.

---

## 17. FAZ 4 ARAYÜZ STANDARTLARI (ÇOKLU EHLİYET, EHLİYET UYARILARI VE ŞOFÖR BİRİM DÜZENLEME)

- **17.1 Çoklu Ehliyet Düzenleyici (Şoför Ekle ve Düzenle):**
  - Şoför formlarında Ehliyet Belge No şoför kartında tek alan olarak muhafaza edilir.
  - Ehliyet Sınıfları ve Geçerlilik Tarihleri dinamik bir satır listesi olarak sunulur; her satırda Sınıf (B, C, D, E vb.) ve Geçerlilik Tarihi bulunur.
  - "+ Ehliyet Ekle" butonu ile yeni satır eklenir; satır silme butonu en az 1 satır kalması kuralıyla korunur.
- **17.2 Şoför Birimi (Department) Yetki Kısıtlaması:**
  - Şoförün Bağlı Olduğu Birim alanı yalnızca `Admin` rolündeki kullanıcılar tarafından düzenlenebilir.
  - Şef (`Kullanici`) rolündeki kullanıcılara alan `readonly` olarak sunulur ve kilit ikonuyla açıklama gösterilir. Sunucu tarafında da Admin olmayan isteklerin birim güncellemesi reddedilir/korunur.
- **17.3 Dashboard ve Şoför Paneli Ehliyet Uyarıları:**
  - Geçerlilik tarihi 30 gün içinde dolacak veya dolmuş olan ehliyetler; Dashboard'da ayrı bir uyarı kartında (Şoför, Birim, Sınıf, Geçerlilik Tarihi, Kalan Gün rozeti) listelenir.
  - Üst navbar'da `#navLicenseExpiryDropdown` bildirim çanı ve sayaç rozeti, sol sidebarda `#sidebarDriverBadge` rozeti bulunur.
  - 30 saniyelik periyodik kontrol sırasında yeni bir uyarı eklendiğinde toastr bildirimi tetiklenir; aynı bildirim sayısı için tekrar eden toast üretilmez.
  - Şoför mobil portalında (`/Surucu`) karşılama başlığının hemen altında şoförün kendi süresi dolmuş veya yaklaşan ehliyetleri için özel uyarı kartı gösterilir.

---

## 18. FAZ 5-A ARAYÜZ STANDARTLARI (ARAÇ BELGELERİ PANELİ — MUAYENE, TRAFİK SİGORTASI, KASKO)

- **18.1 Araç Belgeleri Sayfası (`/Vehicle/Documents`):**
  - Sol ana menüde "Araçlar" altına **"Araç Belgeleri"** bağlantısı eklenmiştir.
  - Sayfa başında Select2 arama destekli araç seçim dropdown'ı bulunur.
  - Seçili araca ait özet künye kartında araç plakası, marka/model, KM ve güncel geçerli muayene/sigorta/kasko bitiş tarihleri rozetlerle özetlenir.
- **18.2 3 Sekmeli Belge Yönetimi (Muayene | Trafik Sigortası | Kasko):**
  - Her sekmede o araca ait tüm geçmiş kayıtlar (yıllara göre azalan sırada) listelenir.
  - Güncel geçerli olan kayıt yeşil satır ve "GÜNCEL GEÇERLİ" rozetiyle açıkça vurgulanır.
  - Süresi dolan veya yaklaşan kayıtlar kırmızı/sarı rozetler ve kalan/geçen gün sayılarıyla gösterilir.
  - Her sekmede "Yeni Belge Kaydı Ekle" butonu ve satır bazlı "Düzenle" / "Sil" işlemleri yer alır.
  - Kayıt alanları: Başlangıç Tarihi, Bitiş Tarihi, Ödenen Tutar (Fiyat / Prim), Firma / İstasyon Adı, Poliçe No ve Açıklama.
- **18.3 Modallar ve Bildirim Bütünlüğü:**
  - `#addDocumentModal` ve `#editDocumentModal` tanımları HTML akışında `</body>` öncesinde kart ve tabloların dışında tanımlanmıştır.
  - Yeni bildirim zili eklenmemiş; mevcut üst navbar belge bildirim çanı (`#navDocExpiryDropdown`) ve Dashboard belge uyarıları yeni yapı ile tam uyumlu çalışmaktadır.

---

## 19. FAZ 5-B ARAYÜZ STANDARTLARI (LASTİK YÖNETİMİ VE TAKİBİ)

- **19.1 Araç Belgeleri Sayfası 4. Sekme ("Lastik"):**
  - `/Vehicle/Documents` sayfasında Muayene, Sigorta ve Kasko sekmelerinin yanına 4. sekme olarak **"Lastik"** eklenmiştir.
  - Sekme başlığında aktif lastik değişim uyarısı sayısını gösteren sayaç rozeti yer alır.
  - Üst özet künyesinde aracın güncel lastik markası, mevsimi ve DOT kodu gösterilir.
- **19.2 Lastik Kayıt Alanları ve Doğrulama:**
  - Alanlar: Marka, Ebat, Üretim Tarihi (DOT haftası/yılı "52/23" — istemci pattern regex `^(0[1-9]|[1-4][0-9]|5[0-3])\/[0-9]{2}$` ve sunucu tarafında zorunlu doğrulama), Mevsim (Yaz / Kış / Dört Mevsim), Takılma/Değişim Tarihi, Takıldığı Andaki Araç KM.
  - Opsiyonel Eşikler: "Değiştirilmesi Gereken KM" ve "Değiştirilmesi Gereken Tarih".
  - Kullanım KM'si (`Vehicle.CurrentKm - InstalledKm`) statik dondurulmaz; aracın anlık KM'sine göre dinamik hesaplanır. Eski taşınan kayıtlarda `InstalledKm` boş olduğundan "Bilinmiyor" ibaresiyle gösterilir.
- **19.3 Güncel Lastik Gösterimi ve Plaka Arama Kartı Entegrasyonu:**
  - Güncel (en son takılan) lastik kaydı yeşil satır ve "GÜNCEL" rozetiyle vurgulanır.
  - Araç Detay sayfasında (`/Vehicle/Details/{id}`) güncel lastik kartı (Marka, Ebat, Mevsim, DOT, Kullanım KM ve Durum) yer alır.
  - Dashboard Plaka Arama kartında (`/Home/SearchPlate`) sorgulanan aracın güncel lastik marka/ebat, mevsim ve kullanım durumu görüntülenir.
- **19.4 Lastik Modalları ve Soft Delete:**
  - `#addTireModal` ve `#editTireModal` modalları HTML akışında `</body>` öncesinde kartların dışında tanımlanmıştır.
  - Kayıtlar soft delete ile silinir; bir aracın sistemdeki tek/son aktif lastik kaydı silinemez ("Araçta en az bir kayıtlı lastik bulunmalıdır; son lastik kaydı silinemez" uyarısı).
- **19.5 Lastik Değişim Uyarıları ve Bildirimler:**
  - Değişim tarihine 30 gün kaldığında veya KM eşiği yaklaştığında (kalan <= 1000 KM) ya da aşıldığında Dashboard belge uyarılarında ve mevcut üst belge bildirim çanında uyarı listelenir (YENİ ZİL EKLENMEMİŞTİR).
  - Lastik uyarısı araç durumunu `DocumentExpired` (Belgesi Geçersiz) **yapmaz**.
- **19.6 QuestPDF Lastik Kullanım Raporu:**
  - Sayfa üzerindeki "Lastik Kullanım Raporu PDF" butonu ile `/Vehicle/ExportTiresPdf` endpoint'i üzerinden A4 yatay formatta resmi rapor dökümü alınır.

---

## 20. FAZ 7 - A KULLANIM VE MALİYET RAPORLARI

- **20.1 Kullanım Raporu (`/Report`):**
  - **Süre Sütunu:** Tablo ve PDF çıktısında satır bazında `DurationHelper` ile "2 sa 40 dk" biçiminde süre sütunu yer alır.
  - **Şoför Gruplama ve Alt Toplamlar:** Seferler şoför bazında gruplanır; her grup için "Genel Toplam Süre" ve "Genel Toplam Mesafe" alt toplamları hesaplanır.
  - **Deduplicated Genel Toplam:** Çoklu şoförlü görevler her şoförün altında yer alabilirken, tablonun ve PDF'in sonundaki RAPOR GENEL TOPLAMI (`DistinctAssignments`) her görevi YALNIZCA BİR KEZ sayarak çifte sayımı engeller.
- **20.2 Bakım Raporu ve Maliyet (`/Report/Maintenance`):**
  - Bakım modelinde `Cost` (decimal? 18,2) alanı ekleme/düzenleme formlarında opsiyonel olarak girilebilir.
  - Tablo ve PDF dökümünde her satırda Maliyet gösterilir. Boş olanlar "-" yazar ve toplamda sayılmaz.
  - Sayfa KPI kartında ve tablonun/PDF'in altındaki dip toplamda filtrelenen kayıtların "Genel Toplam Bakım Maliyeti" yer alır.
- **20.3 Sigorta & Muayene Raporu Maliyetleri (`/Report/InsuranceInspection`):**
  - `VehicleDocuments` tablosundan her aracın aktif/en son Trafik Sigortası, Fenni Muayene ve Kasko poliçeleri üzerinden ödenen tutarlar ve araç bazlı toplam belge maliyeti listelenir.
  - Tablo footer'ında ve PDF çıktısında kategori bazlı ve genel toplam belge harcamaları raporlanır.
- **20.4 Para Birimi Formatı:**
  - Tüm maliyet alanları Türkçe para formatında (`N2 ₺`, örn: `1.250,00 ₺`) sunulur.
- **20.5 QuestPDF Entegrasyonu:**
  - Tüm raporlar mevcut QuestPDF şablon yapısıyla, kurumsal A4 yatay formata uygun olarak üretilir.

---

## 21. FAZ 6 ARAÇ TALEP FORMU VE YÖNETİM PANELİ

- **21.1 Girişsiz Araç Talep Formu (`/AracTalebi`):**
  - **Birim Seçimi:** Serbest metin kaldırılmıştır; talep eden yalnızca admin tarafından tanımlanmış listeden açılır liste (`<select>`) ile seçim yapar.
  - **İstenen Araç Türü:** Admin tanımlı açılır listeden seçilir (`RequestVehicleTypes`). Teknik enum'dan bağımsızdır.
  - **Kaç Kişi:** Sayısal zorunlu alan (`PassengerCount >= 1`).
  - **Tarih-Saat Aralığı:** Görev başlangıç ve bitiş zamanı (`datetime-local`) zorunlu alanlardır; bitişin başlangıçtan sonra olması kontrol edilir.
  - **Görünür İkaz:** Formun üst kısmında kalıcı uyarı paneli yer alır: *"Yalnızca il içi görevlendirme yapılır."*
  - **Güvenlik:** Mevcut SVG captcha, IP rate-limiting (`vehicle-request-limit`) ve `@duzce.edu.tr` kurumsal e-posta doğrulaması aynen korunmuştur.
- **21.2 Yönetim Paneli — Talep Seçenekleri (`/VehicleRequest/Options`):**
  - Yalnızca `Admin` rolü erişebilir; menüden ve talep listesi başlığından doğrudan erişim butonu bulunur. Yetki sunucu tarafında da denetlenir.
  - **Birim Listesi Yönetimi:** Yeni birim ekleme, düzenleme ve aktif/pasif durumu değiştirme. Fiziksel silme yapılmaz (geçmiş talepler korunur).
  - **Araç Türü Listesi Yönetimi:** Yeni araç türü ekleme, düzenleme ve aktif/pasif durumu değiştirme.
  - **Modal Standardı:** `#departmentModal` ve `#vehicleTypeModal` modalları HTML akışında `</body>` öncesinde kartların dışında tanımlanmıştır.
- **21.3 Talep Karşılama Ekranı (`/VehicleRequest/Fulfill/{id}`) & E-Posta Gönderimi:**
  - Sol kartta talep sahibinin istediği araç türü ve yolcu sayısı rozet olarak gösterilir.
  - Formda Ulaşım Şefleri ve Şoförler arasından **İletişim Kişisi** seçimi yer alır.
  - **E-Posta Gizlilik Kuralı:** Talep onay e-postasında **görevli şoförün yalnızca ADI** gösterilir (şoförün telefonu ve e-postası gizlenir). Talep sahibinin koordinasyon için arayacağı kişi olarak seçilen İletişim Kişisinin adı, telefonu ve e-postası e-postaya eklenir.
  - **E-Posta Hata Toleransı:** Kurumsal SMTP yapılandırılmamışsa veya e-posta sunucusunda anlık hata oluşursa talep akışı ve görevlendirme çökmez; hata loglanır ve talep karşılama başarıyla tamamlanır.
- **21.4 Bildirim Çanları:**
  - Yeni bildirim çanı/zili eklenmemiştir; mevcut `sidebarRequestBadge` ve navbar bildirimleri aynen korunmuştur.

---

## 22. FAZ 8 PDF ÖNİZLEME BİLEŞENİ VE ARAYÜZ STANDARDI

- **22.1 Ortak Önizleme Modalı (`_PdfPreviewModal.cshtml`):**
  - Tüm PDF çıktıları (görev emri, arıza bildirim formu, kullanım raporu, bakım ve maliyet raporu, sigorta/muayene/kasko raporu, lastik raporu) indirme butonlarının hemen yanında "Önizle" butonu barındırır.
  - Önizleme modalı (`#globalPdfPreviewModal`), `_AdminLayout.cshtml` ve `_SurucuLayout.cshtml` içinde `</body>` etiketinden hemen önce, herhangi bir kart veya başka modalın dışında tanımlanmıştır.
  - Sayfa yenilenmeden iframe üzerinden PDF belgesi anlık yüklenir. Modal başlığında "İndir" ve "Kapat" butonları yer alır.
- **22.2 Mobil Uyumluluk ve Fallback:**
  - Mobil şoför portalında (`_SurucuLayout`) tam entegre çalışır.
  - Mobil tarayıcılarda iframe PDF desteği bulunmadığı durumlar için modal başlığında "Yeni Sekmede Aç" butonu yer alır.
  - Dış kütüphane (PDF.js vb.) kullanılmamış; tarayıcının yerel görüntüleyicisi ve `inline` Content-Disposition standardı kullanılmıştır.
- **22.3 Boş Formlar:**
  - Boş matbu görev emri ve boş matbu arıza bildirim formları da aynı önizleme modalı üzerinden doğrudan görüntülenebilir.

---

## 23. FAZ 7-B ARAÇ TALEPLERİ RAPORU VE PDF ÇIKTISI

- **23.1 Rapor Ekranı (`/Report/VehicleRequests`):**
  - **Erişim & Sekmeler:** Raporlar ana menüsünde `Araç Talepleri Raporu` sekmesi olarak yer alır. Tüm rapor ekranlarında (`/Report`, `/Report/Maintenance`, `/Report/InsuranceInspection`, `/Report/VehicleRequests`) sekmeler arası geçiş bulunur.
  - **Yetki:** `Admin` ve Şef (`Kullanici`) erişebilir; sunucu tarafında doğrulanır.
  - **Filtreler:** Tarih aralığı filtresi (Başlangıç ve Bitiş) talep oluşturulma tarihine (`CreatedAt`) göre uygulanır. Eski kayıtlarda istenen tarih alanı boş olduğu için filtreleme tutarlılığı sağlanmıştır.
  - **KPI Özet Kartları:** Toplam Talep, Karşılanan Talep, Reddedilen Talep, Bekleyen / İncelenen Talep sayılarını gösteren kurumsal renkli istatistik kartları.
  - **Tablo Sütunları:**
    1. Birim (Birim adı boş kayıtlar "Belirtilmemiş" satırında toplanır, eşleşen birimler büyük/küçük harf duyarsız tek satırda birleştirilir).
    2. Toplam Talep Sayısı
    3. Karşılanan (Fulfilled)
    4. Reddedilen (Rejected)
    5. Bekleyen / Diğer (Pending)
  - **Genel Toplam Satırı:** Tablonun sonunda tüm sütunların dip toplamını gösteren vurgulu satır.
  - **Durum Eşleme Notu:** Tablonun altında koddaki `VehicleRequestStatus` durum eşlemesini açıklayan tek satırlık kurumsal bilgilendirme bandı yer alır.
- **23.2 PDF Çıktısı ve Önizleme:**
  - **PDF Yapısı:** QuestPDF tabanlı A4 Yatay (Landscape) kurumsal şablon. Düzce Üniversitesi başlığı, filtre/tarih bilgisi, özet kartları, veri tablosu, genel toplam satırı ve durum eşleme dipnotu içerir.
  - **Önizleme Butonu:** Ortak `_PdfPreviewModal` bileşenini kullanır (`openPdfPreview('/Report/ExportVehicleRequestsPdf?StartDate=...&EndDate=...&preview=true', '...')`).
  - **İndirme Butonu:** `preview` parametresi olmadan doğrudan PDF indirir.

---

## 24. FAZ 9 DASHBOARD TAKVİMİ VE GÜNLÜK DETAY MODALI

- **24.1 Dashboard Takvim Bileşeni (`Views/Home/Index.cshtml`):**
  - Dış kütüphane (FullCalendar vb.) eklenmemiştir; Razor, CSS Grid ve mevcut Inspinia/jQuery bileşenleri ile 7 sütunlu aylık ızgara olarak uygulanmıştır.
  - Önceki Ay, Sonraki Ay ve Bugün butonları ile dinamik ay değişimi sağlanır.
  - **Aylık Genel Görünüm:** Sade tutulmuştur. Her güne yalnızca araç doluluğu (o gün göreve çıkan/planlı araç sayısı) rozet olarak işaretlenir. Bakım, belge ve ehliyet bitişleri için mini renkli gösterge noktaları yer alır.
- **24.2 Günlük Detay Modalı (`#dashboardCalendarDayModal`):**
  - Bir güne tıklandığında sayfa yenilenmeden açılır. HTML akışında `</body>` öncesinde kart dışında konumlandırılmıştır.
  - **İçerik Bölümleri:**
    1. Planlı ve Seyirdeki Görevler (Araç plaka/marka, şoförler, kalkış/varış saati, güzergah/hedef, amaç, durum ve detay bağlantısı).
    2. Bakımda Olan Araçlar (Araç, bakım türü, giriş tarihi, servis/firma ve bakım yönetim bağlantısı).
    3. Süresi Dolan / Dolacak Belgeler (Periyodik Muayene, Trafik Sigortası, Kasko Poliçesi, Lastik Değişim tarihi).
    4. Geçerlilik Süresi Dolan Şoför Ehliyetleri (Şoför adı, birim, ehliyet sınıfı, geçerlilik bitiş tarihi).
- **24.3 Renk ve Tasarım Uyumu:**
  - Mevcut kurumsal renk kodları ve durum rozetleri (`bg-primary`, `bg-warning`, `bg-danger`, `bg-info`) ile birebir uyumludur.
  - Yeni bildirim zili/çanı eklenmemiştir.

---

## 25. FAZ 1-B YETKİ DEVRİ (VEKÂLET) VE AUDITLOG DENETİM İZİ ARAYÜZÜ

- **25.1 Yetki Devri Ekranı (`/RoleDelegation`):**
  - **Erişim:** Sol menüde "Yetki Devri (Vekâlet)" linki üzerinden erişilir. Admin, Daire Başkanı, Genel Sekreter ve Şefler görür. Genel Sekreter için ekran salt okunurdur ("Yetkiyi Devret" ve "Sonlandır" butonları gizlenir).
  - **KPI Kartları:** Aktif Vekâlet Sayısı, Planlı / İleri Tarihli Sayısı, Tamamlanan Sayısı, İptal Edilen Sayısı.
  - **Vekâlet Listesi Tablosu:**
    1. Belge / Id (`YD-00001` formatında)
    2. Görevi Devreden (Asıl Şef)
    3. Görevi Devralan (Vekil personel ve asıl rolü)
    4. Vekâlet Tarih Aralığı (Başlangıç - Bitiş)
    5. Durum Rozeti (Aktif [yeşil], Planlı [mavi], Bitti [gri], İptal Edildi [kırmızı])
    6. Gerekçe & İptal Notu
    7. İşlemler: Belge Önizle (`_PdfPreviewModal` ile iframe içinde), PDF İndir ve Sonlandır (yalnızca aktif/planlı ve yetkili olunan kayıtlarda).
  - **Modal Mimarisi:** `#modalCreateDelegation` ve `#modalRevokeDelegation` HTML yapıları sayfa gövdesinde diğer kartların/container'ların dışında, `</body>` kapanışından hemen önce yer alır. Böylece iç içe modal backdrop donması kesin olarak engellenir.
  - **Çakışma Listesi Uyarısı:** Devralan şoförün ilgili aralıkta onaylı veya seyirde görevi varsa modal içinde kırmızı liste olarak gösterilir ve işlem onaylanmaz.
- **25.2 Üst Bilgilendirme Bandı (`_AdminLayout.cshtml`):**
  - **Asıl Şef İçin:** Görevini devrettiği aktif bir vekâlet varsa sayfa tepesinde mavi bilgi bandı çıkar: *"Şu anda Ulaşım Şefliği yetkileriniz geçici olarak [Vekil Adı] personeline devredilmiştir."* ve doğrudan "Devri Sonlandır" butonu sunulur.
  - **Vekil Personel İçin:** Bir Şefin görevini vekâleten yürütüyorsa sayfa tepesinde yeşil/kehribar bant çıkar: *"[Asıl Şef Adı] adına vekâleten Ulaşım Şefliği yetkilerini kullanmaktasınız."*
- **25.3 Denetim Günlüğü (`/AuditLog`):**
  - Vekâleten yapılan işlemlerde işlemi yapan sütununda: `Ali Demir (Ahmet Şef adına vekâleten [YD-00001])` şeklinde mavi rozet gösterilir.
- **25.4 Yetki Devri Belgesi (QuestPDF):**
  - T.C. DÜZCE ÜNİVERSİTESİ resmi anteti, "YETKİ DEVRİ (VEKÂLET) BELGESİ" başlığı, Belge No, Düzenleme Tarihi, "Dayanak: ........", tarafların bilgileri, vekâlet aralığı ve gerekçesi ile iki imza kutusu (Görevi Devreden, Görevi Devralan) içerir.

---

## 26. SİSTEM TANIMLARI (DİNAMİK SEÇENEK LİSTELERİ) ARAYÜZÜ

- **26.1 Sayfa Erişimi ve Navigasyon:**
  - Sol ana menüde "Sistem Tanımları" bağlantısı (`/SystemDefinition`).
  - Yalnızca `Admin` ve `DaireBaskani` tam yönetim yetkisiyle düzenleyebilir.
  - `GenelSekreter` yalnızca izleyebilir (salt okunur mod: ekle, adı düzenle ve durum değiştirme butonları gizlenir).
  - `Kullanici` (Şef) ve `Driver` (Şoför) bu sayfaya erişemez (`[Authorize(Roles = "Admin,DaireBaskani,GenelSekreter")]`).
- **26.2 İki Sütunlu Sayfa Düzeni:**
  - **Sol Panel (Kategoriler):** 5 sabit kategori listelenir (`TireSeason`, `FuelType`, `MaintenanceType`, `VehicleType`, `LicenseClass`). Her kategorinin simgesi, adı ve aktif seçenek sayısı rozeti gösterilir. Altında "Talep Seçenekleri" (`/VehicleRequest/Options`) modülüne doğrudan bağlantı butonu yer alır.
  - **Sağ Panel (Seçenek Yönetimi):** Seçili kategorinin seçenek tablosu, toplam/aktif sayaçları ve "Yeni Seçenek Ekle" butonu yer alır.
- **26.3 Seçenekler Tablosu ve Kilit Mekanizması:**
  - **Sıra No:** Küçükten büyüğe sıralama (`DisplayOrder`).
  - **Seçenek Adı:** Orijinal seçenek ismi. Sistem varsayılanı ise yanında mavi `Sistem Varsayılanı` rozeti gösterilir.
  - **Kullanım Bilgisi:** "N kayıtta kullanılıyor" rozeti (ilgili veritabanı tablolarındaki gerçek zamanlı kullanım sayısı).
  - **Durum:** Aktif (yeşil rozet) veya Pasif (gri rozet).
  - **İşlemler:**
    - **Adı Düzenle:** Yalnızca kullanımda olmayan (`UsageCount == 0`) ve sistem varsayılanı olmayan (`IsSystemDefault == false`) seçeneklerde tıklanabilir. Kullanımda veya sistem varsayılanı ise buton devre dışı kalır ve kilit simgesiyle açıklayıcı tooltip gösterilir.
    - **Pasife Al / Aktifleştir:** Toggle butonu. Bir kategorideki son aktif seçenekte buton kilitlenir (`disabled`), son aktif seçeneğin pasife alınması engellenir.
    - **Seçeneği Sil:** Yalnızca kullanımda olmayan (`UsageCount == 0`) ve sistem varsayılanı olmayan (`IsSystemDefault == false`) seçeneklerde aktif kırmızı sil butonu gösterilir. Tıklandığında onay sorulur ve yumuşak silme yapılır. Son aktif seçenek silinemez.
- **26.4 Modal Mimarisi (Backdrop Donma Önlemi):**
  - `#createOptionModal` ve `#editOptionNameModal` HTML yapıları herhangi bir kart, panel veya container içinde değil, sayfanın en altında `</body>` öncesinde bağımsız olarak tanımlanmıştır. Bu sayede iç içe modal backdrop donması kesin olarak engellenmiştir.
- **26.5 Form Dropdown Entegrasyonu ve Eski Değer Koruması:**
  - Araç oluşturma/düzenleme (`FuelType`, `VehicleType`), araç belgeleri lastik sekmesi (`Season`), bakım oluşturma/düzenleme (`MaintenanceType`) ve şoför oluşturma/düzenleme (`LicenseClass`) ekranlarında seçenekler dinamik olarak yüklenir.
  - Düzenleme ekranlarında, kaydın mevcut eski değeri pasife alınmış olsa dahi listede `(Pasif)` etiketiyle seçili olarak gösterilir ve form kaydedildiğinde verinin kaybolması engellenir.

---

## 27. FAZ 10 ARAYÜZ VE STANDARTLAŞTIRMA GÜNCELLEMELERİ

### 27.1 PDF Önizleme Yükleme Katmanı Standardı (`_PdfPreviewModal.cshtml`)
- Ortak PDF önizleme modalındaki loading spinner katmanı, jQuery `show()` / `hide()` ve `!important` CSS çakışmalarını önlemek için tek bir `.is-hidden` CSS sınıfı (`display: none !important; opacity: 0; pointer-events: none;`) ile kontrol edilir.
- Iframe yükleme olayı (`iframe.on('load')`) ile katman gizlenir.
- Bazı tarayıcı içi PDF görüntüleyicilerinde iframe `load` olayının tetiklenmeme riskine karşı **3000 ms güvenlik zaman aşımı (fallback timeout)** uygulanmıştır.
- Modal gizlendiğinde (`hidden.bs.modal`), iframe arka planda bellek tüketmemesi ve ses/veri akışını kesmesi için `src="about:blank"` yapılarak temizlenir; her yeni açılışta yükleme katmanı `.is-hidden` kaldırılarak yeniden gösterilir.
- Mobilde "Yeni Sekmede Aç" butonu harici sekmede PDF indirme/görüntüleme standardını korur.

### 27.2 Araç Belgeleri Paneli PDF Önizleme ve İndirme (`Views/Vehicle/Documents.cshtml`)
- Muayene, Trafik Sigortası, Kasko ve Lastik sekmelerinin her birinin sağ üst köşesine "PDF Önizle" (göz simgeli) ve "İndir" butonları eklenmiştir.
- Bu butonlar seçili aracın Id'sini (`vehicleId`) otomatik algılayarak ilgili rapor endpoint'ini ön filtreli çağırır. Araç seçilmemişse tüm filoyu kapsar.
- Ortak `_PdfPreviewModal` üzerinden sayfa yenilenmeden önizlenir.

### 27.3 Birleşik Araç Belge Raporu Ekranı (`Views/Report/InsuranceInspection.cshtml`)
- Belge Kapsamı filtresine `Kasko` seçeneği eklenmiştir (Tümü, Muayene, Trafik Sigortası, Kasko).
- Sayfaya Araç Dropdown filtresi eklenmiştir (Tüm Araçlar veya tek araç).
- Üst kısma Araç Lastik Raporu'na doğrudan geçiş sağlayan sekme/link eklenmiştir.

### 27.4 Genel Sekreter Salt Okunur Görünürlük Kalkanı (`_AdminLayout.cshtml`)
- `User.IsInRole("GenelSekreter")` durumunda `<body>` etiketine `.role-genelsekreter` sınıfı atanır.
- Merkezi CSS kuralı ile tüm modüllerdeki (Araç, Araç Belgeleri, Şoför, Görev, Bakım, Arıza, Talep, Vekâlet, Talep Seçenekleri, Sistem Tanımları, Kullanıcı Yönetimi) veri değiştirmeye yarayan butonlar, dropdown aksiyonları, modal tetikleyicileri ve düzenle/sil ikonları gizlenir.
- Arama, filtre, sayfalama, Önizle, İndir, detaya girme, takvim ve raporlama öğeleri açık bırakılır.
- Sunucu tarafındaki `ReadOnlyRoleFilter` kalkanı ile çift katmanlı güvenlik sağlanır.

### 27.5 Kullanıcı Yönetimi Silme ve Dondurma Buton Standardı (`Views/Sef/Index.cshtml` & `Edit.cshtml`)
- Index listesinde hem aktif hem de dondurulmuş (pasif) hesaplarda kırmızı "Sil" butonu görünür. Aktif hesaplarda hem "Dondur" hem de "Sil" butonları ayrı ayrı yer alır.
- Düzenle sayfasında "Hesabı Sil" butonu ana formdan bağımsız, harici bir POST formu ile çalışır.
- Onay modalları HTML akışında kartların dışında ve `</body>` etiketinden hemen önce tanımlanarak backdrop kilitlenmesi önlenmiştir.


