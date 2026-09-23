# Cheatsheet ASP.NET Core

Referensi cepat untuk ASP.NET Core 10 dan repository ini. Gunakan file ini saat menulis atau menelusuri kode. Baca `REVIEW-ASP.NET-CORE.md` untuk penjelasan konsep dan `TOUR.md` untuk latihan terarah.

## Mulai di sini

```text
request
  -> Kestrel dan host
  -> middleware masuk
  -> routing memilih endpoint
  -> authentication membentuk identity
  -> authorization memeriksa policy
  -> binding dan validation
  -> controller action atau Minimal API handler
  -> application/domain service
  -> database atau dependency lain
  -> formatting response
  -> middleware keluar
```

| Bentuk | Fungsi | Contoh |
| --- | --- | --- |
| `AddX(...)` | Mendaftarkan service atau konfigurasi sebelum `Build()` | `AddControllers()`, `AddOpenApi()` |
| `UseX(...)` | Menambahkan middleware. Urutan berpengaruh | `UseAuthentication()` |
| `MapX(...)` | Memetakan endpoint | `MapControllers()`, `MapOpenApi()` |
| `RequireX(...)` | Menambahkan policy pada endpoint | `RequireAuthorization()` |
| `WithX(...)` | Menambahkan metadata endpoint | `WithTags()` |

## Pilih extension point

| Kebutuhan | Pilih |
| --- | --- |
| Endpoint dengan convention MVC, model binding, dan attribute routing | Controller |
| Endpoint ringkas atau vertical slice | Minimal API |
| Perilaku lintas banyak endpoint, sebelum atau sesudah `next` | Middleware |
| Perilaku khusus action MVC | MVC filter |
| Perilaku khusus handler Minimal API | Endpoint filter |
| Keputusan akses berdasarkan principal atau resource | Authorization policy |
| Invariant yang harus berlaku dari semua entry point | Application/domain layer |

Controller dan Minimal API adalah gaya endpoint. Middleware membungkus request pipeline. Filter berjalan lebih dekat ke endpoint yang sudah dipilih.

## Urutan pipeline pada lab

```csharp
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRouting();
app.UseMiddleware<RequestDiagnosticsMiddleware>();
app.UseMiddleware<DomainErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.MapControllers();
```

Urutan ini sesuai kebutuhan lab, bukan template untuk semua aplikasi. Pertahankan constraint berikut:

- exception handler harus membungkus tahap yang exception-nya ingin dipetakan;
- routing harus mendahului middleware yang membaca endpoint metadata;
- authentication harus mendahului authorization;
- endpoint rate limiter harus berjalan setelah routing;
- middleware compression harus berada sebelum penghasil response yang ingin dikompresi.

## Peta 16 topik

| Topik | Mulai dari | API atau file utama | Periksa |
| --- | --- | --- | --- |
| Application Host | Startup, DI, configuration, lifetime | `WebApplication.CreateBuilder`, `Program.cs` | Lifetime service dan logic yang bocor ke startup |
| Controllers | Endpoint MVC | `AddControllers`, `MapControllers`, `[ApiController]` | Controller tetap tipis |
| Minimal API | Handler ringkas dan route group | `MapGet`, `MapPost`, `MapGroup`, `TypedResults` | DTO, validation, dan policy tetap diperlukan |
| Middleware | Concern lintas request | `UseMiddleware`, `HttpContext`, `next` | Urutan dan short-circuit |
| Filters | Concern dekat action/handler | MVC filters, `AddEndpointFilter` | Jangan taruh invariant domain di filter |
| REST dan HTTP | Resource, method, status, header | Controller di `Modules/*` | Status dan retry semantics konsisten |
| API Versioning | Contract lama masih dipakai client | `Asp.Versioning.Mvc`, `[ApiVersion]` | Compatibility, deprecation, dan lifecycle |
| Validation | Menolak shape atau nilai input | Data annotations, `[ApiController]` | Bedakan model validation dan domain rule |
| Error Handling | Memetakan failure ke contract | `DomainErrorHandlingMiddleware` | Bug server tidak berubah menjadi `400` |
| Logging | Context yang dapat dicari | `ILogger<T>`, scope, `Activity` | Jangan log token, secret, atau body sensitif |
| Authentication | Membentuk principal | `AddAuthentication`, JWT bearer | Issuer, audience, signature, dan expiry |
| Authorization | Memeriksa hak akses | policy, role, claim, `[Authorize]` | `401` berbeda dari `403` |
| EF Core | Query dan persistence | `DbContext`, LINQ, migrations | Scope, tracking, translation, transaction |
| Caching | Mengurangi pekerjaan berulang | `IMemoryCache`, output cache | Key, TTL, invalidation, dan privacy |
| Response Compression | Mengurangi payload text | `AddResponseCompression`, Brotli, Gzip | CPU, HTTPS risk, MIME type, `Vary` |
| Rate Limiting | Membatasi burst atau concurrency | `AddRateLimiter`, `UseRateLimiter` | Partition key dan `Retry-After` |
| API Documentation | Contract machine-readable | `AddOpenApi`, `MapOpenApi`, Swagger UI | Dokumentasikan failure dan security |

