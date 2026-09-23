# Repo Tour: ASP.NET Core Domain Lab

Tour ini adalah **jalur belajar resmi** repository. Ikuti urutannya supaya host, controller, service, database, dan request HTTP terlihat sebagai satu alur. Gunakan `REVIEW-ASP.NET-CORE.md` saat konsep perlu penjelasan, `CHEATSHEET-ASP.NET-CORE.md` untuk lookup cepat, dan `CHAPTERS.md` untuk menemukan implementasi.

> **Target akhir:** setelah selesai, Anda dapat memprediksi perilaku satu request, menjalankannya, mengamati hasilnya, menjelaskan penyebabnya, dan menelusurinya dari `curl` sampai PostgreSQL.

Setiap checkpoint memakai loop berikut:

1. **Predict** — tulis status, body, header, log, atau perubahan state yang diharapkan.
2. **Run** — jalankan command yang diberikan.
3. **Observe** — bandingkan hasil aktual dengan prediksi.
4. **Explain** — tunjukkan controller, middleware, policy, service, atau mapping yang menyebabkan hasil tersebut.

Checkpoint mengukur pemahaman. Checklist implementasi di `TODO.md` hanya mencatat pekerjaan repository yang sudah dilakukan.

## 0. Prasyarat dan state per run

Gunakan satu terminal shell agar variable seperti `BASE`, token, dan ID tetap tersedia.

Gunakan:

- .NET SDK 10
- Docker dan Docker Compose
- `curl`
- Python 3 untuk membaca token/ID dari JSON

Masuk ke repo dan siapkan variable:

```bash
cd ~/workspace/aspnet-core-domain-lab
BASE=http://localhost:8080
RUN_ID="$(date -u +%s)-$$"

json_value() {
  python3 -c 'import json, sys; print(json.load(sys.stdin)[sys.argv[1]])' "$1"
}
```

`RUN_ID` membuat identifier unique tanpa menghapus volume PostgreSQL. Setiap run baru mendapat MRN, asset code, customer reference, dan account reference baru. Duplicate request dalam run yang sama tetap dapat dipakai untuk mempelajari `409 Conflict`.

Jangan gunakan credential atau signing key development untuk production. Semua data domain di lab ini sintetis.

---

## 1. Orientasi: baca peta sebelum kode

Baca file berikut dalam urutan ini:

```text
README.md
CHEATSHEET-ASP.NET-CORE.md
CHAPTERS.md
PLAN.md
TODO.md
HANDOFF.md
Modules/README.md
```

### Apa yang dicari

1. **`README.md`** — tujuan lab, cara menjalankan API, credential sintetis, dan batasan non-production.
2. **`CHEATSHEET-ASP.NET-CORE.md`** — lookup API, HTTP contract, command, dan diagnosis selama tour.
3. **`CHAPTERS.md`** — indeks 16 bab dan lokasi kode setiap bab.
4. **`PLAN.md`** — keputusan arsitektur: satu Web API, Controllers, satu `DomainDbContext`, schema PostgreSQL per bounded domain.
5. **`TODO.md`** — catatan implementation baseline dan learning-quality batch; checkbox bukan bukti mastery.
6. **`HANDOFF.md`** — state sesi dan verifikasi terakhir yang dapat menjadi basi; keputusan durable tetap berada di `PLAN.md`.
7. **`Modules/README.md`** — batas healthcare, industrial, logistics, dan banking.

`REVIEW-ASP.NET-CORE.md` tidak perlu dibaca sekali duduk. Buka bagian yang sesuai ketika checkpoint membutuhkan penjelasan lebih dalam.

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
curl --fail --silent "$BASE/openapi/v1.json" | head -c 500
printf '\n'
curl --fail --silent --output /dev/null --write-out 'Swagger HTTP %{http_code}\n' \
  "$BASE/swagger/index.html"
```

### Checkpoint — host dan environment

- **Predict:** kedua request menghasilkan `200` hanya karena Compose memakai environment `Development`.
- **Run:** jalankan request OpenAPI dan Swagger di atas.
- **Observe:** OpenAPI berisi route `/api/v1`; Swagger mengembalikan `200`.
- **Explain:** tunjukkan blok `if (app.Environment.IsDevelopment())` serta `MapOpenApi()`/`UseSwaggerUI()` di `Program.cs`.

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
  }' | json_value accessToken)
```

