# Repo Tour: ASP.NET Core Domain Lab

Tour ini adalah jalur membaca repo dari awal sampai akhir. Ikuti urutan ini supaya hubungan antara host, controller, service, database, dan request HTTP terlihat sebagai satu alur.

> **Target akhir:** setelah selesai, Anda dapat menjelaskan satu request dari `curl` sampai PostgreSQL, lalu menghubungkannya dengan 16 bab ASP.NET Core.

## 0. Prasyarat

Gunakan satu terminal shell untuk mengikuti command agar variable seperti `BASE`, token, dan ID tetap tersedia.

Gunakan:

- .NET SDK 10
- Docker dan Docker Compose
- `curl`
- `jq` opsional, untuk membaca token/ID dari JSON

Masuk ke repo:

```bash
cd ~/workspace/aspnet-core-domain-lab
```

Jangan gunakan credential atau signing key development untuk production. Semua data domain di lab ini sintetis.

---

## 1. Orientasi: baca peta sebelum kode

Baca file berikut dalam urutan ini:

```text
README.md
CHAPTERS.md
PLAN.md
TODO.md
HANDOFF.md
Modules/README.md
```

### Apa yang dicari

1. **`README.md`** — tujuan lab, cara menjalankan API, credential sintetis, dan batasan non-production.
2. **`CHAPTERS.md`** — indeks 16 bab dan lokasi kode setiap bab.
3. **`PLAN.md`** — keputusan arsitektur: satu Web API, Controllers, satu `DomainDbContext`, schema PostgreSQL per bounded domain.
4. **`TODO.md`** — checklist implementasi dan verifikasi. Semua item saat ini sudah selesai.
5. **`HANDOFF.md`** — keputusan teknis, hasil validasi, dan dragons/risiko yang diketahui.
6. **`Modules/README.md`** — batas healthcare, industrial, logistics, dan banking.

Cari semua komentar pembelajaran dengan:

```bash
grep -RIn --include='*.cs' --include='*.md' 'Bab [0-9]' .
```

Komentar `Bab N` adalah penunjuk saat membaca source.

---

## 2. Jalankan host: Bab 1 — Application Host

Mulai PostgreSQL dan API melalui Compose:

```bash
docker compose up -d --build
```

Periksa container:

```bash
docker compose ps
```

Baca source dalam urutan berikut:

```text
compose.yaml
Dockerfile
Program.cs
appsettings.json
appsettings.Development.json
```

### Cara membaca `Program.cs`

Ikuti bagian ini dari atas ke bawah:

1. `WebApplication.CreateBuilder(args)` — membuat host dan configuration.
2. `AddControllers()` — mengaktifkan controller pipeline.
3. `AddMemoryCache()`, response compression, output cache, dan rate limiter — mendaftarkan cross-cutting services.
4. API versioning — menyiapkan `/api/v1`.
5. `AddDomainPersistence()` — mendaftarkan EF Core/PostgreSQL.
6. `AddDomainAuthentication()` — mendaftarkan JWT dan policies.
7. `AddOpenApi()` — mendaftarkan document transformer.
8. `builder.Build()` — mengubah service collection menjadi aplikasi.
9. migration dan development seed — dijalankan sesuai configuration.
10. middleware pipeline — menentukan urutan request.
11. `MapControllers()` — memetakan endpoint controller.

Kirim request pertama:

```bash
BASE=http://localhost:8080
curl --fail --silent "$BASE/openapi/v1.json" | head -c 500
printf '\n'
curl --fail --silent --output /dev/null --write-out 'Swagger HTTP %{http_code}\n' \
  "$BASE/swagger/index.html"
```

**Yang dipahami:** host tidak berisi aturan bisnis. Host menyusun dependency, pipeline, dan endpoint.

---

## 3. Baca route map: Bab 2, 3, 5, 6, dan 7

Buka file berikut:

```text
Controllers/AuthController.cs
Modules/Healthcare/HealthcareController.cs
Modules/IndustrialAutomation/IndustrialController.cs
Modules/Logistics/LogisticsController.cs
Modules/Banking/BankingController.cs
```

Perhatikan pada setiap controller:

- `[ApiController]` — model binding dan automatic controller behavior.
- `[ApiVersion(1.0)]` — controller mendukung versi 1.0.
- `[Route("api/v{version:apiVersion}/...")]` — versi berada di URL segment.
- `[HttpGet]`, `[HttpPost]` — HTTP verb merepresentasikan operasi resource.
- `[Authorize(Policy = ...)]` — endpoint terhubung ke policy.
- `Created`, `Ok`, `Accepted`, `NoContent` — status HTTP menjadi bagian contract.
- Controller memanggil service; aturan bisnis tidak ditulis di action.

