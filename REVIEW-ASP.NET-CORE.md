# Review teori ASP.NET Core

Dokumen ini menjelaskan konsep di balik ASP.NET Core Domain Lab. Fokusnya adalah request pipeline, batas tanggung jawab, trade-off, dan contract yang dapat diamati dari luar. Contoh API mengikuti ASP.NET Core 10 karena project menargetkan `net10.0`.

Pilih dokumen sesuai kebutuhan:

- `CHEATSHEET-ASP.NET-CORE.md` untuk lookup cepat saat menulis atau menelusuri kode;
- `TOUR.md` untuk latihan `predict -> run -> observe -> explain`;
- `CHAPTERS.md` untuk menemukan implementasi setiap topik;
- review ini untuk memahami alasan dan batas konsep.

Lab memakai Controllers untuk endpoint domain. Minimal API dan custom filters tetap dibahas sebagai pembanding agar pilihan extension point dapat dijelaskan tanpa menambah endpoint contoh yang tidak diperlukan.

Nama API, default template, dan tooling dapat berubah. Request pipeline, HTTP semantics, dependency boundary, dan public contract lebih stabil daripada syntax satu versi.

### Cakupan

Review ini membahas 16 topik dalam urutan yang mengikuti aliran request sebelum fitur pendukung:

1. Application Host
2. Controllers
3. Minimal API
4. Middleware
5. Filters
6. REST Fundamentals with ASP.NET Core
7. API Versioning
8. Validation
9. Error Handling
10. Logging
11. Authentication and Authorization
12. Entity Framework Core
13. Caching
14. Response Compression
15. Rate Limiting
16. API Documentation

### Cara membaca

Mulai dari request pipeline. Setelah itu, bedakan endpoint style dari extension point. Controllers dan Minimal API adalah dua cara mendefinisikan endpoint. Middleware dan Filters adalah dua cara menyisipkan perilaku di sekitar pemrosesan endpoint, tetapi keduanya bekerja pada boundary yang berbeda.

Jangan menyimpulkan bahwa semua fitur harus dipakai pada satu aplikasi. Sebuah API kecil mungkin hanya memerlukan host, satu endpoint style, middleware, validation, error handling, logging, dan authorization. Versioning, database, cache, compression, rate limiting, dan dokumentasi masuk ketika risiko atau kebutuhan contract memang membutuhkannya.

### Model mental request pipeline

Model mental berikut cukup untuk membaca sebagian besar arsitektur API ASP.NET Core:

```text
client
    -> HTTP server, biasanya Kestrel
    -> application host dan dependency injection
    -> middleware masuk sesuai urutan registrasi
    -> routing memilih endpoint dan metadata
    -> authentication membentuk principal
    -> authorization memeriksa policy
    -> endpoint invocation
         -> MVC filters, binding, deserialization, validation, action, dan result execution
         -> atau endpoint filters, binding, validation, handler, dan result execution
    -> application/domain logic dan infrastructure
         -> misalnya database atau service lain
    -> response dibentuk dan diformat
    -> middleware keluar dalam urutan terbalik
    -> client
```

Diagram tersebut adalah model konseptual, bukan janji bahwa setiap aplikasi mendaftarkan semua tahap dengan urutan yang sama. Routing, authentication, authorization, CORS, exception handling, compression, caching, rate limiting, dan endpoint execution mempunyai hubungan urutan yang penting. Membaca pipeline berarti menanyakan dua hal: siapa yang menerima request lebih dulu, dan siapa yang masih dapat mengubah response ketika tahap berikutnya selesai.

Middleware membungkus middleware sesudahnya. Karena itu middleware yang lebih luar dapat melakukan pekerjaan sebelum dan sesudah `next`. Middleware juga dapat berhenti tanpa memanggil `next`, sehingga request tidak pernah sampai ke endpoint. Filter bekerja lebih dekat dengan endpoint yang dipilih. Filter tidak menggantikan middleware global.

### Perbedaan empat komponen

| Komponen | Menjawab pertanyaan | Cakupan | Titik utama | Dapat menghentikan alur |
|---|---|---|---|---|
| Controllers | Bagaimana action MVC menerima request dan menghasilkan response? | Satu controller, action, atau seluruh MVC pipeline | Setelah routing memilih endpoint MVC | Ya, melalui result, authorization, atau filter |
| Minimal API | Bagaimana handler endpoint yang ringkas didefinisikan? | Satu route, route group, atau sekumpulan endpoint | Endpoint execution | Ya, melalui result, authorization, atau endpoint filter |
| Middleware | Perilaku lintas request atau lintas endpoint diletakkan di mana? | Seluruh pipeline atau cabang pipeline | Sebelum dan sesudah tahap berikutnya | Ya, dengan tidak meneruskan request |
| Filters | Perilaku yang khusus untuk pemanggilan action atau handler diletakkan di mana? | MVC action atau Minimal API endpoint | Di dalam endpoint framework | Ya, dengan short-circuit atau result |

Controllers dan Minimal API bukan dua pipeline yang terpisah dari ASP.NET Core. Keduanya memakai host, server, routing, middleware, dependency injection, serializer, authentication, authorization, dan banyak layanan yang sama. Perbedaan utamanya berada pada bentuk endpoint dan extension point yang tersedia di sekitar endpoint tersebut.

### Batas domain

Ada tiga boundary yang perlu dijaga:

| Boundary | Tanggung jawab |
|---|---|
| Framework dan transport | HTTP, routing, binding, serialization, status code, middleware, filters, host, dan integrasi security |
| Application dan domain | Use case, aturan bisnis, invariant, state transition, keputusan domain, dan contract internal |
| Infrastructure dan operasi | Database provider, identity provider, secrets, message broker, deployment, observability backend, dan topology runtime |

Controller atau handler boleh menerjemahkan HTTP ke input use case, tetapi sebaiknya tidak menjadi tempat seluruh aturan domain. Entity Framework Core dapat menyimpan state, tetapi tidak otomatis menentukan invariant bisnis. Authentication dapat memvalidasi identitas, tetapi tidak otomatis menentukan apakah user boleh melakukan aksi tertentu pada resource tertentu.

### Konsep inti, pendukung, dan di luar scope

Konsep inti review adalah Application Host, Controllers, Minimal API, Middleware, Filters, REST Fundamentals, Validation, Error Handling, Logging, serta Authentication and Authorization. Topik ini membentuk model mental request pipeline dan contract API.

Konsep pendukungnya adalah API Versioning, Entity Framework Core, Caching, Response Compression, Rate Limiting, dan API Documentation. Semuanya penting pada kondisi tertentu, tetapi tidak harus menjadi dependency setiap endpoint atau setiap domain.