Buat patient:

```bash
PATIENT_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/healthcare/patients" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header 'Content-Type: application/json' \
  --data "{
    \"medicalRecordNumber\": \"MRN-TOUR-$RUN_ID\",
    \"displayName\": \"Synthetic Tour Patient\",
    \"birthDate\": \"1988-05-10\"
  }")
printf '%s\n' "$PATIENT_JSON"
PATIENT_ID=$(printf '%s' "$PATIENT_JSON" | json_value id)
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
  --data '{}' \
  --write-out '\nHTTP %{http_code}\n'
```

Ulangi MRN yang sama untuk memicu domain conflict:

```bash
curl --silent --request POST \
  "$BASE/api/v1/healthcare/patients" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header 'Content-Type: application/json' \
  --data "{
    \"medicalRecordNumber\": \"MRN-TOUR-$RUN_ID\",
    \"displayName\": \"Duplicate Synthetic Tour Patient\",
    \"birthDate\": \"1988-05-10\"
  }" \
  --write-out '\nHTTP %{http_code}\n'
```

Coba domain invariant yang lolos model validation tetapi ditolak service:

```bash
curl --silent --request POST \
  "$BASE/api/v1/healthcare/patients/$PATIENT_ID/appointments" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{
    "startsAtUtc": "2020-01-01T00:00:00Z",
    "provider": "Synthetic Provider"
  }' \
  --write-out '\nHTTP %{http_code}\n'
```

### Checkpoint — validation, domain invariant, dan persistence

- **Predict:** body kosong menghasilkan `400 validation_failed`, MRN duplikat menghasilkan `409 patient_already_exists`, dan appointment lampau menghasilkan `422 appointment_must_be_future`.
- **Run:** jalankan create/read patient dan tiga failure request di atas.
- **Observe:** patient `201` dapat dibaca kembali dengan `200`; ketiga failure memakai error contract healthcare.
- **Explain:** DataAnnotations bekerja sebelum action, sedangkan duplicate MRN dan future-only appointment diperiksa di `HealthcareService`.
- **Trace:** ikuti `HealthcareController -> HealthcareService -> DomainDbContext -> healthcare.patients`.

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
CORRELATION_ID="tour-$RUN_ID"
curl --silent --include \
  "$BASE/api/v1/healthcare/patients/$PATIENT_ID" \
  --header "Authorization: Bearer $HEALTH_TOKEN" \
  --header "X-Correlation-Id: $CORRELATION_ID"
```

Cari header response dan completion log dengan correlation ID yang sama:

```bash
docker compose logs --since 2m --no-color api | grep "$CORRELATION_ID"
```

### Checkpoint — middleware dan correlation logging

- **Predict:** response mempertahankan `X-Correlation-Id`; log completion memuat method, path, status, elapsed time, dan correlation ID yang sama.
- **Run:** kirim request dan baca log di atas.
- **Observe:** satu ID menghubungkan client response dengan server log.
- **Explain:** `RequestDiagnosticsMiddleware` memasang response header, structured scope, dan explicit correlation field sebelum memanggil middleware berikutnya.

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

Bandingkan tanpa identity, identity yang diizinkan, dan identity dengan role salah:

```bash
# Tanpa bearer token.
curl --silent --write-out '\nHTTP %{http_code}\n' \
  "$BASE/api/v1/auth/me"

# Token healthcare dapat membaca identity dan probe healthcare.
curl --silent --write-out '\nHTTP %{http_code}\n' \
  "$BASE/api/v1/auth/me" \
  --header "Authorization: Bearer $HEALTH_TOKEN"

curl --silent --write-out '\nHTTP %{http_code}\n' \
  "$BASE/api/v1/auth/probes/healthcare" \
  --header "Authorization: Bearer $HEALTH_TOKEN"

