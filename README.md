# 🚗 Düzce Üniversitesi Araç Takip Paneli

Üniversitenin resmi araç filosunu, şoför kadrosunu, görevlendirme süreçlerini, bakım ve arıza kayıtlarını, araç belgelerini ve personel araç taleplerini tek bir panelden yöneten, **ASP.NET Core MVC** ile geliştirilmiş kurumsal bir iç yönetim sistemidir.

> Bu sistem ticari bir kiralama platformu değildir. Üniversite içi resmi taşıt görevlendirme ve filo takibi için geliştirilmiştir.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927)
![Lisans](https://img.shields.io/badge/Kullan%C4%B1m-Kurum%20i%C3%A7i-lightgrey)

## İçindekiler

1. [Özellikler](#özellikler)
2. [Kullanıcı Rolleri](#kullanıcı-rolleri)
3. [Teknolojiler](#teknolojiler)
4. [Kurulum](#kurulum)
5. [İlk Giriş](#i̇lk-giriş)
6. [E-posta Ayarı](#e-posta-ayarı-isteğe-bağlı)
7. [Proje Yapısı](#proje-yapısı)
8. [Güvenlik](#güvenlik)
9. [Sık Karşılaşılan Sorunlar](#sık-karşılaşılan-sorunlar)
10. [Dokümantasyon](#dokümantasyon)

---

## Özellikler

### 🚘 Araç Yönetimi
- Plaka, marka, model, tür, yakıt, kilometre ve durum takibi (Boşta, Görevde, Bakımda, Belgesi Geçersiz, Hizmet Dışı)
- Plaka, marka ve model ile arama; durum ve tür filtreleri; sayfalı liste
- Araç detayında künye, güncel lastik, son görevler ve bakım geçmişi
- Aktif görevi veya açık bakımı olan araçların silinmesine karşı koruma

### 📄 Araç Belgeleri ve Lastik Takibi
- **Fenni Muayene, Trafik Sigortası, Kasko ve Lastik** için tek ekranda 4 sekmeli yönetim ve tüm geçmiş kayıtlar
- Süresi dolan muayene/sigortası olan araçların otomatik olarak "Belgesi Geçersiz" durumuna alınması (arka plan servisi, günde bir kez çalışır)
- Lastik envanteri: marka, ebat, DOT yılı, mevsim, takılma KM'si, değişim hedefleri; kullanılan KM otomatik hesaplanır
- Süresi yaklaşan belgeler ve lastikler için bildirim merkezi uyarıları

### 👨‍✈️ Şoför Yönetimi
- Şoför hesabı, profil ve birden fazla ehliyet sınıfı tek işlemde oluşturulur
- Ehliyet geçerlilik uyarıları (30 gün kala), aktif/pasif yapma, yönetici tarafından şifre sıfırlama
- Kurum dışı (taşeron) şoför desteği

### 📋 Görevlendirme
- 3 görev modu: **kurum şoförlü** (asil + yedek şoförler), **kurum dışı şoförlü**, **şoförsüz**
- Anlık müsaitlik kontrolü: araç/şoför çakışması, bakım, belge geçerliliği, ehliyet süresi
- 3 kademeli onay akışı: şoför başlangıç/bitişi bildirir, şef onaylar
- KM doğrulama eşikleri, görev ücreti, konaklama ve harcırah hesabı, süre uzatma, güzergah değişikliği onayı
- **Resmi Taşıt Görev Emri PDF** (dolu, boş veya şoföre özel)
- **Çoklu imzalı belge yükleme:** göreve bağlı sınırsız sayıda ıslak imzalı belge, şoföre bağlı, önizleme/indirme/silme ve merkezi evrak arşivi

### 📱 Şoför Mobil Portalı
- Telefon için tasarlanmış, tablosuz ve dokunmatik dostu ekranlar
- Görev başlatma/bitirme, KM ve araç kontrol bildirimi, güzergah değişikliği, arıza bildirimi
- Şoför yalnızca kendisine atanan görevleri görür

### 📝 Personel Araç Talebi
- Giriş yapmadan doldurulabilen kurumsal talep formu (`@duzce.edu.tr` e-posta doğrulaması)
- Şef için talep havuzu, tek tıkla talebi göreve dönüştürme
- Onay/ret durumunda talep sahibine kurumsal HTML e-posta bildirimi

### 🔧 Bakım ve Arıza
- Bakım kayıtları, maliyet takibi, araç durumunun otomatik güncellenmesi
- Şoför arıza bildirimleri, şef inceleme ve onay akışı, resmi arıza formu PDF

### 📊 Raporlar
- 5 sekmeli rapor merkezi: Sefer ve Kullanım, Bakım ve Tamir, Belge Durumu, Lastik, Araç Talepleri
- Hepsi için resmi A4 PDF çıktısı ve sayfa içinde önizleme

### ⚙️ Yönetim
- **Kullanıcı yönetimi:** şef, daire başkanı ve genel sekreter hesapları, hiyerarşik yetki kuralları
- **Yetki devri (vekâlet):** süreli devir, otomatik sonlanma, resmi vekâlet belgesi PDF
- **Sistem tanımları:** yakıt tipi, araç türü, lastik mevsimi, bakım türü ve ehliyet sınıfı listelerinin merkezi yönetimi
- **Denetim günlüğü (Audit Log):** kritik tüm işlemlerin eski/yeni değerleriyle kaydı, vekâleten yapılan işlemlerin izlenmesi
- **Dashboard:** filo özet kartları ve grafik, hızlı plaka arama, aylık operasyon takvimi, birleşik bildirim merkezi (ses ve toast uyarısı)

---

## Kullanıcı Rolleri

| Rol | Açıklama | Yetki |
|---|---|---|
| `Admin` | Sistem Yöneticisi | Tüm modüllerde ve üst düzey hesap yönetiminde tam yetki |
| `DaireBaskani` | Daire Başkanı | Operasyonel ekranlarda tam yetki; kullanıcı yönetiminde yalnızca şef ve şoför hesapları |
| `GenelSekreter` | Genel Sekreter | Tüm ekranları ve raporları **salt okunur** inceleme |
| `Kullanici` | Ulaşım / Birim Şefi | Araç, şoför, görev, bakım, arıza ve talep yönetimi |
| `Driver` | Şoför | Mobil portalda yalnızca kendisine atanan görevler |

Sisteme dışarıdan kayıt olma kapalıdır. Hesapları yalnızca yetkili yöneticiler açar.

---

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Platform | .NET 9, ASP.NET Core MVC (C#) |
| Veritabanı | Microsoft SQL Server, Entity Framework Core (Code-First, Migrations) |
| Kimlik ve Yetki | ASP.NET Core Identity, rol tabanlı yetkilendirme, dinamik vekâlet (`IClaimsTransformation`) |
| PDF | QuestPDF (resmi belge ve rapor şablonları) |
| Arka Plan Görevi | `BackgroundService` (belge geçerlilik denetimi) |
| E-posta | SMTP (`SmtpEmailService`) veya konsol simülasyonu (`ConsoleEmailService`) |
| Güvenlik | Stateless SVG Captcha, IP bazlı hız sınırlandırma, DataProtection |
| Arayüz | Razor Views, Bootstrap, jQuery, Select2, Chart.js |

---

## Kurulum

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express sürümü yeterlidir) veya LocalDB
- [Git](https://git-scm.com/)
- EF Core komut satırı aracı (bir kere kurulur):

```bash
dotnet tool install --global dotnet-ef
```

### 1. Projeyi indirin

```bash
git clone https://github.com/EslemPekdemir/aractakipsys
cd aractakipsys
```

### 2. Veritabanı bağlantısını ayarlayın

`appsettings.json` içindeki `DefaultConnection` değeri geliştiricinin bilgisayarındaki SQL Server adını içerir. **`SUNUCU_ADINIZ` kısmını kendi sunucu adınızla değiştirin:**

```json
"DefaultConnection": "Server=SUNUCU_ADINIZ\\SQLEXPRESS;Database=aractakip_db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

LocalDB kullanıyorsanız:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=aractakip_db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

SQL Server adınızı bilmiyorsanız SQL Server Management Studio'da bağlantı ekranında "Server name" alanına bakın.

### 3. Veritabanını oluşturun

Migration'lar tüm tabloları sıfırdan kurar, ayrıca yedek dosyasına gerek yoktur:

```bash
dotnet ef database update
```

### 4. Uygulamayı çalıştırın

```bash
dotnet run
```

Terminalde yazan adresi tarayıcıda açın (örneğin `http://localhost:5293`).

İlk çalıştırmada sistem; rolleri, varsayılan yönetici ve şef hesaplarını, örnek şoför, araç, görev ve talep kayıtlarını otomatik oluşturur (`Data/DbInitializer.cs`). Bu işlem yalnızca kayıt yoksa yapılır, mevcut verinin üzerine yazmaz.

---

## İlk Giriş

Giriş ekranında kullanıcı adı, parola ve ekrandaki güvenlik kodu (captcha) istenir.

| Hesap | Kullanıcı adı |
|---|---|
| Yönetici | `admin@duzce.edu.tr` |
| Ulaşım Şefi (örnek) | `sef@duzce.edu.tr` |

- Parolalar güvenlik nedeniyle bu dosyada yer almaz. İlk kurulum hesaplarının parolası `Data/DbInitializer.cs` içinde tanımlanır veya üretilir.
- **İlk girişten sonra parolaları mutlaka değiştirin.**
- Parola kuralı: en az 10 karakter, büyük harf, küçük harf, rakam ve özel karakter.
- 5 hatalı denemeden sonra hesap 15 dakika kilitlenir.

---

## E-posta Ayarı (isteğe bağlı)

Sistem şifre sıfırlama ve talep onay/ret bildirimleri için e-posta gönderir. Güvenlik nedeniyle e-posta kullanıcı adı ve parolası depoda **boş** bırakılmıştır.

E-posta göndermek isterseniz kendi SMTP bilgilerinizi **kodu değiştirmeden**, `user-secrets` ile ekleyin (test için ücretsiz bir [Mailtrap](https://mailtrap.io) hesabı yeterlidir):

```bash
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:UserName" "SMTP_KULLANICI_ADINIZ"
dotnet user-secrets set "EmailSettings:Password" "SMTP_PAROLANIZ"
```

Tırnak içindeki büyük harfli kısımlara kendi bilgilerinizi yazın. `Host` ve `Port` ayarları `appsettings.json` içindeki `EmailSettings` bölümündedir.

E-posta ayarı yapılmazsa uygulama normal çalışır. Gönderim hatası yakalanıp kaydedilir, görevlendirme ve talep işlemleri kesintiye uğramaz.

---

## Proje Yapısı

```
aractakipsys/
├── Controllers/      MVC denetleyicileri (Account, Assignment, Vehicle, Driver, Report ...)
├── Services/         İş mantığı (Interfaces + Implementations) ve QuestPDF belge sınıfları
├── Models/           Entity, ViewModel ve enum tanımları
├── Data/             ApplicationDbContext, Fluent API yapılandırmaları, DbInitializer
├── Migrations/       EF Core veritabanı şeması geçmişi
├── Views/            Razor görünümleri (yönetim paneli ve şoför mobil portalı)
├── Filters/          Global MVC filtreleri (ör. salt okunur rol kalkanı)
├── Common/           Yardımcı sınıflar, rol sabitleri, claims dönüşümü
├── Properties/       Çalıştırma profilleri
├── wwwroot/          CSS, JS ve statik dosyalar
├── docs/             Raporlar ve dokümanlar
└── Program.cs        Uygulama başlangıç ve servis yapılandırması
```

---

## Güvenlik

- **Kimlik doğrulama:** güçlü parola politikası, 5 denemede hesap kilitleme, HttpOnly çerezler, kalıcı DataProtection anahtarları
- **Captcha:** harici servis kullanmayan, sunucuda şifreli üretilen SVG captcha; veritabanı sorgusundan önce doğrulanır
- **Hız sınırlandırma:** giriş (dakikada 5), şifre sıfırlama ve talep formu için IP bazlı sınırlar
- **Yetkilendirme:** rol tabanlı erişim, dikey yetki yükseltme koruması, şoförler için yatay yetki izolasyonu (başkasının görevine erişim engellenir)
- **Dosya yükleme:** dosya başlığı (magic byte) doğrulaması, boyut sınırı, rastgele dosya adı; belgeler web'den erişilemeyen `App_Data` klasöründe tutulur ve yalnızca yetki kontrollü işlemlerle sunulur
- **Web güvenliği:** CSRF doğrulaması, otomatik HTML encoding, açık yönlendirme koruması, parametreli sorgular (EF Core)
- **Denetim:** tüm kritik işlemler `AuditLog` tablosuna kaydedilir

**Depoda bulunmayanlar:** parolalar ve SMTP bilgileri, yüklenmiş belgeler (`App_Data/`), şifreleme anahtarları ve veritabanı yedekleri (`.bak`).

> ⚠️ Gerçek kurum verisiyle canlıya almadan önce varsayılan hesap parolalarını değiştirin, HTTPS kullanın ve profesyonel bir güvenlik testinden geçirin.

---

## Sık Karşılaşılan Sorunlar

| Sorun | Çözüm |
|---|---|
| `Cannot open database` veya bağlantı hatası | `appsettings.json` içindeki `Server=` değerini kendi SQL Server adınızla değiştirin |
| `dotnet ef` bulunamadı | `dotnet tool install --global dotnet-ef` komutunu çalıştırıp terminali yeniden açın |
| `Invalid object name '...'` | `dotnet ef database update` komutunu çalıştırmamışsınız |
| Build sırasında dosya kilitli hatası | Çalışan uygulamayı durdurun (Ctrl+C) ve tekrar deneyin |
| E-postalar gönderilmiyor | [E-posta Ayarı](#e-posta-ayarı-isteğe-bağlı) adımlarını uygulayın |
| Girişte "güvenlik kodu hatalı" | Captcha 3 dakika geçerlidir, yenileyip tekrar deneyin |
| 35 uyarıyla derleniyor | Normaldir, bunlar null değer uyarılarıdır, hata değildir |

---

## Dokümantasyon

Kök dizindeki doküman dosyalarında veri modeli, uç noktalar, arayüz ve test kontrol listesi ayrıntılı olarak anlatılmıştır:

- `VERI-MODELI.md`: veritabanı varlıkları ve ilişkileri
- `ENDPOINT-SPESIFIKASYONU.md`: uç nokta listesi
- `ARAYUZ-SPESIFIKASYONU.md`: ekran tasarımları ve akışlar
- `TEST-KONTROL-LISTESI.md`: test senaryoları
- `docs/rapor/`: geliştirme ve güvenlik çalışmaları raporu (Markdown ve HTML)

---

## Geliştirici

**Eslem Pekdemir**