Review ini tidak membahas UI dengan Razor Pages atau Blazor, SignalR, gRPC, message broker, background processing, deployment dan container orchestration, service mesh, secret management provider, full observability platform, atau aturan domain khusus. Sebagian konsep tersebut menjadi dependency pada sistem nyata, tetapi tidak dibahas lengkap di sini.

## 1. Application Host

Application Host adalah proses yang mengumpulkan konfigurasi, dependency injection, logging, lifetime, server web, environment, dan startup aplikasi. Pada hosting model modern, `WebApplicationBuilder` menyiapkan builder dan `WebApplication` merepresentasikan aplikasi yang sudah dibangun. Kestrel adalah HTTP server yang biasanya menerima koneksi, sedangkan host mengatur bagaimana server dan aplikasi hidup bersama.

Project web ASP.NET Core biasanya memakai `Microsoft.NET.Sdk.Web`. SDK tersebut menambahkan default, framework reference, dan tooling untuk aplikasi web; ia bukan pengganti host atau HTTP server. Host tetap dibentuk oleh kode startup dan mengelola lifetime aplikasi.

### Tanggung jawab host

Host menjawab pertanyaan yang lebih luas daripada endpoint:

- Dari mana konfigurasi dibaca?
- Environment apa yang aktif?
- Service apa yang tersedia melalui dependency injection?
- Logger provider apa yang digunakan?
- Kapan aplikasi mulai menerima request?
- Bagaimana aplikasi berhenti ketika process menerima shutdown signal?
- Server dan address apa yang digunakan untuk listening?

Alur konseptual startup adalah builder mengumpulkan configuration dan service registration, aplikasi dibangun menjadi service provider dan pipeline, server mulai listening, lalu host mengelola lifetime sampai shutdown. Startup yang gagal dapat membuat aplikasi tidak pernah menerima request. Ini berbeda dari request yang sudah diterima tetapi gagal di endpoint.

### Configuration dan environment

Configuration adalah kumpulan key-value yang dapat berasal dari file, environment variable, command line, atau provider lain. Environment seperti Development, Staging, dan Production biasanya mengubah perilaku logging, error detail, dan sumber configuration. Environment bukan security boundary. Siapa pun yang dapat mengubah environment atau configuration process dapat mengubah perilaku aplikasi.

Secrets tidak seharusnya ditanam di source, committed ke repository, atau dimasukkan ke response dan log. Local development dapat memakai secret store yang sesuai. Production memerlukan kebijakan dan provider yang berada di luar scope framework dasar.

### Dependency injection dan lifetime

Container DI menyimpan registration dan membuat object graph ketika dependency diminta. Tiga lifetime umum adalah transient, scoped, dan singleton. Scoped biasanya cocok untuk state yang hidup selama satu request, seperti `DbContext`. Singleton harus aman untuk dipakai banyak request dan tidak boleh memegang scoped service secara tidak sah.

DI bukan alasan untuk membuat semua type menjadi service atau menyembunyikan dependency di service locator. Constructor yang eksplisit biasanya memberi informasi lebih jelas. Host menyediakan composition root; domain code tetap sebaiknya dapat dipahami tanpa harus mengetahui detail container.

### Batas topik

Inti topik ini adalah hubungan antara SDK/project, host, server, configuration, DI, logging, dan application lifetime. Detail deployment, reverse proxy, container, cloud hosting, secret vault administration, dan tuning OS adalah topik operasi yang terpisah.

## 2. Controllers

Controller adalah endpoint style berbasis MVC. `ControllerBase` biasanya menjadi dasar controller API, sedangkan `Controller` juga membawa kemampuan view yang tidak diperlukan untuk API-only. Sebuah action menerima input dari route, query, header, atau body, menjalankan application logic, lalu menghasilkan result yang diterjemahkan menjadi HTTP response.

### Apa yang dilakukan MVC

MVC API pipeline menggabungkan beberapa kegiatan:

- routing dan action selection;
- model binding dari request ke parameter atau model;
- input formatter dan deserialization;
- model validation;
- authorization dan filters;
- pemanggilan action;
- result execution dan output formatting.

Pada setup berbasis attribute routing, `builder.Services.AddControllers()` mendaftarkan MVC services dan `app.MapControllers()` memetakan action controller menjadi endpoint. Detail setup dapat berbeda untuk aplikasi yang juga memakai views atau endpoint style lain.

Attribute seperti `[ApiController]` dapat mengaktifkan convention API tertentu, termasuk perilaku otomatis ketika model state tidak valid. Perilaku tersebut adalah fitur MVC controller, bukan aturan universal untuk semua Minimal API handler.

Controller sebaiknya menjadi boundary HTTP yang tipis. Ia menerjemahkan request DTO ke input use case dan menerjemahkan hasil use case ke status code, response DTO, atau error contract. Aturan seperti "rekening tidak boleh ditutup ketika masih memiliki saldo tertahan" adalah aturan domain, bukan alasan untuk menambah banyak branch di controller.

### Kapan controllers berguna

Controllers cocok ketika aplikasi memerlukan convention MVC yang jelas, banyak action dengan pola serupa, attribute routing, filter MVC, model binding yang kaya, atau struktur yang sudah dipahami tim. Controllers juga dapat memberi boundary file dan class yang mudah dinavigasi ketika jumlah endpoint bertambah.

Controllers tidak otomatis lebih enterprise, lebih lambat, atau lebih benar daripada Minimal API. Nilainya berasal dari struktur dan feature set yang dibutuhkan, bukan dari panjang class atau jumlah attribute.

### Kesalahan umum

- Controller memanggil database langsung, membuat aturan domain, dan membentuk semua response sekaligus.
- Entity persistence dikembalikan langsung sebagai contract publik sehingga perubahan schema mengubah API tanpa sengaja.
- Semua exception ditangkap di setiap action dengan format berbeda.
- Status code dipilih karena terlihat nyaman, bukan karena contract resource.
- Attribute menumpuk sampai alur eksekusi tidak lagi mudah dibaca.

Pemisahan yang lebih sehat adalah transport boundary, application service atau use case, domain model, dan infrastructure. Tidak semua aplikasi memerlukan empat project. Yang penting adalah dependency direction dan ownership keputusan.

## 3. Minimal API

Minimal API mendefinisikan endpoint melalui route handler yang dipetakan langsung pada aplikasi. Handler dapat menerima parameter dari route, query, header, body, atau dependency yang terdaftar di DI. Endpoint dapat dikelompokkan dengan route group dan diberi metadata untuk authorization, OpenAPI, tags, atau policy lain.

Minimal berarti permukaan ceremony lebih kecil, bukan berarti aplikasi tanpa arsitektur. Handler tetap perlu contract input, status code, validation, error handling, authorization, logging, dan pemisahan dari domain logic. Aplikasi Minimal API yang besar tetap membutuhkan struktur module, use case, DTO, dan test boundary yang jelas.

### Kekuatan dan trade-off