# Token valid, tetapi tidak memiliki role banking.transfer.
curl --silent --write-out '\nHTTP %{http_code}\n' \
  "$BASE/api/v1/auth/probes/banking-transfer" \
  --header "Authorization: Bearer $HEALTH_TOKEN"
```

### Checkpoint — authentication versus authorization

- **Predict:** tanpa token menghasilkan `401`; token healthcare menghasilkan `200` pada `/me` dan probe healthcare; token yang sama menghasilkan `403` pada probe banking-transfer.
- **Run:** jalankan empat request di atas.
- **Observe:** `401` berarti identity belum terbentuk; `403` berarti identity valid tetapi policy menolak role.
- **Explain:** tunjukkan `UseAuthentication()`, `UseAuthorization()`, JWT bearer validation, dan `AuthPolicies.Configure`.

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

Uji persistence melewati restart proses API:

```bash
docker compose restart api
until curl --fail --silent "$BASE/openapi/v1.json" > /dev/null; do sleep 1; done
curl --fail --silent \
  "$BASE/api/v1/healthcare/patients/$PATIENT_ID" \
  --header "Authorization: Bearer $HEALTH_TOKEN"
```

### Checkpoint — EF Core dan persistence

- **Predict:** lima schema memiliki table; patient tetap dapat dibaca setelah API restart.
- **Run:** periksa schema, restart API, lalu baca `PATIENT_ID` yang dibuat sebelumnya.
- **Observe:** process memory hilang tetapi row PostgreSQL tetap ada.
- **Explain:** tunjukkan connection string, persistent volume, migration startup, dan `ToTable(table, schema)` di `DomainDbContext`.

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
  }' | json_value accessToken)

ASSET_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/industrial/assets" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
  --header 'Content-Type: application/json' \
  --data "{
    \"assetCode\": \"TOUR-ASSET-$RUN_ID\",
    \"displayName\": \"Synthetic Tour Asset\"
  }")
ASSET_ID=$(printf '%s' "$ASSET_JSON" | json_value id)

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
  }' | json_value accessToken)
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
  }' | json_value accessToken)

CUSTOMER_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/banking/customers" \
  --header "Authorization: Bearer $BANKING_TOKEN" \
  --header 'Content-Type: application/json' \
  --data "{
    \"customerReference\": \"TOUR-CUSTOMER-$RUN_ID\",
    \"displayName\": \"Synthetic Tour Customer\"
  }")
CUSTOMER_ID=$(printf '%s' "$CUSTOMER_JSON" | json_value id)

ACCOUNT_JSON=$(curl --fail --silent --request POST \
  "$BASE/api/v1/banking/customers/$CUSTOMER_ID/accounts" \
  --header "Authorization: Bearer $BANKING_TOKEN" \
  --header 'Content-Type: application/json' \
  --data "{
    \"accountReference\": \"TOUR-ACCOUNT-$RUN_ID\",
    \"currency\": \"SIM\",
    \"initialBalanceMinor\": 10000
  }")
ACCOUNT_ID=$(printf '%s' "$ACCOUNT_JSON" | json_value id)
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

Lihat application-cache miss, hit, lalu invalidation. Query marker berbeda memastikan request mencapai service, sedangkan service tetap memakai cache key asset yang sama.

```bash
curl --fail --silent \
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry?tour=miss" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" > /dev/null
curl --fail --silent \
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry?tour=hit" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" > /dev/null

# Mutation menghapus cache key telemetry asset.
curl --fail --silent --request POST \
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
  --header 'Content-Type: application/json' \
  --data '{"value":43,"unit":"percent"}' > /dev/null

curl --fail --silent \
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry?tour=after-invalidation" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" > /dev/null

docker compose logs --since 2m --no-color api | \
  grep "industrial:telemetry:$ASSET_ID"
