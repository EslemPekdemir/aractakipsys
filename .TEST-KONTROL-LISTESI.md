# TEST / KONTROL LİSTESİ — Araç Takip ve Görevlendirme Sistemi

> Bu listeyi yukarıdan aşağı, sırayla tek tek dene. Her maddenin yanına ✅ veya ❌ koy, ❌ olanları not al — birlikte düzeltiriz.

---

## 1. Kimlik Doğrulama ve Yetkilendirme

- [ ] Admin (`admin`) ile giriş → Dashboard'a düşüyor mu?
- [ ] Kullanici/Şef (`sef`) ile giriş → Dashboard'a düşüyor mu?
- [ ] Şoför (`ahmet.yilmaz`) ile giriş → `/Surucu`'ya yönleniyor mu?
- [ ] **SVG Captcha:** Login sayfasında dinamik SVG güvenlik kodu görünüyor mu?
- [ ] **SVG Captcha Yenileme:** Yenile ikonuna tıklandığında sayfa yenilenmeden yeni görsel ve token geliyor mu?
- [ ] **SVG Captcha Doğrulama:** Kod boş veya yanlış girildiğinde kırmızı uyarı verip giriş engelleniyor mu?
- [ ] Yanlış şifreyle 5 kez üst üste giriş dene → hesap 15 dakika kilitleniyor mu?
- [ ] Şoför hesabıyla `/Vehicle`, `/Driver`, `/AuditLog`, `/Sef` adreslerine elle girmeyi dene → hepsinde `AccessDenied` alıyor musun?
- [ ] Kullanici (Şef) hesabıyla `/AuditLog` adresine elle girmeyi dene → `AccessDenied` alıyor musun?
- [ ] `/Account/Register` adresine elle git → böyle bir sayfa hiç yok mu (404)?
- [ ] "Şifremi Unuttum" akışını dene → konsolda/log'da sıfırlama linki görünüyor mu, link çalışıyor mu?
- [ ] Çıkış yap, aynı sekmede farklı bir kullanıcı ile giriş yap → önceki kullanıcının bildirimleri karışmıyor mu?

## 2. Araç Yönetimi (Vehicle)

- [ ] Yeni araç ekle → tüm alanlar (Muayene/Sigorta Başlangıç-Bitiş, Lastik, Ceza Notu) formda var mı ve kaydediliyor mu?
- [ ] Aynı plakayla ikinci bir araç eklemeyi dene → engelleniyor mu?
- [ ] Bir aracı sil, sonra aynı plakayla yeni araç eklemeyi dene → engelleniyor mu?
- [ ] Edit ekranında Güncel KM ve Durum alanları gerçekten salt okunur mu?
- [ ] Muayene/Sigorta tarihini geçmişe çekip kaydet → araç durumu `Belgesi Geçersiz` (kırmızı) oluyor mu?
- [ ] Aynı aracın tarihini tekrar geleceğe çekip kaydet → durum otomatik düzeliyor mu?
- [ ] Aktif/planlı görevi olan bir aracı silmeyi dene → engelleniyor mu?
- [ ] Devam eden bakımı olan bir aracı silmeyi dene → engelleniyor mu?
- [ ] Araç listesi filtreleri (Durum, Tip, Select2 arama) çalışıyor mu?

## 3. Şoför Yönetimi (Driver)

- [ ] Yeni şoför ekle → hem giriş yapabiliyor mu hem Driver kaydı oluşmuş mu?
- [ ] Aynı ehliyet numarasıyla ikinci bir şoför eklemeyi dene → engelleniyor mu?
- [ ] Aktif görevi olan şoförü pasife almayı/silmeyi dene → engelleniyor mu?
- [ ] Şoförü pasife al → o şoför artık giriş yapamıyor mu?
- [ ] Ehliyet süresi dolmak üzere olan şoförde uyarı rozeti görünüyor mu?

## 4. Ulaşım Şefleri (Sef/Kullanici) Yönetimi