Kekuatan utama Minimal API adalah jarak pendek antara route dan handler, kemampuan membuat vertical slice, dan metadata endpoint yang mudah dikelompokkan. Ia cocok untuk API kecil, service dengan endpoint yang terfokus, atau fitur yang ingin dikelola per module.

Trade-off muncul ketika jumlah endpoint, aturan binding, metadata, dan cross-cutting behavior membesar. Tanpa konvensi internal, `Program.cs` dapat menjadi daftar route yang sulit dicari. Controllers menawarkan bentuk class/action yang lebih eksplisit; Minimal API menawarkan komposisi endpoint yang lebih langsung. Tidak ada aturan bahwa salah satunya harus dipakai secara eksklusif.

### Endpoint filters

Minimal API memakai endpoint filters untuk perilaku yang dekat dengan handler. Filter dapat membaca metadata dan argumen endpoint, memeriksa kondisi sebelum handler, lalu mengubah atau meneruskan hasil. Ini berbeda dari middleware yang melihat semua request dan berbeda dari MVC action filter yang berada di pipeline controller.

Minimal API juga dapat memakai middleware global, authentication, authorization, serialization, dan layanan yang sama dengan controllers. Perbedaan endpoint style tidak menghapus request pipeline.

### Batas tanggung jawab

Handler sebaiknya mengkoordinasikan transport dan use case. Query database, policy bisnis, dan formatting error yang panjang lebih jelas jika berada pada boundary yang memang memilikinya. Typed result atau contract result dapat memperjelas intent status code selama type tersebut tetap mencerminkan contract publik.

## 4. Middleware

Middleware adalah komponen berantai yang menerima `HttpContext` dan pilihan untuk meneruskan request ke komponen berikutnya. Secara konseptual setiap middleware dapat melakukan pekerjaan sebelum `next`, memanggil `next`, lalu melakukan pekerjaan setelah `next` ketika response bergerak kembali.

### Order adalah perilaku

Urutan registrasi menentukan urutan ingress dan memengaruhi apa yang dapat dilihat oleh komponen berikutnya. Middleware yang berada lebih awal dapat menangani exception dari middleware setelahnya jika ia membungkus tahap tersebut. Middleware yang tidak memanggil `next` melakukan short-circuit.

Contoh kelompok middleware yang sering membutuhkan urutan sadar:

- forwarded headers atau HTTPS redirect sebelum keputusan yang bergantung pada scheme;
- exception handling cukup luar agar dapat mengubah kegagalan menjadi response contract;
- routing sebelum middleware yang membutuhkan endpoint metadata;
- CORS pada posisi yang dapat menambahkan header ke response yang relevan;
- authentication sebelum authorization;
- authorization sebelum endpoint execution;
- compression dan caching di sekitar response sesuai policy;
- endpoint execution di bagian akhir jalur normal.

Daftar tersebut bukan template universal. Static files, health endpoints, proxy topology, CORS policy, dan framework version dapat mengubah susunan yang tepat. Prinsipnya adalah memahami dependency antar-middleware, bukan menghafal satu daftar.

### Cocok untuk middleware

Middleware cocok untuk exception boundary, correlation atau trace context, header umum, request logging, security headers, rate limiting global, dan policy yang berlaku lintas endpoint. Middleware juga cocok untuk mengelompokkan branch pipeline ketika aplikasi mempunyai cabang request yang benar-benar berbeda.

Middleware kurang cocok untuk aturan yang hanya bermakna setelah endpoint tertentu dipilih, seperti validasi argumen action tertentu atau pemeriksaan metadata resource tertentu. Untuk itu, endpoint filter, MVC filter, authorization policy, atau application service dapat menjadi boundary yang lebih tepat.

### Lifetime dan state

Middleware biasa dapat dibuat saat aplikasi dibangun dan dipakai berkali-kali. Jangan menyimpan state request pada field instance. Dependency scoped juga perlu dipakai pada boundary yang benar; dependency yang dibutuhkan per request biasanya diterima pada method invocation atau memakai bentuk middleware yang dikelola DI sesuai dokumentasi.

Middleware bukan tempat untuk mengandalkan urutan side effect yang tidak terdokumentasi. Jika sebuah komponen bergantung pada endpoint metadata, routing harus sudah melakukan endpoint selection. Jika sebuah komponen mengubah response body, ia harus memahami streaming, buffering, dan kemungkinan response sudah dimulai.

## 5. Filters

Filters adalah extension point yang berjalan di dalam pipeline framework endpoint. Pada MVC, filter dapat berada pada tahap authorization, resource, action, exception, atau result. Filter dapat diterapkan secara global, pada controller, atau pada action. Minimal API memiliki endpoint filters dengan model yang berbeda tetapi tujuan kedekatannya sama: mengelilingi handler tertentu.

### Filter versus middleware

Middleware melihat request pipeline secara umum. Ia dapat berjalan sebelum routing atau sebelum endpoint tertentu dipilih, tergantung posisinya. Filter biasanya berjalan setelah framework mengetahui endpoint dan memiliki context action atau handler. Karena itu filter dapat melihat argument, action metadata, atau result yang lebih spesifik.

Gunakan middleware ketika policy berlaku untuk banyak jenis endpoint dan tidak perlu mengetahui detail action. Gunakan filter ketika behavior hanya masuk akal untuk sekelompok action atau handler dan memerlukan context endpoint. Gunakan authorization policy untuk aturan akses, bukan filter ad hoc yang memeriksa claim secara tersebar.

### Filter versus domain logic

Filter dapat memeriksa format, metadata, atau concern transport. Filter tidak seharusnya menjadi tempat invariant domain. Invariant harus tetap berlaku jika use case dipanggil dari job, message consumer, test, atau endpoint lain yang tidak memakai filter tersebut.

Exception filter juga memiliki boundary terbatas. Ia tidak menangani exception dari tahap yang terjadi sebelum MVC memasuki filter pipeline, dan tidak menggantikan global exception handling middleware. Filter dapat menjadi alat tepat untuk kebutuhan MVC tertentu, tetapi contract error lintas endpoint lebih mudah dikendalikan dari boundary yang lebih luar.

### Risiko filter berlebihan

Filter yang terlalu banyak membuat behavior tersembunyi di attribute dan order menjadi sulit diprediksi. Periksa apakah perilaku tersebut sebenarnya lebih jelas sebagai middleware, authorization policy, validation component, atau application service. Extension point harus dipilih karena boundary-nya tepat, bukan karena dapat menyisipkan kode tanpa mengubah handler.

## 6. REST Fundamentals with ASP.NET Core

REST adalah gaya arsitektur yang menekankan resource, representation, uniform interface, stateless interaction, dan semantics HTTP. Banyak API menyebut dirinya REST meskipun hanya memakai JSON melalui HTTP. Label itu tidak otomatis memberi contract yang baik. ASP.NET Core menyediakan alat HTTP dan routing, tetapi developer tetap menentukan makna resource dan operasi.

