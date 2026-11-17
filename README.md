# BT Operasyon Platformu / IT Operations Platform

🇬🇧 [English](#english) · 🇹🇷 [Türkçe](#türkçe)

---

<a id="english"></a>
## English

A small ITSM / IT operations tool: computer inventory, network monitoring, ticketing, an Active Directory
management UI, software updates, real-machine monitoring, event logs and remote commands.

**Stack:** C# / .NET 8, Blazor Server, EF Core 8 + SQLite, xUnit.

### Simulated vs. real

| Part | Status |
| --- | --- |
| 10-computer inventory, installed software, tickets | Simulated (seed data) |
| Switches, ports, computer-to-port mapping (MAC hash) | Simulated |
| Active Directory (unlock, password reset, groups) | Simulated (`IDirectoryService`, ready for real LDAP) |
| Software update agent | Simulated (installs nothing) |
| The machine running the server: inventory, live metrics, winget findings, event log | **Real**, read-only |
| Remote commands on the real machine | **Real**, disabled by default, read-only commands only |

### Architecture

Dependency direction: `Core` ← `Infrastructure` ← `Web`.

- `BtPlatform.Core`: entities, rules, interfaces, security classes
- `BtPlatform.Infrastructure`: database, seeders, service implementations, embedded PowerShell scripts
- `BtPlatform.Web`: Blazor pages, background services, login and authorization
- `tests/BtPlatform.Tests`: unit tests (Core only)

### Setup

Requirements: .NET 8 SDK, Windows (PowerShell and winget for the real-machine features).

```
dotnet user-secrets set "Security:HmacKey" "<long-random-key>" --project src/BtPlatform.Web
dotnet user-secrets set "Seed:AdminPassword" "<password>" --project src/BtPlatform.Web
dotnet user-secrets set "Seed:TechnicianPassword" "<password>" --project src/BtPlatform.Web
dotnet user-secrets set "Seed:ViewerPassword" "<password>" --project src/BtPlatform.Web
dotnet run --project src/BtPlatform.Web
```

The database (`bt.db`) is created automatically on first run via migrations and seeders.

Tests:

```
dotnet build
dotnet test
```

### Roles

| Role | Permissions |
| --- | --- |
| Admin | Everything: software updates, remote commands, audit log |
| Technician | Inventory, alerts, switches, AD actions; no updates or remote commands |
| Viewer | Open tickets and limited switch info; sensitive fields masked |

Authorization is two-layered: hiding in the UI and a server-side `AccessGuard` check.

### Security notes

**Initial login passwords.** There are no passwords in the code. On the first run, if the database is empty,
random passwords are generated for `admin`, `teknisyen` and `izleyici` and shown **once** in the terminal.
To set fixed passwords, run the `Seed:*Password` commands above **before the first run**. Only the hash is
stored in the database, so a lost password cannot be recovered (delete `bt.db` and start again).

**Remote commands on the real machine (default: OFF).** On simulated computers a virtual agent produces fake
output and nothing is executed. On the real machine running the server, command execution is disabled by
default and must be enabled deliberately:

```
dotnet user-secrets set "RemoteCommands:RealExecutionEnabled" "true" --project src/BtPlatform.Web
```

Even when enabled: Admin only, a reason is required, a confirmation step is shown, only read-only commands
are allowed (allow-list plus PowerShell AST check) and every command is written to the audit log.
To disable, set the value to `false` or remove it.

Other measures:

- Passwords: PBKDF2-SHA256, 600,000 iterations, salt, constant-time comparison.
- IP, MAC and OS name are stored as HMAC-SHA256; raw values are not kept.
- Installer integrity is verified with SHA-256.
- Login: HttpOnly cookie, antiforgery, 5-minute lockout after 5 failed attempts, open-redirect protection.
- Update commands are generated from templates; client selections are never trusted.
- The audit log is append-only; raw IP/MAC and temporary passwords are never written.

### Known limitations

- AD and switch data are simulated; there is no real LDAP/SNMP connection.
- A role change does not reach an already open Blazor Server session immediately.
- The cookie secure policy is `SameAsRequest` locally; it should be `Always` in production.

---

<a id="türkçe"></a>
## Türkçe

Küçük bir ITSM / BT operasyon aracı: bilgisayar envanteri, ağ izleme, ticket sistemi, Active Directory
yönetim arayüzü, yazılım güncelleme, gerçek makine izleme, olay günlükleri ve uzaktan komut.

**Teknoloji:** C# / .NET 8, Blazor Server, EF Core 8 + SQLite, xUnit.

### Simülasyon ve gerçek: açık tablo

| Parça | Durum |
| --- | --- |
| 10 bilgisayarlık envanter, yazılımlar, ticket'lar | Simüle (seed verisi) |
| Switch ve portlar, bilgisayar-port eşleştirmesi (MAC özeti) | Simüle |
| Active Directory (kilit açma, şifre sıfırlama, gruplar) | Simüle (`IDirectoryService`, gerçek LDAP'a hazır) |
| Yazılım güncelleme ajanı | Simüle (gerçekte hiçbir şey kurmaz) |
| Sunucunun çalıştığı makine: envanter, canlı ölçüm, winget bulguları, olay günlüğü | **Gerçek**, salt okunur |
| Gerçek makinede uzaktan komut | **Gerçek**, varsayılan kapalı, yalnızca okuma komutları |

### Mimari

Bağımlılık yönü: `Core` ← `Infrastructure` ← `Web`.

- `BtPlatform.Core`: varlıklar, kurallar, arayüzler, güvenlik sınıfları
- `BtPlatform.Infrastructure`: veritabanı, seeder'lar, servis uygulamaları, gömülü PowerShell betikleri
- `BtPlatform.Web`: Blazor sayfaları, arka plan servisleri, giriş ve yetki
- `tests/BtPlatform.Tests`: birim testleri (yalnızca Core'a bağlı)

### Kurulum

Gereksinimler: .NET 8 SDK, Windows (gerçek makine özellikleri için PowerShell ve winget).

```
dotnet user-secrets set "Security:HmacKey" "<uzun-rastgele-anahtar>" --project src/BtPlatform.Web
dotnet user-secrets set "Seed:AdminPassword" "<sifre>" --project src/BtPlatform.Web
dotnet user-secrets set "Seed:TechnicianPassword" "<sifre>" --project src/BtPlatform.Web
dotnet user-secrets set "Seed:ViewerPassword" "<sifre>" --project src/BtPlatform.Web
dotnet run --project src/BtPlatform.Web
```

Veritabanı (`bt.db`) ilk çalıştırmada migration'lar ve seeder'larla kendiliğinden oluşur.

Testler:

```
dotnet build
dotnet test
```

### Roller

| Rol | Yetki |
| --- | --- |
| Admin | Her şey: yazılım güncelleme, uzaktan komut, denetim kaydı |
| Technician | Envanter, alarmlar, switch, AD işlemleri; güncelleme ve uzaktan komut yok |
| Viewer | Yalnızca ticket açma ve sınırlı switch bilgisi; hassas alanlar maskeli |

Yetki iki katmanlıdır: arayüzde gizleme ve sunucuda `AccessGuard` kontrolü.

### Güvenlik notları

**İlk giriş şifreleri.** Kodda parola yoktur. İlk çalıştırmada veritabanı boşsa `admin`, `teknisyen` ve
`izleyici` için rastgele şifreler üretilir ve terminalde **bir kez** gösterilir. Sabit şifre vermek için
yukarıdaki `Seed:*Password` komutlarını **ilk çalıştırmadan önce** çalıştırın. Veritabanında yalnızca hash
saklanır, kaybolan şifre geri getirilemez (`bt.db` silinip yeniden başlatılır).

**Gerçek makinede uzaktan komut (varsayılan: KAPALI).** Simüle bilgisayarlarda sanal ajan sahte çıktı üretir,
gerçekte hiçbir şey çalışmaz. Sunucunun çalıştığı gerçek makinede komut çalıştırma varsayılan olarak
kapalıdır, bilinçli olarak açılmalıdır:

```
dotnet user-secrets set "RemoteCommands:RealExecutionEnabled" "true" --project src/BtPlatform.Web
```

Açıkken bile: yalnızca Admin, gerekçe zorunlu, onay ekranı, yalnızca okuma komutları (izin listesi ve
PowerShell ayrıştırıcısı kontrolü), her komut denetim kaydına yazılır. Kapatmak için ayarı `false`
yapın ya da silin.

Diğer önlemler:

- Parolalar: PBKDF2-SHA256, 600.000 tekrar, tuz, sabit zamanlı karşılaştırma.
- IP, MAC ve işletim sistemi adı HMAC-SHA256 ile saklanır; ham değer tutulmaz.
- Yükleyici bütünlüğü SHA-256 ile doğrulanır.
- Giriş: HttpOnly çerez, antiforgery, 5 hatalı denemede 5 dakika kilit, açık yönlendirme koruması.
- Güncelleme komutları şablondan üretilir; istemci seçimine güvenilmez.
- Denetim kaydı yalnızca eklenir; ham IP/MAC ve geçici parola yazılmaz.

### Bilinen sınırlar

- AD ve switch verisi simülasyondur; gerçek LDAP/SNMP bağlantısı yoktur.
- Rol değişikliği açık bir Blazor Server oturumuna hemen yansımaz.
- Çerez güvenli politikası yerelde `SameAsRequest`; canlıda `Always` olmalıdır.