### Dua pendekatan yang sengaja tidak dipakai

- **Bab 3 — Minimal API:** domain endpoint tidak memakai `MapGet`/`MapPost`; lab memilih Controllers agar action, metadata, dan workflow lebih mudah dibandingkan.
- **Bab 5 — Filters:** lab tidak mendaftarkan custom MVC filter. Concern lintas endpoint memakai middleware, `[ApiController]`, dan authorization metadata.

Lihat seluruh route yang dihasilkan:

```bash
curl --fail --silent "$BASE/openapi/v1.json" > /tmp/domainlab-openapi.json
python3 - <<'PY'
import json
spec = json.load(open('/tmp/domainlab-openapi.json'))
for path in sorted(spec['paths']):
    print(path)
PY
```

**Yang dipahami:** request masuk melalui route versioned, melewati metadata authorization/limiter/cache, lalu diteruskan ke action controller.

---

## 4. Ikuti satu request sampai database: Healthcare

Mulai dari route:

```text
POST /api/v1/healthcare/patients
```

Lalu baca berurutan:

```text
Modules/Healthcare/HealthcareController.cs
Modules/Healthcare/HealthcareModels.cs
Modules/Healthcare/HealthcareService.cs
Infrastructure/Api/DomainErrors.cs
Infrastructure/Api/DomainErrorHandlingMiddleware.cs
Infrastructure/Persistence/DomainDbContext.cs
```

### Buat token healthcare

```bash
HEALTH_TOKEN=$(curl --fail --silent --request POST \
  "$BASE/api/v1/auth/login" \
  --header 'Content-Type: application/json' \
  --data '{
    "userName": "healthcare.demo",
    "password": "HealthDemo!123"
  }' | jq -r '.accessToken')
```

Jika `jq` tidak tersedia, simpan nilai `accessToken` dari response dan set manual:

```bash
HEALTH_TOKEN='paste-access-token-here'
```

Buat patient:

```bash
PATIENT_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/healthcare/patients" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{
    "medicalRecordNumber": "MRN-TOUR-001",
    "displayName": "Synthetic Tour Patient",
    "birthDate": "1988-05-10"
  }')
printf '%s\n' "$PATIENT_JSON"
PATIENT_ID=$(printf '%s' "$PATIENT_JSON" | jq -r '.id')
```

Ambil patient:

```bash
curl --fail --silent \
  "$BASE/api/v1/healthcare/patients/$PATIENT_ID" \
  --header "Authorization: Bearer $HEALTH_TOKEN"
```

### Alur kode yang perlu diikuti

1. Controller menerima request dan memanggil `CreatePatientAsync`.
2. Request record memakai DataAnnotations untuk validasi shape.
3. Service menormalisasi medical record number.
4. EF Core memeriksa duplicate.
5. Entity ditambahkan ke `dbContext.Patients` dan disimpan.
6. Service menulis structured log.
7. Controller mengembalikan `201 Created`.
8. `DomainDbContext` memetakan entity ke `healthcare.patients`.

Coba validation error:

```bash
curl --silent --request POST \
  "$BASE/api/v1/healthcare/patients" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{}'
```

Perhatikan response `400` dengan `errorCode: validation_failed`. Coba domain rule dengan medical record number yang sama untuk melihat `409` dan error contract healthcare.

---

## 5. Baca cross-cutting pipeline: Bab 4, 8, 9, dan 10

Buka file berikut:

```text
Infrastructure/Api/RequestDiagnosticsMiddleware.cs
Infrastructure/Api/DomainErrorHandlingMiddleware.cs
Infrastructure/Api/DomainErrors.cs
Infrastructure/Caching/DomainMemoryCache.cs
```

### Bab 4 — Middleware

Urutan penting di `Program.cs`:

```text
RequestDiagnosticsMiddleware
DomainErrorHandlingMiddleware
Authentication
Authorization
RateLimiter
OutputCache
MapControllers
```

Coba correlation ID:

```bash
curl --silent --include \
  "$BASE/api/v1/healthcare/patients/$PATIENT_ID" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header 'X-Correlation-Id: tour-correlation-001'
```

Cari header response `X-Correlation-Id` dan lihat log:

```bash
docker compose logs --since 2m --no-color api
```

### Bab 8 — Validation

Baca request record di setiap file model:

```text
Modules/Healthcare/HealthcareModels.cs
Modules/IndustrialAutomation/IndustrialModels.cs
Modules/Logistics/LogisticsModels.cs
Modules/Banking/BankingModels.cs
```