### Resource dan method

URI sebaiknya memberi identitas resource, sedangkan HTTP method membawa intent operasi. `GET` mengambil representation dan seharusnya safe. `POST` biasanya meminta server membuat atau memproses sesuatu dan tidak otomatis idempotent. `PUT` umumnya mengganti representation pada URI tertentu dan memiliki semantics idempotent. `PATCH` menerapkan perubahan parsial dengan semantics yang ditentukan media type atau contract. `DELETE` meminta penghapusan dan biasanya idempotent dalam hasil akhir, walaupun log atau side effect internal tetap dapat muncul.

Idempotent bukan berarti request hanya boleh diproses sekali. Artinya pengulangan request yang sama memiliki intended effect yang sama menurut semantics method dan resource. Retry aman memerlukan pertimbangan tambahan seperti idempotency key, duplicate operation, dan transaction boundary. Idempotency key tidak termasuk materi inti 16 topik, tetapi menjadi dependency penting pada operasi yang dapat diulang.

### Status HTTP dan contract

Status code bukan hiasan response. Ia adalah sinyal untuk client, proxy, retry policy, observability, dan dokumentasi. Body response tidak menggantikan status code.

| Status | Makna umum pada API |
|---|---|
| 200 OK | Operasi berhasil dan response representation tersedia |
| 201 Created | Resource baru dibuat; `Location` dapat menunjukkan resource tersebut |
| 202 Accepted | Request diterima untuk diproses kemudian; bukan bukti pekerjaan sudah selesai |
| 204 No Content | Operasi berhasil tanpa representation pada body |
| 400 Bad Request | Request tidak dapat diproses sebagai input HTTP atau contract yang valid |
| 401 Unauthorized | Client belum memiliki authentication yang valid; server dapat memberi challenge |
| 403 Forbidden | Identitas diketahui tetapi tidak diizinkan melakukan operasi |
| 404 Not Found | Resource atau endpoint tidak tersedia; kadang dipakai untuk menyembunyikan existence |
| 405 Method Not Allowed | Path ada tetapi method tersebut tidak didukung |
| 409 Conflict | Request bertentangan dengan state resource atau aturan concurrency |
| 412 Precondition Failed | Precondition header seperti `If-Match` tidak terpenuhi |
| 415 Unsupported Media Type | Format request body tidak didukung |
| 422 Unprocessable Content | Syntax dapat dibaca tetapi isi melanggar semantic contract, jika policy API memilih status ini |
| 429 Too Many Requests | Request dibatasi oleh rate limit atau quota |
| 500 Internal Server Error | Kegagalan server yang tidak dipetakan sebagai error client |
| 503 Service Unavailable | Service sementara tidak siap atau dependency unavailable, bila pemetaan itu memang benar |

Tidak ada satu tabel yang memaksa semua domain memakai mapping identik. Yang penting adalah mapping konsisten, dapat dijelaskan, dan tidak menyamarkan bug server sebagai kesalahan client.

### Error contract

Error response sebaiknya mempunyai bentuk machine-readable yang konsisten. `ProblemDetails` adalah model yang umum dipakai ASP.NET Core untuk contract ini. Model tersebut memiliki field seperti `type`, `title`, `status`, `detail`, dan `instance`, serta dapat memiliki extension seperti code error, field errors, atau trace identifier.

RFC 7807 memperkenalkan Problem Details dan telah diperbarui oleh RFC 9457. ASP.NET Core documentation masih sering menyebut RFC 7807 dalam contoh API. Yang penting adalah menyepakati media type `application/problem+json`, arti field, stabilitas code, dan informasi apa yang tidak boleh dibocorkan.

`detail` tidak boleh menjadi tempat stack trace, connection string, token, atau data pribadi pada production. Trace ID yang aman dapat membantu operator mencari detail di log tanpa menjadikan response sebagai log dump. Validation errors dapat memakai extension terstruktur yang membedakan field, code, dan pesan yang aman ditampilkan.

### HTTP contract yang sering dilupakan

Contract juga mencakup content type, charset, header cache, ETag, pagination, ordering, filter, authentication scheme, dan perilaku ketika resource tidak ditemukan. Dokumentasi endpoint perlu menjelaskan success dan failure beserta schema yang relevan.

## 7. API Versioning

API versioning mengelola perubahan contract publik ketika client tidak dapat bermigrasi serentak. Segmen `/v1` hanya menyatakan versi pada URL; strategi lengkapnya juga mengatur compatibility, dokumentasi, deprecation, dan lifecycle. Versi berada pada boundary API yang dikonsumsi client, bukan pada pembagian tabel database.

### Pilihan representasi versi

Bentuk yang umum adalah:

- version pada URI path, misalnya segmen versi;
- query parameter;
- custom header;
- media type atau content negotiation.

Path version mudah dilihat, mudah di-cache dan didokumentasikan, tetapi versi menjadi bagian URL. Header atau media type menjaga URI tetap resource-oriented, tetapi version lebih tersembunyi dari manusia dan tooling sederhana. Tidak ada pilihan universal. Consistency, discoverability, routing, caching, dan kemampuan client menjadi pertimbangan utama.

ASP.NET Core routing menyediakan mekanisme memilih endpoint berdasarkan route dan metadata. Ia tidak sendirian menetapkan seluruh kebijakan versioning, compatibility, deprecation, dan version-aware documentation. Ekosistem .NET memiliki package `Asp.Versioning.Http` dan `Asp.Versioning.Mvc` untuk kebutuhan tersebut. Package itu bukan bagian dari konsep host atau routing dasar dan versinya harus dipilih berdasarkan target framework serta dokumentasi package aktif.

### Perubahan compatible dan breaking

Perubahan additive sering lebih mudah dipertahankan: field response baru yang optional, endpoint baru, atau request field optional dengan default yang tidak mengejutkan. Menghapus field, mengganti arti field, mengubah tipe, memperketat validation, mengubah ordering yang diandalkan client, atau menambah enum value dapat menjadi breaking change.

Version baru tetap memerlukan policy lifecycle: siapa yang memelihara versi lama, kapan deprecation diumumkan, bagaimana sunset dikomunikasikan, dan bagaimana dokumentasi memisahkan setiap versi. Menyalin seluruh codebase untuk setiap versi sering menciptakan bug divergence. Lebih baik memisahkan contract adapter dari application/domain behavior ketika aturan bisnisnya masih sama.

### Batas versioning

Versioning tidak menyelesaikan compatibility database, message schema, authentication provider, atau mobile client release process secara otomatis. Ia juga tidak membenarkan response yang berbeda tanpa dokumentasi. Jika perubahan dapat dilakukan secara additive tanpa memecah client, menambah versi mungkin hanya menambah biaya operasi.