```

### Checkpoint — application cache

- **Predict:** read pertama log `miss`, read kedua log `hit`, mutation log `Removed`, dan read terakhir log `miss` lagi.
- **Run:** jalankan rangkaian di atas dalam waktu kurang dari TTL tiga detik.
- **Observe:** gunakan log event, bukan perbandingan timing, sebagai bukti cache behavior.
- **Explain:** tunjukkan `TelemetryCacheKey`, `TryGet`, `Set`, dan `Remove` di service/cache abstraction. Output cache adalah layer berbeda dan tidak menjadi bukti application-cache hit.

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
  "$BASE/api/v1/industrial/assets/$ASSET_ID/telemetry?tour=compression" \
  --header "Authorization: Bearer $INDUSTRIAL_TOKEN" | \
  tr -d '\r' | grep -i '^content-encoding: gzip$'

if curl --silent --dump-header - --output /dev/null \
  --header 'Accept-Encoding: gzip' \
  "$BASE/api/v1/banking/accounts/$ACCOUNT_ID" \
  --header "Authorization: Bearer $BANKING_TOKEN" | \
  tr -d '\r' | grep -qi '^content-encoding:'; then
  echo 'Unexpected: banking response was compressed' >&2
  exit 1
else
  echo 'Expected: banking response is not compressed'
fi
```

### Checkpoint — response compression

- **Predict:** industrial telemetry memiliki `Content-Encoding: gzip`; banking tidak memiliki `Content-Encoding` walau client menawarkan gzip.
- **Run:** jalankan kedua request di atas.
- **Observe:** gunakan response header, bukan ukuran atau timing, sebagai bukti.
- **Explain:** tunjukkan `UseResponseCompression()` dan exclusion path di `DomainResponseCompressionProvider`.

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

Gunakan partition industrial yang fresh. Jika command baru saja diuji, tunggu 30 detik atau restart API terlebih dahulu.

```bash
for attempt in 1 2 3 4; do
  curl --silent --output /dev/null --dump-header - \
    --request POST \
    "$BASE/api/v1/industrial/assets/$ASSET_ID/commands" \
    --header "Authorization: Bearer $INDUSTRIAL_TOKEN" \
    --header 'Content-Type: application/json' \
    --data "{
      \"idempotencyKey\": \"tour-$RUN_ID-$attempt\",
      \"commandType\": \"start\"
    }" \
    --write-out "attempt=$attempt HTTP %{http_code}\n"
done
```

### Checkpoint — rate limiting

- **Predict:** tiga request pertama menghasilkan `202`; request keempat menghasilkan `429` dan `Retry-After: 5`.
- **Run:** jalankan loop di atas satu kali pada partition fresh.
- **Observe:** status dan header membuktikan limiter; tidak perlu mengandalkan timing.
- **Explain:** tunjukkan `PermitLimit = 3`, fixed window 30 detik, partition username, queue limit nol, dan endpoint metadata.

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

Jalankan gate repeatable yang digunakan repository:

```bash
./scripts/verify-learning-lab.sh
```

Gate tersebut memverifikasi:

- restore, warning-free build, Compose config, migration list, dan table pada lima schema;
- OpenAPI/Swagger serta path/tag module;
- authorization `401`, `403`, dan permitted `200`;
- representative `400`, `404`, `409`, `422`, dan rate-limit `429`;
- patient tetap tersedia setelah API restart;
- correlation completion log;
- application-cache miss, hit, dan invalidation;
- gzip untuk industrial dan exclusion untuk banking;
- tidak ada unhandled failure pada verification log window.

Script membuat data sintetis dengan `RUN_ID` unique, me-restart API satu kali, menghentikan Compose setelah selesai, dan mempertahankan volume PostgreSQL. Ia tidak pernah menjalankan `docker compose down -v`.

Buktikan repeatability dengan volume yang sama:

```bash
./scripts/verify-learning-lab.sh
./scripts/verify-learning-lab.sh
```

Gunakan `KEEP_STACK=1 ./scripts/verify-learning-lab.sh` jika service harus tetap berjalan untuk inspeksi lanjutan.

### Checkpoint — verification evidence

- **Predict:** run kedua tetap lulus walau row dari run pertama masih ada.
- **Run:** jalankan gate dua kali.
- **Observe:** kedua run memakai suffix berbeda dan tidak berhenti pada duplicate identifier.
- **Explain:** bedakan static gate, host gate, HTTP gate, state gate, dan operational-observation gate. Output saat ini adalah evidence; checklist lama hanya historical record.

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