Lokasi implementasi rinci ada di `CHAPTERS.md`.

## HTTP dalam satu tabel

| Method/status | Arti praktis |
| --- | --- |
| `GET` | Membaca. Safe dan idempotent menurut semantics HTTP |
| `POST` | Membuat atau memproses. Tidak otomatis idempotent |
| `PUT` | Mengganti representation pada URI. Umumnya idempotent |
| `PATCH` | Mengubah sebagian representation sesuai contract |
| `DELETE` | Meminta penghapusan. Hasil akhir biasanya idempotent |
| `200 OK` | Berhasil dengan body |
| `201 Created` | Resource dibuat. Sertakan `Location` bila tersedia |
| `202 Accepted` | Diterima untuk diproses; pekerjaan belum tentu selesai |
| `204 No Content` | Berhasil tanpa body |
| `400 Bad Request` | Binding atau model validation gagal |
| `401 Unauthorized` | Credential tidak ada atau tidak valid |
| `403 Forbidden` | Identity valid, policy menolak |
| `404 Not Found` | Endpoint atau resource tidak ditemukan |
| `409 Conflict` | Request berbenturan dengan state saat ini |
| `412 Precondition Failed` | Precondition seperti `If-Match` gagal |
| `415 Unsupported Media Type` | Format body tidak didukung |
| `422 Unprocessable Content` | Bentuk terbaca, aturan semantik/domain gagal |
| `429 Too Many Requests` | Rate limit terlampaui |
| `500 Internal Server Error` | Bug atau failure server yang belum dipetakan |
| `503 Service Unavailable` | Service atau dependency sementara tidak siap |

Gunakan `ProblemDetails` atau contract error terstruktur. Jangan mengirim stack trace, connection string, token, secret, atau data pribadi.

## Validation dan error

```text
HTTP/body tidak terbaca       -> 400
model/request shape invalid   -> 400 pada lab ini
resource tidak ada            -> 404
state bentrok/duplikat         -> 409
invariant domain gagal        -> 422 pada lab ini
exception tak terduga         -> 500
```

Checklist:

- controller memvalidasi boundary HTTP;
- application/domain menjaga invariant;
- database menjaga constraint persistence;
- centralized handler menjaga format error dan logging;
- client melakukan branching pada status dan error code, bukan kalimat bebas.

`[ApiController]` memberi convention validation MVC. Minimal API .NET 10 dapat memakai `Microsoft.Extensions.Validation` dan `AddValidation()`, tetapi lab ini memakai Controllers.

## Authentication dan authorization

```text
tidak ada/invalid token -> 401
valid token, policy gagal -> 403
valid token, policy lolos -> endpoint berjalan
```

Urutan minimum:

```csharp
builder.Services.AddAuthentication(/* scheme */);
builder.Services.AddAuthorization(/* policies */);

app.UseAuthentication();
app.UseAuthorization();
```

Authentication membentuk `ClaimsPrincipal`. Authorization mengevaluasi policy, role, claim, atau resource. CORS, HTTPS, dan route tersembunyi bukan access control.

## EF Core