## 8. Validation

Validation adalah proses menolak input atau state yang tidak memenuhi contract. Validation harus dibedakan dari deserialization, authentication, authorization, dan domain invariant. Request yang dapat dibaca belum tentu valid; user yang authenticated belum tentu berhak; input yang lolos schema belum tentu sah menurut bisnis.

### Lapisan validation

Model mental yang berguna:

1. Transport validation memeriksa method, route, content type, dan bentuk body.
2. Binding dan schema validation memeriksa apakah nilai dapat dipetakan ke type dan constraint input.
3. Application validation memeriksa apakah use case dapat dilakukan pada request tersebut.
4. Domain validation menjaga invariant yang harus berlaku pada semua entry point.
5. Persistence constraint menjaga aturan database seperti uniqueness atau foreign key.

Lapisan tersebut dapat menghasilkan error yang berbeda. Jangan menghapus domain validation hanya karena controller atau Minimal API sudah memeriksa annotation. Use case dapat dipanggil dari background process, test, import, atau endpoint lain.

### Controllers dan Minimal API

Controllers dengan `[ApiController]` mempunyai behavior otomatis untuk sebagian model validation dan binding failure. Minimal API memiliki binding dan metadata sendiri; jangan mengasumsikan convention MVC berlaku identik pada handler minimal.

Pada .NET 10, `Microsoft.Extensions.Validation` menyediakan validation untuk Minimal API dan beberapa host lain, tetapi dokumentasi resminya tidak menjadikannya pengganti validation MVC atau Razor Pages. Untuk Minimal API, service didaftarkan melalui `builder.Services.AddValidation()`. Framework kemudian menambahkan validation pada endpoint melalui endpoint filter dan source generator menemukan type yang relevan dari assembly tempat `AddValidation()` dipanggil. Jika endpoint berada di assembly lain, assembly tersebut perlu menyediakan registration-nya sendiri. Atribut `ValidatableTypeAttribute` dan `SkipValidationAttribute` masih experimental untuk aplikasi yang menargetkan .NET 10.

Apa pun endpoint style-nya, validation harus menghasilkan contract yang konsisten. Pilihan antara 400 dan 422 adalah keputusan API. 400 sering dipakai untuk invalid request umum, sedangkan 422 dapat memisahkan semantic validation dari malformed request. Yang penting client dapat membedakan field error, domain conflict, dan server failure.

### Validation bukan sanitization atau security lengkap

Validation memeriksa contract, bukan menggantikan parameterized query, output encoding, authorization, rate limiting, atau secret handling. Jangan menganggap string yang panjangnya valid otomatis aman. Jangan menaruh seluruh aturan domain di attribute jika aturan tersebut memerlukan repository, waktu, atau state lain.

Error pesan untuk manusia boleh berubah atau dilokalkan. Code error dan struktur machine-readable perlu lebih stabil agar client tidak melakukan branching berdasarkan kalimat bebas.

## 9. Error Handling

Error handling menentukan bagaimana kegagalan internal menjadi response, log, dan signal operasi. Tujuannya bukan menghilangkan semua exception. Tujuannya adalah membedakan error yang diharapkan, error domain, kegagalan dependency, dan bug yang tidak terduga.

### Kategori kegagalan

- Client mengirim request yang tidak memenuhi contract.
- Client authenticated tetapi melanggar policy atau resource rule.
- Operasi valid bertabrakan dengan state, misalnya concurrency conflict.
- Dependency gagal atau timeout.
- Bug aplikasi menyebabkan exception yang tidak diperkirakan.
- Client membatalkan request atau koneksi terputus.

Kategori tersebut tidak selalu memiliki satu status code. Timeout dependency yang dapat dipulihkan mungkin menjadi 503, sedangkan timeout akibat client cancellation tidak selalu perlu dikirim sebagai response normal. Mapping harus mempertimbangkan siapa yang dapat memperbaiki masalah dan apakah retry masuk akal.

### Centralized handling

Global exception handling middleware atau exception handler adalah tempat yang tepat untuk policy error lintas endpoint. Ia dapat menghasilkan Problem Details, memilih status code untuk exception yang sudah dikenal, menambahkan trace identifier, dan menyembunyikan detail sensitif pada production. `UseExceptionHandler` dan `AddProblemDetails` adalah contoh building block resmi; pilihan final tetap mengikuti contract aplikasi.

Exception handler tidak boleh mengubah setiap exception menjadi 400. Exception dari bug atau dependency bukan kesalahan input client. Mapping yang terlalu luas membuat monitoring tampak sehat karena defect dikirim sebagai response client error.

### Development versus production

Developer memerlukan stack trace dan context saat development. Client production memerlukan error contract yang aman dan dapat ditindaklanjuti. Detail debug sebaiknya berada di log yang terlindungi, bukan body response publik. Pastikan error handler sendiri tidak melempar exception ketika serializer, response, atau logging sedang gagal.

Jangan menangkap exception di setiap controller hanya untuk mengembalikan string berbeda. Pola itu menduplikasi contract dan sering kehilangan stack trace. Tangkap exception dekat dengan boundary yang dapat mengambil keputusan bermakna; propagasikan sisanya ke centralized handler.

## 10. Logging

Logging adalah catatan terstruktur tentang kejadian dan context aplikasi. Logging bukan pengganti response contract, metric, distributed trace, atau audit trail. Satu event dapat berguna untuk diagnosis tanpa menjadi bukti bahwa sebuah aturan bisnis sah.

### Structured logging

`ILogger<T>` menyediakan category, log level, event ID, message template, exception, dan scope. Catat parameter sebagai property terstruktur agar provider dapat melakukan filtering dan query berdasarkan field seperti request ID, operation, tenant, atau resource ID.

Level umum adalah Trace, Debug, Information, Warning, Error, dan Critical. Level menyatakan arti operasional, bukan seberapa penting programmer menganggap baris itu. Log Error seharusnya menandai kegagalan yang memerlukan perhatian, bukan semua input invalid dari client.

### Context dan correlation

Request dapat memiliki trace ID, span ID, correlation ID, atau identifier lain yang menghubungkan log dari beberapa komponen. ASP.NET Core dan .NET memakai `Activity` untuk context tracing pada banyak jalur. Nama field dan propagation policy perlu disepakati agar client support dan operator dapat mencari satu operasi end-to-end.

Scope membantu membawa context ke beberapa log tanpa menambahkan property manual pada setiap pesan. Context harus memiliki cardinality dan ukuran yang masuk akal. Jangan memasukkan access token, password, secret, full request body, atau data pribadi tanpa kebutuhan, minimization, masking, dan retention policy.

### Letak logging

Log di boundary penting: request masuk, keputusan authentication atau authorization yang aman, perubahan state penting, kegagalan dependency, dan hasil operasi yang relevan untuk diagnosis. Hindari logging yang sama di middleware, controller, service, dan repository sampai satu error menghasilkan empat event yang tidak dapat dibedakan.

