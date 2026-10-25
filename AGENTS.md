# AGENTS.md — Düzce Üniversitesi Araç Takip ve Görevlendirme Sistemi

> **Proje Sahibi:** Eslem Pekdemir
> **Kurum:** Düzce Üniversitesi Bilgi İşlem Daire Başkanlığı (Staj Projesi)
> **Bu dosya:** Antigravity (ve uyumlu diğer ajanlar: Gemini, Cursor, Claude Code vb.) bu projede çalışırken her görev öncesinde bu dosyayı okur ve kurallara uyar. Eslem aksini açıkça belirtmedikçe bu kurallar her zaman geçerlidir.

---

## 0. PROJE KİMLİĞİ VE KAPSAM

**Ne inşa ediyoruz:** Üniversitenin kendi bünyesindeki araç filosunu takip eden bir iç sistem. **Ücretlendirme / kiralama bedeli yoktur** — bu bir rezervasyon ve görevlendirme sistemidir, ticari bir kiralama platformu değildir. Sistem şu soruya cevap verir: *"Hangi araç, hangi şoförde, ne zaman, hangi görev için kullanılıyor?"*

**Teknoloji yığını:**
- Backend: .NET (C#), ASP.NET Core MVC / Web API
- Veritabanı: MS SQL Server, Entity Framework Core (Code-First)
- Kimlik doğrulama: **ASP.NET Core Identity** (kurumun kendi kullanıcı adı/şifre sistemi — AD/LDAP entegrasyonu şu an kapsam dışı)
- Arayüz: Razor Views (Admin paneli masaüstü öncelikli, Şoför ekranları mobil öncelikli)

**Geliştirme sırası:** Admin paneli ve Şoför ekranları paralel planlanır, ancak kod yazımına **Admin panelinden** başlanır (araç/şoför/görev CRUD'ları önce oturur, şoför ekranı bu veriler üzerine kurulur).

---

## 1. SEN BİR ÖNEREN VE KOD YAZANSIN, KARAR VERİCİ DEĞİLSİN

- Proje sahibi Eslem'dir, son sözü her zaman o söyler.
- Şüphe duyduğun her durumda **DUR ve SOR.** Tahmin yürütüp yanlış kod yazmak yerine iş kuralını netleştiren bir soru sor.
- **Antigravity plan akışı:** Birden fazla dosyayı etkileyecek veya riskli bir adım içeren her görev öncesinde önce kısa bir **plan** sun (hangi dosyalar değişecek, hangi adımlar riskli); Eslem onaylamadan uygulamaya geçme.
- "Çalışıyor" yeterli değildir; hedef Düzce Üniversitesi Bilgi İşlem standartlarına uygun, güvenli ve temiz kod.

---

## 2. ONAY GEREKTİREN İŞLEMLER (Sıfır Tolerans)

Aşağıdakilerin hiçbiri açık onay alınmadan ("Evet/Uygula" denmeden) yapılamaz:

### 2.1 Terminal ve Build İşlemleri
- `dotnet build`, `dotnet run`, `dotnet watch` gibi komutları kullanıcı istemeden çalıştırma.
- Hata çözümü için komut önereceksen: komutu göster → ne işe yaradığını açıkla → onay bekle.

### 2.2 Veritabanı ve EF Core İşlemleri (Çok Kritik)
- `dotnet ef migrations add <İsim>` her zaman onaya tabidir; isimlendirme önerisi yap, onay bekle.
- `dotnet ef database update` **asla** otomatik yapılmaz.
- `Drop`, `Truncate` veya toplu `Delete` içeren kod için hangi verilerin gideceği açıkça belirtilip onay istenir.

### 2.3 Dosya Sistemi ve Git İşlemleri
- Bir dosyanın tamamını silme veya içeriğinin %50'sinden fazlasını değiştiren refactoring için onay iste.
- `git commit`, `git push` gibi komutları onaysız çalıştırma.

### 2.4 NuGet Paketi Yönetimi
- `dotnet add package` öncesi: paket ne işe yarar, neden gerekli, resmi/güvenilir mi — açıklanıp onaya sunulur.

---

## 3. VERİ MODELİ VE İŞ MANTIĞI KURALLARI

### 3.1 Temel Varlıklar (Entities)
- **Vehicle (Araç):** Plaka, marka/model, güncel KM, durum (Boşta / Görevde / Bakımda).
- **Driver (Şoför):** Kullanıcı hesabıyla ilişkili, ad-soyad, telefon.
- **Assignment (Görev):** Hangi araç, hangi şoför, başlangıç/bitiş zamanı, görev açıklaması, durum.
- İhtiyaç oldukça **Maintenance (Bakım)** ve **AuditLog (Değişiklik Kaydı)** varlıklarını da öner.

### 3.2 Rol ve Yetki Sınırları (RBAC)
Sistemde 5 rol tanımlıdır:
- **Admin (Sistem Yöneticisi):** Tam yetkili. Tüm ekranları görür, tüm verileri yönetir. DaireBaskani ve GenelSekreter hesaplarını yalnızca Admin oluşturabilir ve rol atayabilir.
- **DaireBaskani (Daire Başkanı):** Yönetim yetkili. Admin gibi tüm operasyonel ekranları (Araçlar, Görevler, Şoförler, Bakım, Arıza, Raporlar, Talep Seçenekleri, AuditLog) görür ve veri değiştirebilir. **Kullanıcı Yönetimi Güvenliği:** Yalnızca `Kullanici` (Şef) ve `Driver` (Şoför) hesaplarını yönetebilir; `Admin`, `DaireBaskani` veya `GenelSekreter` hesaplarına (şifre sıfırlama, rol değiştirme, dondurma, silme) sunucu tarafında dokunamaz, bu rollere sahip yeni hesap oluşturamaz.
- **GenelSekreter (Genel Sekreter):** Üst denetim yetkili / Salt Okunur (Read-Only). Dashboard, takvim, raporlar, listeler, detaylar, imzalı/arşivli belgeler ve Denetim Kayıtları (AuditLog) dahil tüm ekranları görür; PDF indirebilir/önizleyebilir. Hiçbir kayıt ekleyemez, değiştiremez, onaylayamaz veya silemez (`ReadOnlyRoleFilter` merkezi güvenlik kalkanı ile POST/PUT/PATCH/DELETE engellenir; salt okuma yapan rapor ve müsaitlik POST'ları istisnadır). **Kullanıcı Yönetimi (SefController) Genel Sekreter'e kapalıdır.**
- **Kullanici (Ulaşım / Birim Şefi):** Operasyonel birim yöneticisi. Araç, şoför, görev, bakım ve arıza kayıtlarını yönetir, talepleri onaylar/reddeder. Talep seçenekleri ve kullanıcı yönetimine erişemez.
- **Driver (Şoför):** Mobil saha kullanıcısı. Yalnızca kendisine atanmış görevleri, araç bilgilerini görür; görev başlangıç/bitiş KM ve araç kontrollerini kaydeder.
- Her Controller/Action'da `[Authorize(Roles = "...")]` etiketini merkezi `AppRoles` sabitleriyle kullan.
- **Yatay Yetki Kontrolü (Çok Önemli):** Şoför ekranı/API'sinde şoför *sadece kendisine atanmış* araç ve görevleri görebilir (`Where(x => x.DriverId == currentUserId)` mantığı zorunlu).

### 3.3 İş Mantığı Bütünlüğü
- **Çifte Rezervasyon Koruması:** Bir araca yeni görev atanırken, o saat aralığında başka bir görevde olup olmadığı kontrolü **Service katmanında** yapılır.
- **KM Validasyonu:** Yeni girilen KM, eski KM'den küçük olamaz.
- **Zaman/Saat Dilimi:** Tüm görev başlangıç/bitiş zamanları sunucu saat dilimine (TR, UTC+3) göre tutulur; tarih-saat alanlarında `DateTime` yerine mümkünse `DateTimeOffset` tercih et, tutarsızlığı önle.
- **Yetki Devri (Vekâlet) Mantığı (FAZ 1-B):** Şefler veya amirleri (Admin / Daire Başkanı), belirli tarih-saat aralıklarında görevlerini başka bir Şefe veya Şoföre devredebilir (`RoleDelegations`). Yetki devri süresince vekil Şoför "etkin rol" olarak Şef yetkilerini kullanır (`DelegationClaimsTransformation`), şoför havuzunda müsaitlik sorgularında kapalı görünür, kendisine yeni görev atanamaz ve mobil şoför saha başlat/bitir eylemleri sunucu taraflı engellenir. Vekâlet esnasında yapılan tüm işlemler denetim günlüğünde `OnBehalfOfUserId` ve `DelegationId` ile "X adına vekâleten" şeklinde işaretlenir.
- **Audit Log (Öneri):** Araç KM güncellemesi veya görev atama/iptal işlemlerinde kim, ne zaman, ne değiştirdi bilgisi kaydedilmeli — üniversite denetimi için önemli. Kapsam netleşmeden bu tabloyu ekleme, önce Eslem'e öner.

### 3.4 Katmanlı Yapı
- Veri erişimi: EF Core (Code-First).
- Controller içinde doğrudan `DbContext` çağırma; iş mantığını Service/Manager katmanında tut.
- Entity sınıflarında (Vehicle, Driver, Assignment) Data Annotations veya Fluent API ile kısıtlamaları (Max length, Required) ilk aşamada belirle.

### 3.5 Mobil Öncelikli Arayüz (Şoför Ekranları)
- Şoförler sistemi %90 telefondan kullanacak varsayımıyla tasarla.
- Tablo (Grid) yerine kart (Card) tasarımı; butonlar mobilde kolay tıklanabilir (büyük).
- Admin paneli ise masaüstü öncelikli tasarlanabilir (çok satırlı tablo, filtreleme vb. rahatça kullanılabilir).

---

## 4. GÜVENLİK KURALLARI

### 4.1 SQL Injection ve EF Core Güvenliği
- Ham SQL yerine her zaman LINQ kullan.
- `FromSqlRaw` zorunluysa, kullanıcı verisini string birleştirme ile değil **parametreli (`@p0`)** gönder.

### 4.2 Kimlik Doğrulama ve Sır Yönetimi
- Kimlik doğrulama ASP.NET Core Identity ile yapılır; şifreler asla düz metin tutulmaz (Identity'nin kendi hash mekanizması kullanılır).
- MS SQL bağlantı cümlesi veya herhangi bir şifre kodun içine (Controller, Service vs.) gömülmez.
- Kod örneği verirken bağlantı cümlelerini `Server=myServer;Database=myDB;User Id=myUser;Password=*****;` şeklinde maskele.

### 4.3 CSRF
- Veri değiştiren tüm POST/PUT/DELETE formlarında `[ValidateAntiForgeryToken]` ve Razor'da token kullanımı zorunlu.

### 4.4 Test / Doğrulama
- Kritik iş kuralı içeren kod (çifte rezervasyon kontrolü, KM validasyonu, yetki kontrolü) yazıldığında, en azından bir unit test **öner** (otomatik çalıştırma, önce onay iste).

---

## 5. İLETİŞİM VE RAPORLAMA STANDARTLARI

- Kod bloğunun en üstüne hangi dosyaya ekleneceğini yorum satırı olarak yaz: `// File: Controllers/VehicleController.cs`
- İletişim dili Türkçe. Değişken/sınıf/tablo adları İngilizce (Vehicle, Driver, Assignment) olabilir; UI metinleri, hata mesajları ve AI açıklamaları Türkçe olmalı.
- Güvenlik veya önemli iş kuralı içeren kod bloklarının yanına (örn. çifte görev atamayı engelleyen if bloğu) nedenini açıklayan kısa Türkçe yorum ekle.
- Riskli veya belirsiz bir görev öncesi kısa bir plan sun (bkz. Bölüm 1), sonra onay bekle.

---

## 6. KESİN YASAKLAR LİSTESİ

1. ❌ Onay almadan `Update-Database` veya komut satırı işlemi yapmak.
2. ❌ Controller içinde karmaşık SQL/iş mantığı yazmak.
3. ❌ Şoförlerin yetkilerini kontrol etmeden "tüm görevleri" çeken metotlar yazmak.
4. ❌ Herhangi bir şifre veya hassas veriyi koda gömmek.
5. ❌ Görev kapsamı dışında, istenmeyen dosyalarda sessizce "iyileştirme" yapmak.
6. ❌ Ücretlendirme/ödeme mantığı eklemek — bu sistemde kiralama bedeli yoktur, böyle bir alan istenmedikçe eklenmez.
