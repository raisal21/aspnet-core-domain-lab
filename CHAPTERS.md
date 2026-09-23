# Peta 16 Bab ASP.NET Core

File ini adalah indeks referensi dari 16 topik ke source code, bukan walkthrough kedua. Mulai dari `TOUR.md` untuk urutan belajar resmi dan checkpoint `predict -> run -> observe -> explain`. Gunakan `REVIEW-ASP.NET-CORE.md` untuk teori dan `CHEATSHEET-ASP.NET-CORE.md` untuk lookup cepat. Kembali ke file ini saat mencari lokasi implementasi; komentar inline dengan format `Bab N — ...` menandai titik utamanya.

| Bab | Topik | Kode terkait | Penjelasan singkat |
|---:|---|---|---|
| 1 | Application Host | `Program.cs`, `compose.yaml`, `Dockerfile` | `WebApplicationBuilder` membuat host, membaca konfigurasi, mendaftarkan dependency, membangun aplikasi, lalu menjalankan host. Compose dan Dockerfile menyediakan boundary runtime API/PostgreSQL. |
| 2 | Controllers | `Controllers/AuthController.cs`, `Modules/*/*Controller.cs` | Controller menerima HTTP request, memanggil service, dan mengubah hasil domain menjadi `ActionResult` tanpa menaruh aturan bisnis di endpoint. |
| 3 | Minimal API | `Program.cs` pada `MapControllers()` | Minimal API sengaja tidak dipakai untuk workflow domain. Titik ini menjadi perbandingan dengan pendekatan controller-based yang dipilih lab. |
| 4 | Middleware | `Program.cs`, `Infrastructure/Api/*Middleware.cs` | Pipeline menjalankan middleware untuk correlation ID, timing, error handling, authentication, authorization, rate limiting, dan output caching. Urutan pipeline menentukan perilaku request. |
| 5 | Filters | `Program.cs` dan atribut controller | Lab tidak mendaftarkan custom MVC filter. Validasi host memakai `ApiController`, concern lintas endpoint memakai middleware, dan authorization memakai metadata `[Authorize]`; ini keputusan trade-off yang disengaja. |
| 6 | REST Fundamentals | `Modules/*/*Controller.cs`, `Modules/*/*Service.cs` | Route nouns, HTTP verbs, status `201/200/202/204/404/409/422`, `CreatedAtAction`, idempotency, dan resource references menunjukkan kontrak REST workflow. |
| 7 | API Versioning | `[ApiVersion]`, `[Route("api/v{version:apiVersion}/...")]`, konfigurasi ApiExplorer di `Program.cs` | Versi berada di URL segment `/api/v1`; versi yang tidak disebutkan ditolak dan versi yang didukung dilaporkan. |
| 8 | Validation | Request records di `Modules/*/*Models.cs`, `DomainValidationResponseFactory` | Data annotations memvalidasi shape request. Model-state error diubah menjadi error contract yang mengikuti domain URL. Aturan bisnis tervalidasi lagi di service. |
| 9 | Error Handling | `DomainErrors.cs`, `DomainErrorHandlingMiddleware.cs`, `Rule(...)` di service | Exception domain dipetakan menjadi response healthcare/industrial/logistics/banking yang berbeda; exception tak terduga tetap dicatat dan diteruskan ke host. |
| 10 | Logging | `RequestDiagnosticsMiddleware.cs`, logger di semua service dan auth service | Structured logging merekam correlation ID, request timing, domain rule rejection, workflow event, cache event, dan concurrency/failure diagnosis. |
| 11 | Authentication & Authorization | `Infrastructure/Authentication/*`, `AuthController.cs`, `[Authorize]` | Login lokal memakai password hash, JWT access token, hashed refresh token, rotation/reuse detection, revocation, claims, roles, dan policy per domain. |
| 12 | Entity Framework Core | `DomainDbContext.cs`, `PersistenceServiceCollectionExtensions.cs`, `Migrations/*`, semua service | Satu context memetakan entity secara eksplisit ke schema PostgreSQL, memakai LINQ/transactions/concurrency token, dan migration history di `public`. |
| 13 | Caching | `DomainMemoryCache.cs`, industrial/logistics service, output-cache policy di `Program.cs` | Cache aplikasi memakai TTL dan invalidation saat mutation. Output cache diberi policy terpisah untuk telemetry dan logistics read. |
| 14 | Response Compression | `Program.cs`, `DomainResponseCompressionProvider.cs`, industrial/banking controller | Brotli/Gzip dipakai untuk response yang sesuai; request banking sengaja tidak dikompresi sebagai contoh policy domain yang berbeda. |
| 15 | Rate Limiting | `Program.cs`, `[EnableRateLimiting]` pada controller | Fixed-window limiter dipartisi per user/IP untuk command industrial, dispatch logistics, dan transfer banking; rejection mengembalikan `429` dan `Retry-After`. |
| 16 | API Documentation | `Program.cs`, `BearerSecurityOpenApiTransformer.cs`, Swagger UI, `README.md`, `AspNetCoreDomainLab.http` | Built-in OpenAPI menghasilkan document bertag per module dan security bearer hanya pada operation yang membutuhkan authorization; Swagger UI menyediakan eksplorasi interaktif. |

## Cara memakai indeks

Jika mengikuti lab dari awal, gunakan urutan dan request di `TOUR.md`. Jika sedang menelusuri satu konsep, mulai dari baris tabel yang sesuai, buka kode terkait, lalu cari `Bab N` untuk berpindah antar titik implementasi.

Alur source utamanya tetap `Program.cs -> Controller -> Models -> Service -> DomainDbContext`. Migration generated dianggap output EF Core; komentar pembelajaran ditempatkan pada context, konfigurasi, service, dan pipeline agar generated code tidak perlu diedit manual.