Log exception bersama object exception agar stack trace dan inner exception tersedia. Jangan mengganti exception asli dengan message yang kehilangan cause. Logging config, sink, retention, redaction, dan alerting adalah tanggung jawab operasi yang lebih luas.

## 11. Authentication and Authorization

Authentication menjawab "siapa principal ini?" Authorization menjawab "apa yang boleh dilakukan principal ini pada resource atau operation ini?" Authentication biasanya menghasilkan `ClaimsPrincipal` pada `HttpContext.User`. Authorization mengevaluasi policy, role, claim, requirement, dan kadang resource context.

### Authentication pipeline

Authentication scheme memilih handler yang tahu cara membaca credential, memvalidasi signature atau session, dan membuat identity. Untuk API, bearer token adalah pola umum; cookie, OpenID Connect, OAuth 2.0, dan identity provider mempunyai peran yang berbeda. OAuth 2.0 adalah authorization framework, sedangkan OpenID Connect menambahkan identity layer. Jangan menyamakan keduanya dengan mekanisme internal ASP.NET Core.

Authentication middleware perlu berjalan sebelum authorization middleware. Endpoint metadata atau attribute dapat meminta policy tertentu. `AllowAnonymous` dan default policy juga memengaruhi keputusan, sehingga endpoint yang tampak tidak memakai attribute belum tentu tanpa policy.

Pada .NET 10, known API endpoints yang memakai cookie authentication mengembalikan `401` atau `403` untuk kegagalan API, bukan redirect `302` ke halaman login. Behavior ini membantu client programmatic, tetapi tidak mengubah perbedaan antara authentication dan authorization.

### 401 dan 403

`401 Unauthorized` berarti request belum memiliki authentication yang valid atau belum memenuhi challenge. `403 Forbidden` berarti server memahami identity tetapi policy menolak akses. Mengembalikan 200 dengan body "not allowed" merusak semantics HTTP dan membuat client, monitoring, serta cache salah memahami hasil.

Server dapat memilih 404 untuk menyembunyikan keberadaan resource tertentu agar tidak membocorkan informasi. Itu adalah security policy yang harus konsisten, bukan pengganti authorization.

### Authorization policy

Role dan claim sederhana dapat cukup untuk aturan dasar. Policy-based authorization lebih eksplisit untuk requirement yang dapat digunakan ulang. Resource-based authorization diperlukan ketika keputusan bergantung pada resource yang sedang diakses, ownership, tenant, status workflow, atau kombinasi context lain.

Authorization harus mengikuti trust model. Periksa issuer, audience, expiry, scope, tenant, dan hubungan principal dengan resource; keberadaan claim bernama `role` belum cukup. CORS tidak melakukan authentication, HTTPS tidak memberi authorization, dan route tersembunyi tidak menjadi access control.

### Batas security

Identity provider, key rotation, token issuance, secret storage, threat modeling, audit trail, account recovery, dan compliance berada di luar fondasi 16 topik. ASP.NET Core membantu memvalidasi dan menerapkan policy, tetapi tidak memilih policy bisnis untuk aplikasi. Default yang aman adalah least privilege, deny by default untuk resource sensitif, dan tidak mempercayai input claim tanpa validasi scheme.

## 12. Entity Framework Core

Entity Framework Core adalah ORM yang memetakan object dan query .NET ke database relasional atau provider lain. `DbContext` menyediakan unit of work, identity map, change tracking, query translation, dan persistence boundary. Ia bukan database, bukan application service, dan bukan domain model otomatis.

### Siklus DbContext

Dalam aplikasi web, satu `DbContext` scoped per request sering menjadi default praktis. Context tidak thread-safe dan tidak boleh dipakai untuk operasi parallel yang tidak terkoordinasi. Sesuaikan lifetime, transaction boundary, dan cancellation dengan unit of work aplikasi.

LINQ expression dapat diterjemahkan menjadi query provider. Tidak semua method .NET dapat diterjemahkan. Memanggil materialization terlalu awal dapat memindahkan filtering ke memory. Projection ke DTO sering lebih aman daripada mengambil entity besar lalu membuang sebagian field.

### Tracking, query, dan performance

Tracking membantu EF Core mengetahui perubahan entity, tetapi tidak selalu dibutuhkan untuk query read-only. No-tracking dapat mengurangi overhead pada jalur baca tertentu. `Include`, projection, split query, pagination, index, dan query plan mempunyai trade-off yang perlu diukur.

N+1 query terjadi ketika aplikasi mengambil daftar parent lalu memicu query tambahan untuk setiap item. Masalah ini tidak diselesaikan hanya dengan menambah `Include`; bentuk data yang dibutuhkan dan batas response perlu dirancang dulu.

### Schema dan consistency

Migrations membantu mengelola perubahan schema, tetapi migration execution adalah bagian deployment dan operasi database. Transaction dapat menjaga beberapa perubahan atomic dalam boundary yang sesuai. Unique constraint, foreign key, concurrency token, dan isolation level dapat membantu, tetapi tidak menggantikan aturan domain atau idempotency pada operation.

Jangan mengembalikan entity EF langsung sebagai public API contract. Navigation property, proxy, internal field, dan perubahan schema dapat bocor ke response. Mapping entity ke DTO memisahkan model persistence dari representation API dan membuat perubahan setiap boundary lebih terkontrol.

### Batas EF Core

Database nyata mempunyai behavior yang tidak selalu sama dengan fake atau in-memory provider. Query translation, transaction, constraint, locking, dan collation perlu diverifikasi pada provider yang digunakan. Integration test dengan database berada di luar bahan teori ini, tetapi boundary tersebut harus diakui sejak desain.

## 13. Caching

Caching menyimpan hasil atau data agar request berikutnya tidak perlu membayar biaya yang sama. Cache mengubah trade-off latency, throughput, freshness, memory, privacy, dan consistency. Cache bukan source of truth dan bukan perbaikan otomatis untuk query atau domain design yang buruk.

### Jenis cache

- HTTP client atau proxy cache mengikuti header seperti `Cache-Control`, `ETag`, `Last-Modified`, dan `Vary`.
- Response caching server mengikuti semantics HTTP dan apakah response boleh disimpan.
- Output caching server menyimpan hasil endpoint berdasarkan policy aplikasi.
- `IMemoryCache` menyimpan data pada process tertentu.
- Distributed cache menyimpan data pada service bersama yang dapat diakses beberapa instance.
- `HybridCache` menggabungkan cache lokal dan distributed cache, serta menyediakan stampede protection untuk operasi yang memakai key sama.