- [ ] Sadece Admin bu sayfayı görebiliyor mu?
- [ ] Yeni Şef ekle → giriş yapabiliyor mu?
- [ ] Şef'i pasife al (sil) → soft-delete oluyor mu?
- [ ] Daha önce AuditLog'a girmiş bir Şef'i pasife almayı dene → hata almadan çalışıyor mu?
- [ ] Kendi hesabını pasife almaya çalış → engelleniyor mu?

## 5. Görev Atama ve Onay Akışı (Assignment) — En Kritik Modül

- [ ] Yeni görev oluştur, araç/şoför Select2 (aramalı) mı?
- [ ] Aynı araca/şoföre çakışan saatlerde ikinci görev atamayı dene → engelleniyor mu?
- [ ] Ehliyeti görev bitişini kapsamayan şoföre görev atamayı dene → engelleniyor mu?
- [ ] Bakımdaki/Belgesi Geçersiz araca görev atamayı dene → engelleniyor mu?
- [ ] **Gece senaryosu:** Şoför "Başlangıcı Bildir" desin → Admin ekranında hâlâ `Planlandı` mı, turuncu rozet çıkıyor mu?
- [ ] Aynı şoför, Şef onaylamadan "Bitişi Bildir" yapabiliyor mu?
- [ ] "Onayla" butonu → görev `Tamamlandı` oluyor, KM güncelleniyor mu?
- [ ] Şef "Doğrudan Başlat"/"Doğrudan Bitir" yapabiliyor mu?
- [ ] Bir görevi iptal et → "Onay Bekliyor" rozeti yanlışlıkla kalmıyor mu?
- [ ] **InProgress Görev Süresi Uzatma:** Seyir halindeki (`InProgress`) bir görev detayında "Süreyi Uzat" modalı açılıyor mu?
- [ ] **Süre Uzatma Çakışma:** Yeni uzatılan saate denk gelen başka görev varsa sistem işlemi reddediyor mu?
- [ ] **Süre Uzatma Damgası:** Başarılı uzatmada gerekçe `ExpenseNotes` alanına `[Süre Uzatma - ...]` olarak işleniyor mu?
- [ ] **Ücret Terminolojisi:** Görev formlarında ve PDF'lerde "Kadrolu Personel" veya "Yevmiye" ibaresi KALMADIĞI, "Görev Ücreti" ve "Harcırah" yazdığı doğrulanıyor mu?
- [ ] Görev tamamlarken girilen `HourlyRate` ile toplam görev ücreti (`TotalDriverFee`) doğru hesaplanıyor mu?
- [ ] "Görevlendirme Belgesi (PDF)" indirilip içerik doğrulanabiliyor mu?
- [ ] İmzalı belge (PDF/JPEG) yükle/indir çalışıyor mu?
- [ ] 11MB dosya ve sahte uzantılı dosya reddediliyor mu?
- [ ] **İmzalı Evrak Arşivi:** `/Assignment/SignedDocuments` sayfasına girildiğinde tüm arşiv listeleniyor mu?
- [ ] **Arşiv Filtre ve Temizle:** Plaka, şoför ve tarih filtreleri çalışıyor, "Temizle" butonu formu başarıyla sıfırlıyor mu?

## 6. Bakım Kayıtları (Maintenance)

- [ ] Yeni bakım → araç `Bakımda` oluyor mu?
- [ ] Aynı araca ikinci açık bakım engelleniyor mu?
- [ ] Görevdeki aracı bakıma almak engelleniyor mu?
- [ ] Bakımı tamamla → araç `Boşta`'ya dönüyor mu?
- [ ] Açık bakım kaydını sil → durum doğru geri dönüyor mu?

## 7. Araç Talepleri (VehicleRequest)

- [ ] Giriş yapmadan `/AracTalebi` açılıyor mu?
- [ ] `@duzce.edu.tr` olmayan e-posta reddediliyor mu?
- [ ] Geçerli talep gönderilip "alındı" mesajı geliyor mu?
- [ ] Admin/Şef "Karşıla" ile göreve dönüştürebiliyor mu, mail simülasyonu görünüyor mu?
- [ ] Reddetme gerekçeli çalışıyor mu?