| Kebutuhan | Pilihan awal |
| --- | --- |
| Unit of work per request | `DbContext` scoped |
| Read-only query | projection DTO dan pertimbangkan `AsNoTracking()` |
| Beberapa write harus atomic | transaction |
| Benturan update | concurrency token dan conflict handling |
| Perubahan schema | migration |

Perintah lab:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations list
```

Waspadai query N+1, materialization terlalu awal, pagination tanpa ordering stabil, entity persistence sebagai response publik, dan penggunaan satu `DbContext` secara paralel.

## Cache, compression, dan rate limit

| Fitur | Pertanyaan sebelum dipakai | Sinyal runtime |
| --- | --- | --- |
| Memory/data cache | Apa key, TTL, source of truth, dan invalidation-nya? | hit, miss, remove |
| Output cache | Apakah response aman dibagi untuk key tersebut? | age/hit sesuai policy |
| Response cache | Apakah semantics dan header HTTP mengizinkan cache? | `Cache-Control`, `Vary`, validator |
| Compression | Apakah penghematan bandwidth melebihi biaya CPU dan risikonya? | `Content-Encoding`, `Vary` |
| Rate limit | Siapa partition-nya dan kapan client boleh retry? | `429`, `Retry-After` |

```bash
# Cek gzip pada endpoint industrial
curl -sS -D - -o /dev/null \
  -H 'Accept-Encoding: gzip' \
  -H "Authorization: Bearer $TOKEN" \
  "http://localhost:8080/api/v1/industrial/assets/$ASSET_ID/telemetry"
```

`EnableForHttps` default framework adalah `false`; lab mengaktifkannya secara eksplisit. Brotli dan Gzip adalah provider yang digunakan lab. Response banking sengaja dikecualikan oleh policy domain.

## OpenAPI dan versioning

```text
Compose OpenAPI : http://localhost:8080/openapi/v1.json
Compose Swagger : http://localhost:8080/swagger
Local OpenAPI   : http://localhost:5056/openapi/v1.json
Local Swagger   : http://localhost:5056/swagger
API routes      : /api/v1/...
```

ASP.NET Core 10 menghasilkan OpenAPI 3.1 melalui `Microsoft.AspNetCore.OpenApi`. Swagger UI adalah dependency terpisah. `.WithOpenApi()` deprecated pada .NET 10 (`ASPDEPR002`); gunakan metadata endpoint dan transformer yang didukung.

Versioning checklist:

- pilih path, query, header, atau media type;
- tentukan perubahan compatible dan breaking;
- dokumentasikan deprecation dan sunset;
- pisahkan contract version dari schema database.

## Diagnosis cepat

| Gejala | Periksa lebih dulu |
| --- | --- |
| `404` pada route yang terlihat benar | Prefix `/api/v1`, attribute route, `MapControllers()` |
| Selalu `401` | Header bearer, expiry, issuer, audience, signing key |
| Token valid tetapi `403` | Policy, role/claim, resource ownership |
| Model invalid tidak masuk action | `[ApiController]` dan model-state response factory |
| Middleware tidak melihat metadata | Posisi relatif terhadap routing |
| Cache mengembalikan data salah | Key, tenant/user dimension, invalidation |
| Response tidak terkompresi | `Accept-Encoding`, MIME type, ukuran, exclusion policy |
| Rate limit tidak bekerja | `UseRateLimiter()`, endpoint metadata, partition key |
| EF query lambat | SQL, projection, tracking, N+1, index, pagination |
| OpenAPI tidak sesuai runtime | Endpoint metadata, transformer, conditional domain behavior |

## Perintah harian

```bash
# Build dan Compose config
dotnet restore
dotnet build --no-restore --warnaserror
docker compose config --quiet

# Jalankan stack
docker compose up --build

# Gate lengkap; volume PostgreSQL tetap dipertahankan
./scripts/verify-learning-lab.sh
```

## Rujukan

- `REVIEW-ASP.NET-CORE.md`: penjelasan dan sumber resmi untuk 16 topik.
- `TOUR.md`: latihan `predict -> run -> observe -> explain`.
- `CHAPTERS.md`: lokasi implementasi setiap topik.
- `AspNetCoreDomainLab.http`: request interaktif.
- [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [HTTP Semantics, RFC 9110](https://www.rfc-editor.org/rfc/rfc9110)
- [Problem Details, RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)