ASP.NET Core juga menyediakan middleware response caching dan output caching. Response caching mengikuti HTTP cache headers melalui `AddResponseCaching()` dan `UseResponseCaching()`. Output caching memakai policy server-side melalui `AddOutputCache()`, `UseOutputCache()`, dan metadata seperti `CacheOutput()`. Output caching tidak boleh ditempatkan atau dikonfigurasi sehingga response personalized dapat dibaca principal lain.

Response cache dan data cache bukan hal yang sama. Response cache menyimpan representation HTTP. Data cache menyimpan object atau hasil query yang mungkin dipakai oleh beberapa use case. Output cache lebih mudah dipahami sebagai cache hasil endpoint, tetapi tetap memerlukan key, policy, dan invalidation yang benar.

### Cache key dan privacy

Cache key harus memasukkan semua dimensi yang mengubah response, seperti URI, query, locale, tenant, role, atau representation format. Header `Vary` dapat memberi tahu cache HTTP tentang dimensi tertentu. Caching response personalized atau sensitif tanpa partition yang benar dapat menjadi kebocoran data.

Pertanyaan yang harus dijawab sebelum caching adalah: kapan data dianggap stale, siapa yang membatalkan cache, apa yang terjadi ketika cache kosong, dan apakah dua request boleh menerima data yang sama. Cache stampede, thundering herd, eviction, serialization failure, dan distributed consistency adalah failure mode yang perlu dirancang.

### Letak dan batas cache

Cache di depan endpoint mengurangi pekerjaan HTTP dan application layer. Cache di sekitar query mengurangi beban database. Cache di client atau proxy mengurangi request ke server. Setiap layer mempunyai policy dan visibility berbeda.

Jangan memakai cache untuk menyembunyikan authorization bug, menampung state transaksi yang harus konsisten, atau menggantikan database. Untuk data user-specific, key dan invalidation harus lebih ketat daripada cache data publik. Cache juga perlu logging dan metric agar hit, miss, eviction, dan stale behavior dapat diamati.

## 14. Response Compression

Response compression mengurangi ukuran body HTTP yang dikirim ke client dengan biaya CPU untuk kompresi dan dekompresi. Negosiasi biasanya menggunakan `Accept-Encoding` dari client dan response `Content-Encoding` dari server. Brotli dan Gzip adalah algoritme umum pada web API.

Konfigurasi dasar memakai `AddResponseCompression()` dan `UseResponseCompression()`. Pada ASP.NET Core .NET 10, provider default yang relevan adalah Brotli dan Gzip. Middleware menambahkan `Vary: Accept-Encoding` ketika response dikompresi agar cache membedakan representation berdasarkan encoding. `EnableForHttps` default-nya `false` karena compression pada response dinamis melalui HTTPS dapat menambah risiko seperti BREACH.

### Apa yang layak dikompres

JSON, text, XML, dan format tekstual lain biasanya mendapat manfaat. JPEG, PNG, ZIP, PDF tertentu, dan format yang sudah terkompresi sering tidak mendapat manfaat berarti. Response kecil juga dapat lebih mahal dikompres daripada dikirim langsung. Threshold, MIME type, compression level, dan CPU budget adalah policy.

Cache atau proxy perlu membedakan representation berdasarkan `Accept-Encoding`, biasanya melalui `Vary`. Jika header tersebut tidak benar, client yang tidak mendukung encoding dapat menerima body yang tidak dapat dibaca. Compression memproses representation, bukan mengenkripsi data. HTTPS tetap dibutuhkan untuk confidentiality dan integrity.

### Security dan pipeline

Compression terhadap response dinamis yang mencampur secret dengan input attacker dapat menambah risiko side-channel seperti keluarga BREACH. Keputusan compression perlu mempertimbangkan jenis data, TLS, threat model, dan apakah response personalized.

Response compression berbeda dari request decompression. Client yang mengirim compressed request body memerlukan policy dan middleware yang berbeda. Jangan menyimpulkan bahwa mengaktifkan response compression otomatis menangani upload terkompresi.

Compression middleware harus berada pada posisi yang dapat melihat response endpoint dan header yang relevan, tetapi urutan dengan caching, static files, dan proxy harus mengikuti target behavior. Instrumentasi CPU, ukuran sebelum/sesudah, dan latency lebih berguna daripada mengaktifkan compression tanpa pengukuran.

## 15. Rate Limiting

Rate limiting membatasi jumlah atau concurrency request agar resource tetap tersedia, pemakaian lebih adil, dan abuse lebih sulit. Ia bukan authentication dan bukan pengganti authorization. Request dari identity yang valid tetap dapat terlalu banyak atau terlalu mahal.

### Algoritme dan partition

Algoritme umum meliputi fixed window, sliding window, token bucket, dan concurrency limiter. Fixed window mudah dipahami tetapi dapat mengalami burst di batas window. Sliding window memperhalus distribusi. Token bucket memberi kapasitas burst yang eksplisit. Concurrency limiter membatasi pekerjaan yang aktif, bukan jumlah request dalam periode waktu.

Limiter dapat diterapkan secara global, per endpoint, atau partition berdasarkan user, API key, tenant, IP, atau kombinasi. Partition key harus dipilih dari identity yang dapat dipercaya. IP di belakang proxy hanya bermakna jika forwarded header sudah divalidasi dari proxy yang dipercaya. Partition berdasarkan input bebas dapat menyebabkan memory growth dan evasion.

### Contract 429

Ketika limit terlampaui, response umum adalah `429 Too Many Requests`. `Retry-After` dapat memberi petunjuk kapan retry masuk akal. Body error tetap perlu mengikuti error contract umum, misalnya Problem Details. Client tidak boleh melakukan retry agresif tanpa backoff, terutama jika semua instance mengembalikan 429.

### Batas distribusi

Limiter in-process biasanya menghitung request per instance. Pada deployment multi-instance, total traffic dapat melampaui quota yang dimaksud jika tidak ada shared state atau gateway yang mengoordinasikan limit. Distributed rate limiting membawa latency, availability, dan consistency trade-off sendiri.

Rate limit sebaiknya ditempatkan sebelum pekerjaan mahal, tetapi setelah informasi yang diperlukan untuk partition tersedia. Endpoint login, search, export, upload, dan operasi database berat mungkin memerlukan policy berbeda. Rate limiting tidak menggantikan queue, backpressure, timeout, circuit breaker, atau capacity planning.

Service didaftarkan dengan `AddRateLimiter()`, middleware diaktifkan dengan `UseRateLimiter()`, dan policy endpoint dapat diterapkan dengan `RequireRateLimiting()`. Jika memakai endpoint-specific policy, `UseRateLimiter()` harus berada setelah routing. Global limiter dapat ditempatkan sebelum routing ketika tidak membutuhkan endpoint metadata. Policy tetap perlu diuji dengan beban yang mendekati kondisi penggunaan karena angka limit tanpa load test hanya dugaan.

## 16. API Documentation