`[Required]`, `[StringLength]`, dan `[Range]` memvalidasi bentuk input. Aturan seperti “appointment harus future” atau “saldo harus cukup” berada di service karena itu domain invariant.

### Bab 9 — Error Handling

Ikuti jalur exception:

```text
service Rule(...)
  -> DomainRuleException
  -> DomainErrorHandlingMiddleware
  -> DomainErrorFactory
  -> domain-specific JSON response
```

Healthcare, industrial, logistics, dan banking sengaja memiliki field error berbeda.

### Bab 10 — Logging

Baca `ILogger` pada setiap service. Perhatikan bahwa log menggunakan structured placeholders seperti `{PatientId}` dan tidak menulis raw password, access token, atau refresh-token value.

---

## 6. Baca Authentication & Authorization: Bab 11

Baca file dalam urutan ini:

```text
Infrastructure/Authentication/AuthContracts.cs
Infrastructure/Authentication/JwtOptions.cs
Infrastructure/Authentication/AuthRoles.cs
Infrastructure/Authentication/AuthenticationServiceCollectionExtensions.cs
Infrastructure/Authentication/JwtTokenService.cs
Infrastructure/Authentication/AuthenticationService.cs
Infrastructure/Authentication/DevelopmentDataSeeder.cs
Controllers/AuthController.cs
```

Login dan cek identity:

```bash
curl --fail --silent --request POST \
  "$BASE/api/v1/auth/login" \
  --header 'Content-Type: application/json' \
  --data '{
    "userName": "healthcare.demo",
    "password": "HealthDemo!123"
  }'

curl --fail --silent "$BASE/api/v1/auth/me" \
  --header "Authorization: Bearer $HEALTH_TOKEN"
```

Cek policy allow/deny:

```bash
curl --silent --write-out '\nHTTP %{http_code}\n' \
  "$BASE/api/v1/auth/probes/healthcare" \
  --header "Authorization: Bearer $HEALTH_TOKEN"

curl --silent --write-out '\nHTTP %{http_code}\n' \
  "$BASE/api/v1/auth/probes/banking-transfer" \
  --header "Authorization: Bearer $HEALTH_TOKEN"
```

Baca dan pahami:

- access JWT berisi `sub`, `name`, `display_name`, dan `role` claims;
- refresh token random hanya disimpan sebagai SHA-256 hash;
- refresh rotation menandai token lama sebagai revoked;
- reuse detection mencabut token aktif user tersebut;
- `AuthPolicies` menghubungkan role dengan domain endpoint;
- `platform.admin` menjadi bypass policy untuk kebutuhan lab.

---

## 7. Baca persistence: Bab 12 — Entity Framework Core

Buka file berikut:

```text
Infrastructure/Persistence/DatabaseSchemas.cs
Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
Infrastructure/Persistence/DomainDbContextFactory.cs
Infrastructure/Persistence/DomainDbContext.cs
Infrastructure/Persistence/DatabaseMigrationExtensions.cs
Infrastructure/Persistence/Migrations/
Infrastructure/Persistence/README.md
```

### Yang perlu dipahami dari `DomainDbContext`

- context default-schema neutral;
- setiap entity memakai `ToTable(table, schema)`;
- index unique menjaga idempotency/reference;
- relationship dan delete behavior ditulis eksplisit;
- `Version` menjadi concurrency token pada refresh token, asset, dan account;
- migration history berada di `public.__EFMigrationsHistory`.

Lihat migration:

```bash
dotnet tool run dotnet-ef migrations list
```

Saat Compose berjalan, periksa schema dan table:

```bash
docker compose exec -T postgres psql -U domainlab -d domainlab -c \
  "select table_schema, count(*)
   from information_schema.tables
   where table_schema in ('auth','healthcare','industrial','logistics','banking')
   group by table_schema
   order by table_schema;"
```

**Yang dipahami:** satu database dan satu context tidak berarti semua module kehilangan ownership. Ownership dijaga oleh schema/table mapping.

---

## 8. Tour empat bounded workflow

Setelah memahami healthcare sebagai contoh lengkap, baca setiap module dengan pola yang sama:

```text
Controller -> Models -> Service -> Domain errors/logging -> DomainDbContext mapping -> README
```

### 8.1 Industrial automation

Baca:

```text
Modules/IndustrialAutomation/IndustrialController.cs
Modules/IndustrialAutomation/IndustrialModels.cs
Modules/IndustrialAutomation/IndustrialService.cs
Modules/IndustrialAutomation/README.md
```

Fokus belajar:

- asset lifecycle dan status transition;
- telemetry membuat synthetic critical alarm pada threshold;
- telemetry read memakai memory cache tiga detik;
- maintenance work order memiliki state transition;
- command memakai idempotency key dan hanya simulasi;
- command diberi rate limit tiga request per 30 detik;
- telemetry response memakai output cache dan compression.

Buat token dan asset/telemetry sintetis:

```bash
INDUSTRIAL_TOKEN=$(curl --fail --silent --request POST \
  "$BASE/api/v1/auth/login" \
  --header 'Content-Type: application/json' \
  --data '{
    "userName": "industrial.demo",
    "password": "IndustrialDemo!123"
  }' | jq -r '.accessToken')

ASSET_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/industrial/assets" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{
    "assetCode": "TOUR-ASSET-001",
    "displayName": "Synthetic Tour Asset"
  }')
ASSET_ID=$(printf '%s' "$ASSET_JSON" | jq -r '.id')

curl --fail --silent --request POST \
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{
    "value": 42,
    "unit": "percent",
    "observedAtUtc": "2026-01-01T00:00:00Z"
  }' > /dev/null
```

### 8.2 Logistics

Baca:

```text
Modules/Logistics/LogisticsController.cs
Modules/Logistics/LogisticsModels.cs
Modules/Logistics/LogisticsService.cs
Modules/Logistics/README.md
```

Fokus belajar:

- tracking number dinormalisasi;
- shipment dan warehouse read memiliki cache TTL berbeda;
- mutation menghapus cache terkait;
- route memiliki stop sequence unique;
- carrier event menjadi integration boundary idempotent;
- dispatch mutation memakai rate limit terpisah.

Buat token logistics untuk request read/cache:

```bash
LOGISTICS_TOKEN=$(curl --fail --silent --request POST \
  "$BASE/api/v1/auth/login" \
  --header 'Content-Type: application/json' \
  --data '{
    "userName": "logistics.demo",
    "password": "LogisticsDemo!123"
  }' | jq -r '.accessToken')
```

### 8.3 Banking

Baca:

```text
Modules/Banking/BankingController.cs
Modules/Banking/BankingModels.cs
Modules/Banking/BankingService.cs
Modules/Banking/README.md
```

Fokus belajar:

- account memakai optimistic concurrency version;
- transfer mengecek currency, account state, dan sufficient balance;
- satu EF transaction menulis transfer, debit, credit, dan transaction history;
- idempotency key membuat retry aman;
- risk review adalah workflow state terpisah;
- transfer diberi rate limit;
- response banking sengaja tidak memakai compression.

Buat token banking dan satu account sintetis untuk request berikutnya:

```bash
BANKING_TOKEN=$(curl --fail --silent --request POST \
  "$BASE/api/v1/auth/login" \
  --header 'Content-Type: application/json' \
  --data '{
    "userName": "banking.demo",
    "password": "BankingDemo!123"
  }' | jq -r '.accessToken')

CUSTOMER_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/banking/customers" \
  --header "Authorization: Bearer $BANKING_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{
    "customerReference": "TOUR-CUSTOMER-001",
    "displayName": "Synthetic Tour Customer"
  }')
CUSTOMER_ID=$(printf '%s' "$CUSTOMER_JSON" | jq -r '.id')

ACCOUNT_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/banking/customers/$CUSTOMER_ID/accounts" \
  --header "Authorization: Bearer $BANKING_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{
    "accountReference": "TOUR-ACCOUNT-001",
    "currency": "SIM",
    "initialBalanceMinor": 10000
  }')
ACCOUNT_ID=$(printf '%s' "$ACCOUNT_JSON" | jq -r '.id')
```

### 8.4 Healthcare lanjutan

Kembali ke:

```text
Modules/Healthcare/HealthcareService.cs
Modules/Healthcare/HealthcareModels.cs
Modules/Healthcare/README.md
```

Baca appointment, lab result review, dan inventory setelah memahami jalur patient. Cari perbedaan antara validation shape dan domain rule:

- `DateOnly`/string length — input shape;
- appointment future-only — domain invariant;
- lab review sekali — state invariant;
- inventory quantity tidak boleh negatif — domain invariant.

---

## 9. Bab 13 — Caching

Baca:

```text
Infrastructure/Caching/DomainMemoryCache.cs
Modules/IndustrialAutomation/IndustrialService.cs
Modules/Logistics/LogisticsService.cs
Program.cs
```

Pahami dua level cache:

1. **Service/application cache** — dikontrol langsung oleh service dengan key, TTL, dan invalidation.
2. **Output cache** — dipasang sebagai endpoint policy melalui `[OutputCache]` dan `UseOutputCache()`.