## 8. Denetim Kayıtları (AuditLog)

- [ ] Sadece Admin görebiliyor mu?
- [ ] Tüm kritik işlemler kayıtlı mı?
- [ ] "İncele" modalı Türkçe/okunur mu?
- [ ] Filtreler çalışıyor mu?

## 9. Raporlar

- [ ] Kullanım Raporu filtre + PDF çalışıyor mu?
- [ ] Bakım Raporu tarih+saat filtresi ve PDF'de saat bilgisi var mı?
- [ ] Sigorta/Muayene Raporu 3 filtre + PDF çalışıyor mu?
- [ ] Üç rapor sekmesi arasında geçiş yapılabiliyor mu?

## 10. Dashboard

- [ ] KPI kartları doğru mu?
- [ ] Filo Durum grafiğinde 5 renk (Belgesi Geçersiz dahil) var mı?
- [ ] Plaka arama doğru sonuç veriyor mu?
- [ ] Bildirim zili çalışıyor mu?
- [ ] İlk girişte toast+ses geliyor, kapatınca tekrar çıkmıyor, yeni uyarıda tekrar çıkıyor mu?
- [ ] Sidebar aktif sayfa vurgusu çalışıyor mu?

## 11. Şoför Mobil Paneli & Responsive Standartlar

- [ ] Mobil ekranda (veya tarayıcı mobil modunda) yatay kaydırma çubuğu çıkmadan tek sütun kartlar düzgün sıralanıyor mu?
- [ ] Dokunmatik butonlar ("Başlangıcı Bildir", "Bitişi Bildir") en az 48px yükseklikte ve parmakla rahat tıklanabilir mi?
- [ ] Şoför ilk kez girdiğinde `/Surucu/ChangePassword` şifre yenileme ekranı mobil uyumlu açılıyor ve şifre değiştirilebiliyor mu?
- [ ] Kart/buton tasarımı ve onay bekliyor bilgisi doğru mu?

## 12. Profil Yönetimi, Rozet Avatar ve Yükleme Güvenliği (`/Account/Profile`)

- [ ] **Bağımsız Profil Güncelleme:** Parola alanları boş bırakılarak sadece Ad-Soyad, E-posta ve Telefon güncellendiğinde form hata vermeden başarıyla kaydediliyor mu?
- [ ] **Dinamik Rozet (Baş Harf) Avatarı:** Profil fotoğrafı olmayan veya yeni oluşturulan hesaplarda sidebar'da ve Profil sayfasında kullanıcının ad-soyad baş harflerinden oluşan kurumsal rozet ("AY", "SY", "EŞ" vb.) görüntüleniyor mu?
- [ ] **Canlı Resim Önizleme:** Profil sayfasında yeni bir görsel seçildiğinde rozet gizlenip seçilen görsel önizleme alanında anında beliriyor mu?
- [ ] **Fotoğraf Yükleme:** Geçerli bir resim (.jpg, .png veya .webp) yüklendiğinde profil fotoğrafı başarıyla güncelleniyor ve sidebar'a yansıyor mu?
- [ ] **Magic Number / Dosya Başlığı Koruması:** Uzantısı `.png` yapılmış ancak içeriği metin/script olan sahte dosya yüklenmeye çalışıldığında "Dosya içeriği geçerli bir PNG resmi değil" uyarısı verilip engelleniyor mu?
- [ ] **Boyut Sınırı:** 5 MB'tan büyük bir resim yüklenmeye çalışıldığında "Profil fotoğrafı boyutu en fazla 5 MB olabilir" uyarısı alınıyor mu?
- [ ] **Koşullu Fotoğraf Kaldırma:** Fotoğrafı olan kullanıcıda "Fotoğrafı Kaldır" butonu görünüp tıklandığında diskteki dosya silinip dinamik rozet avatara geri dönüyor mu? Fotoğrafı olmayan kullanıcıda buton gizli mi?

---

Bu listeyi bitirince "şunlar ❌ çıktı" diye söyle, birlikte düzeltiriz.