API documentation adalah representation yang menjelaskan cara client berinteraksi dengan API. OpenAPI adalah format machine-readable yang dapat mendeskripsikan path, method, parameter, schema, response, security scheme, dan metadata. Dokumentasi manusia, generated OpenAPI document, dan interactive UI adalah tiga hal yang berhubungan tetapi tidak identik.

### Isi contract yang berguna

Dokumentasi endpoint sebaiknya menjelaskan:

- path dan HTTP method;
- parameter route, query, header, dan body;
- content type request dan response;
- status code success dan failure;
- schema serta field yang wajib atau optional;
- error contract dan validation error;
- authentication scheme dan required scope atau policy;
- pagination, filtering, ordering, idempotency, dan concurrency bila berlaku;
- deprecation dan API version;
- contoh yang tidak menyesatkan atau membocorkan secret.

Dokumentasi yang hanya memuat response 200 membuat client salah mendesain retry, validation, dan error handling. Error response, 401, 403, 404, 409, 429, dan 5xx yang relevan sama pentingnya dengan happy path.

### Generated documentation

ASP.NET Core modern memiliki dukungan first-party untuk menghasilkan OpenAPI document melalui package `Microsoft.AspNetCore.OpenApi`, service `AddOpenApi()`, dan endpoint `MapOpenApi()`. Pada .NET 10, document default memakai OpenAPI 3.1 dan document `v1` biasanya tersedia di `/openapi/v1.json` ketika endpoint dipetakan. Generator membaca route, metadata endpoint, type, dan konfigurasi yang tersedia. UI seperti Swagger UI atau Scalar adalah lapisan terpisah; menghasilkan OpenAPI tidak otomatis berarti aplikasi menyediakan UI publik.

Metadata endpoint dapat diperkaya melalui `WithName`, `WithTags`, `Produces`, `ProducesProblem`, `ProducesValidationProblem`, `Accepts`, atau `TypedResults`. Untuk perubahan document yang lebih khusus, gunakan transformer API yang didukung versi target. Jangan menjadikan `.WithOpenApi()` sebagai default baru pada .NET 10; dokumentasi terbaru menandainya deprecated (`ASPDEPR002`) untuk beberapa pola customization.

Generated document adalah hasil dari metadata dan convention, bukan bukti bahwa runtime behavior selalu sesuai. Handler dapat memiliki aturan domain, conditional response, authorization nuance, atau error mapping yang tidak sepenuhnya tertangkap generator. Karena itu document perlu direview sebagai contract dan dibandingkan dengan behavior nyata pada boundary yang relevan.

### Keamanan dan lifecycle

Dokumentasi internal dapat memuat endpoint yang tidak boleh dipublikasikan ke internet. Pisahkan document publik dan internal bila diperlukan. Jangan memasukkan credential nyata, token, internal host, atau schema database ke OpenAPI.

Versioned API memerlukan document per version atau cara eksplisit untuk menyatakan compatibility. Ketika contract berubah, dokumentasi, client generator, tests, dan deprecation notice perlu bergerak bersama. API documentation adalah bagian dari product contract, bukan file dekoratif yang hanya dibuka saat onboarding.

### Batas pembahasan

Bagian ini membahas OpenAPI sebagai contract documentation. Pemilihan UI, code generation penuh, SDK publishing, API portal, governance organisasi, dan contract testing antar-service adalah topik lanjutan.

### Sumber resmi

Sumber utama di bawah ini adalah dokumentasi Microsoft, RFC/IETF, atau spesifikasi resmi. Beberapa link memakai halaman tanpa `view` version agar redirect dokumentasi aktif dapat memilih versi terbaru. Saat implementasi dimulai, cocokkan lagi dengan target `net10.0` dan package yang benar-benar direstore.

#### Host, pipeline, endpoint, dan filters

- [ASP.NET Core fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/)
- [ASP.NET Core host and app startup](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host)
- [ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [ASP.NET Core routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
- [Create web APIs with ASP.NET Core controllers](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Controller action return types](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types)
- [Minimal APIs overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview)
- [Minimal API route handlers](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers)
- [Minimal API endpoint filters](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/min-api-filters)
- [Filters in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters)

#### HTTP, REST, versioning, validation, dan error

- [Design and develop resilient APIs](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design)
- [HTTP Semantics, RFC 9110](https://www.rfc-editor.org/rfc/rfc9110)
- [Problem Details for HTTP APIs, RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)
- [ASP.NET Core error handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors)
- [Model validation in ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation)
- [Validation in ASP.NET Core .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-10.0)
- [Microsoft.Extensions.Validation API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.validation?view=net-10.0)
- [ASP.NET API Versioning project documentation](https://github.com/dotnet/aspnet-api-versioning/wiki)
- [Asp.Versioning.Http package](https://www.nuget.org/packages/Asp.Versioning.Http)
- [Asp.Versioning.Mvc package](https://www.nuget.org/packages/Asp.Versioning.Mvc)

The API Versioning project documentation is included as an ecosystem reference, not as Microsoft product documentation. ASP.NET Core routing and endpoint metadata remain the framework foundation; the package supplies additional versioning policy and behavior.

#### Logging, authentication, dan authorization

- [Logging in .NET and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/)
- [HTTP logging in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-logging)
- [Overview of authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Overview of authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction)
- [Policy-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [Authentication and authorization in Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security)
- [Authentication behavior for API endpoints](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/api-endpoint-auth?view=aspnetcore-10.0)
- [OAuth 2.0, RFC 6749](https://www.rfc-editor.org/rfc/rfc6749)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)

#### Entity Framework Core dan performance

- [Entity Framework Core documentation](https://learn.microsoft.com/en-us/ef/core/)
- [DbContext lifetime, configuration, and initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [EF Core querying overview](https://learn.microsoft.com/en-us/ef/core/querying/)
- [EF Core change tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
- [ASP.NET Core caching overview](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/overview)
- [ASP.NET Core output caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output)
- [ASP.NET Core response caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response)
- [ASP.NET Core distributed caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)
- [ASP.NET Core response compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression)
- [ASP.NET Core rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)

#### API documentation

- [OpenAPI support in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview)
- [Generate OpenAPI documents in ASP.NET Core .NET 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
- [Generate OpenAPI documents in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)

### Catatan versi dan audit lanjutan

Audit ulang dokumen ketika target framework, package version, hosting template, validation behavior, OpenAPI generator, API versioning package, compression provider, rate limiter default, atau security guidance berubah. Syntax dari versi lama tidak otomatis cocok dengan target saat ini.

Healthcare, industrial automation, logistics, dan banking juga memerlukan audit trail, idempotency, concurrency, secret management, background processing, messaging, deployment, dan aturan domain khusus pada sistem nyata. Lab mencatat kebutuhan tersebut sebagai batas; 16 topik dalam review ini tidak mencakupnya.