Lihat cache hit/miss:

```bash
# Jalankan dua kali endpoint logistics yang sama.
curl --silent "$BASE/api/v1/logistics/shipments/SYN-DEMO-002" \
  --header "Authorization: Bearer $LOGISTICS_TOKEN" > /dev/null
curl --silent "$BASE/api/v1/logistics/shipments/SYN-DEMO-002" \
  --header "Authorization: Bearer $LOGISTICS_TOKEN" > /dev/null

docker compose logs --since 1m --no-color api | grep 'Memory cache'
```

---

## 10. Bab 14 — Response Compression

Baca:

```text
Program.cs
Infrastructure/Api/DomainResponseCompressionProvider.cs
Modules/IndustrialAutomation/IndustrialController.cs
Modules/Banking/BankingController.cs
```

Tes dengan header `Accept-Encoding`:

```bash
curl --silent --dump-header - --output /dev/null \
  --header 'Accept-Encoding: gzip' \
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" | grep -i 'content-encoding'

curl --silent --dump-header - --output /dev/null \
  --header 'Accept-Encoding: gzip' \
  "$BASE/api/v1/banking/accounts/$ACCOUNT_ID" \
  --header "Authorization: Bearer $BANKING_TOKEN" | grep -i 'content-encoding' || true
```

Industrial dapat mengembalikan `Content-Encoding: gzip`; banking sengaja tidak memiliki header compression.

---

## 11. Bab 15 — Rate Limiting

Baca konfigurasi policy di `Program.cs`, lalu cari metadata berikut:

```text
[EnableRateLimiting("industrial-command")]
[EnableRateLimiting("logistics-dispatch")]
[EnableRateLimiting("banking-transfer")]
```

Perhatikan:

- partition key memakai authenticated username atau remote IP;
- queue limit adalah `0`, sehingga request berlebih langsung ditolak;
- rejection memakai HTTP `429` dan header `Retry-After`;
- setiap domain memiliki permit/window berbeda.

Untuk mengulang pengujian, gunakan request command atau transfer dengan payload valid sampai response berubah menjadi `429`.

---

## 12. Bab 16 — API Documentation

Baca:

```text
Infrastructure/OpenApi/BearerSecurityOpenApiTransformer.cs
Program.cs
README.md
AspNetCoreDomainLab.http
```

Buka di browser:

```text
http://localhost:8080/swagger
http://localhost:8080/openapi/v1.json
```

Di OpenAPI, pastikan ada tag:

```text
Auth
Healthcare
Industrial
Logistics
Banking
```

Perhatikan bahwa login dan refresh anonymous, sedangkan revoke, me, dan workflow domain memiliki bearer security requirement.

---

## 13. Verifikasi akhir

Jalankan pemeriksaan source:

```bash
dotnet restore
dotnet build --no-restore --force
docker compose config --quiet
dotnet tool run dotnet-ef migrations list
```

Jalankan pemeriksaan runtime:

```bash
docker compose up -d --build
curl --fail --silent "$BASE/openapi/v1.json" > /dev/null
curl --fail --silent "$BASE/swagger/index.html" > /dev/null
docker compose logs --since 2m --no-color api
```

Cari error tak terduga:

```bash
if docker compose logs --since 2m --no-color api | grep -E 'Unhandled|fail: Microsoft.AspNetCore.Diagnostics|Exception'; then
  exit 1
fi
```

Matikan stack setelah selesai:

```bash
docker compose down
```

Perintah tersebut menghapus container dan network, tetapi volume PostgreSQL tetap ada. Untuk reset database lokal secara sengaja:

```bash
docker compose down -v
```

Gunakan `down -v` hanya jika data synthetic lokal boleh dihapus.

---

## 14. Ringkasan jalur baca satu halaman

```text
README.md
  -> CHAPTERS.md
  -> PLAN.md / TODO.md / HANDOFF.md
  -> compose.yaml / Dockerfile
  -> Program.cs
  -> AuthController + domain controllers
  -> domain models
  -> domain services
  -> middleware dan error contracts
  -> authentication
  -> DomainDbContext dan migrations
  -> cache / compression / rate limiting
  -> OpenAPI / HTTP examples
  -> curl verification
```

Jika tersesat:

1. kembali ke `Program.cs` untuk melihat request pipeline;
2. pilih satu route dari OpenAPI;
3. ikuti `Controller -> Service -> DbContext`;
4. cari komentar `Bab N`;
5. baca README module untuk trade-off dan failure mode.

Setelah tour ini selesai, buka `CHAPTERS.md` sebagai indeks referensi, bukan sebagai urutan baca utama.
