# Klinik Randevu ve Hasta Yönetim Sistemi

**Bireysel Bitirme Projesi — Yazılım Mühendisliği**

- **Öğrenci:** Utku Uğurlu (231201016)
- **Bölüm:** Bilgisayar Mühendisliği
- **Öğretim Görevlisi:** Dr. Öğr. Üyesi Fatima Bhutta
- **Dönem:** 2025-2026 Bahar

## Özet
Bu proje; klinik ortamında randevu yönetimi, doktor/hasta profilleri ve tıbbi kayıt tutma süreçlerini tek bir web uygulamasında toplayan, rol tabanlı erişim denetimine sahip bir yazılım sistemidir.

## Teknoloji Yığını
- **.NET 9.0 / ASP.NET Core MVC** — sunucu tarafı çerçeve
- **Entity Framework Core 9.0 + SQLite** — ORM ve kalıcı depolama
- **ASP.NET Core Identity** — kimlik doğrulama ve rol yönetimi
- **Bootstrap 5** — duyarlı arayüz
- **xUnit + EF Core InMemory** — birim testi

## Kullanıcı Rolleri
| Rol      | Temel Yetkiler |
|----------|----------------|
| Admin    | Tüm sistem, kullanıcı/rol yönetimi, istatistik |
| Doctor   | Kendi takvimi, hasta tıbbi kayıtları, durum güncelleme |
| Patient  | Doktor arama, randevu oluşturma/iptal, kendi kayıtları |

## Hızlı Başlangıç

```bash
# 1. Bağımlılıkları geri yükle
dotnet restore

# 2. Derle
dotnet build

# 3. Testleri çalıştır
dotnet test

# 4. Çalıştır
cd src/KlinikYonetimSistemi.Web
dotnet run
```

Uygulama `https://localhost:5001` veya `http://localhost:5000` adresinde açılır. İlk çalıştırmada veritabanı oluşturulur ve demo veriler eklenir.

### Demo Hesaplar
| Rol    | E-posta               | Parola     |
|--------|-----------------------|------------|
| Admin  | admin@klinik.local    | `Admin!234` |
| Doktor | doktor@klinik.local   | `Doktor!234` |
| Hasta  | hasta@klinik.local    | `Hasta!234` |

## Proje Yapısı
```
BitirmeProjesi_KlinikYonetimSistemi/
├── KlinikYonetimSistemi.sln
├── src/KlinikYonetimSistemi.Web/       # ASP.NET Core MVC uygulaması
│   ├── Controllers/                    # Home, Appointments, Doctors, Patients, Admin
│   ├── Models/                         # Domain modelleri
│   ├── Services/                       # İş mantığı (AppointmentService)
│   ├── ViewModels/                     # Sunum katmanı modelleri
│   ├── Views/                          # Razor görünümleri
│   ├── Data/                           # DbContext, Migrations, DbInitializer
│   └── Areas/Identity/                 # Identity UI (scaffold)
├── tests/KlinikYonetimSistemi.Tests/   # xUnit birim testleri
└── docs/                               # Akademik rapor ve ek dokümantasyon
```

## Gereksinim Özeti

**Fonksiyonel:** kayıt/giriş; rol tabanlı yetkilendirme; doktor listeleme/filtreleme; hasta-doktor randevu oluşturma (çakışma kontrolü); randevu iptali; doktor durum güncelleme ve not ekleme; hasta tıbbi kayıt görüntüleme; yönetici kullanıcı yönetimi ve raporlama.

**Fonksiyonel olmayan:** güvenlik (parola hash, anti-forgery, HTTPS, rol denetimi, kilitleme); sürdürülebilirlik (katmanlı mimari, DI, migration); performans (eager loading, indeksli sorgular, 2 sn altı yanıt); kullanılabilirlik (Bootstrap duyarlı arayüz, Türkçe etiketler, net hata mesajları).

## Lisans
Yalnızca akademik amaçlı, bireysel proje.
