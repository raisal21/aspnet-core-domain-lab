# Kritik Kualitas Belajar ASP.NET Core Domain Lab

## Verdict

Repository ini sudah kuat sebagai **reference implementation** dan **guided code tour** untuk lab pribadi. Scope non-production, synthetic boundary, integrasi host sampai database, dan perbandingan empat domain sudah tepat.

Kekurangan utama pada baseline bukan jumlah fitur. Saat audit awal, learning loop belum cukup eksplisit dan repeatable. `PLAN.md` menjelaskan aplikasi yang dibangun, `TODO.md` mencatat pekerjaan implementasi yang selesai, dan `HANDOFF.md` menyimpan konteks sesi. Ketiganya saat itu belum sepenuhnya membedakan:

- implementasi tersedia;
- perilaku pernah diverifikasi;
- latihan dapat diulang;
- pembelajar sudah memahami konsep.

Target perbaikan harus tetap kecil. Lab ini tidak perlu diubah menjadi kurikulum formal atau aplikasi production.

## Status Perbaikan

Empat prioritas utama sudah diterapkan pada working tree:

| Prioritas | Status | Bukti |
| --- | --- | --- |
| Pisahkan implementation dari learning checkpoint | Selesai | Status model di `README.md`/`PLAN.md`; sembilan checkpoint di `TOUR.md` |
| Ganti acceptance check berbasis heading | Selesai | `scripts/verify-learning-lab.sh` dengan static dan behavioral gates |
| Buat walkthrough repeatable | Selesai | `RUN_ID` unik, HTTP variables lengkap, dua run lulus pada volume yang sama |
| Segarkan handoff dan arahkan ke mastery | Selesai | `HANDOFF.md` memuat baseline commit, fresh evidence, temporary state, dan next action |

Gate lengkap lulus dua kali dengan run IDs `1789915358-67215` dan `1789915396-69397` terhadap baseline commit `6d217b0` plus working-tree changes. Detail environment dan hasil berada di `HANDOFF.md`.

Follow-up dokumentasi juga selesai: review teori dan cheatsheet sudah berada di repository. Cheatsheet sekarang berfungsi sebagai lookup cepat, sedangkan review menyimpan penjelasan dan sumber.

## Yang Sudah Baik

### Scope belajar terjaga

`PLAN.md` konsisten menyatakan bahwa empat domain adalah simulasi. Tidak ada real PII, real money, device actuation, compliance claim, atau production SLA. Batas ini harus dipertahankan.

### Implementasi menunjukkan kerja sama antarkomponen

Repository tidak berhenti pada snippet. Host, routing, middleware, validation, authorization, EF Core, caching, compression, rate limiting, dan OpenAPI dapat diikuti sebagai satu request pipeline.

### `TOUR.md` sudah menjadi jalur belajar yang layak

Tour memulai dari host dan route, mengikuti request sampai database, lalu membahas cross-cutting concerns. Ini lebih cocok sebagai jalur belajar resmi daripada urutan pembangunan di `PLAN.md`.

### Empat domain memberi konteks pembanding

Healthcare dapat menjadi vertical slice utama. Industrial, logistics, dan banking memberi variasi kebijakan dan failure mode. Scope empat domain tidak perlu dikurangi; pembelajar hanya perlu diarahkan agar tidak membaca semuanya sekaligus.

### Tidak semua fitur dipaksakan ke domain

Minimal API dan custom filters sengaja tidak menjadi endpoint style utama. Keputusan ini benar. Perbandingan konseptual atau snippet kecil cukup; implementasi endpoint tambahan tidak wajib.

## Prioritas yang Disepakati

### P1 — Pisahkan implementation status dari learning checkpoint

`TODO.md` mencatat 72 pekerjaan baseline sebagai selesai. Status tersebut berguna, tetapi bukan bukti bahwa pembelajar sudah menguasai materi.

Perbaikan menerapkan checkpoint ringan berikut:

1. **Prediksi** status, body, log, header, atau perubahan state.
2. **Jalankan** request atau command.
3. **Amati** hasil aktual.
4. **Jelaskan** mengapa hasilnya demikian dan tunjukkan source terkait.

Modifikasi kode atau eksperimen break/fix bersifat opsional. Tidak semua 16 topik wajib memiliki implementasi latihan khusus.

### P1 — Ganti acceptance check yang hanya memeriksa heading

Acceptance check sebelum revisi ini hanya membuktikan bahwa `TODO.md` tersedia dan memiliki heading tertentu. Ia tidak memverifikasi build atau perilaku API.

Perbaikan memakai gate yang proporsional untuk lab:

- restore dan build;
- validasi Compose dan migration list;
- host/OpenAPI smoke check;
- skenario HTTP utama dan failure path;
- observasi authorization, persistence, logging, cache, compression, dan rate limit yang memang menjadi tujuan lab.

Tidak perlu langsung membuat integration-test project. HTTP file, `curl`, dan checklist observasi dapat menjadi bukti yang cukup selama langkahnya repeatable dan expected result jelas.

Klaim verifikasi historis tidak otomatis salah hanya karena output lama tidak disimpan. Namun klaim tersebut harus diberi label historis, bukan dianggap fresh verification.

### P1 — Buat walkthrough dapat diulang

Temuan konkret pada baseline audit:

- `AspNetCoreDomainLab.http` memakai `{{accountId}}` tanpa definisi.
- `TOUR.md` memakai identifier tetap dengan suffix `001` untuk patient dan account.
- Volume PostgreSQL sengaja dipertahankan setelah `docker compose down`.
- Unique constraint dapat membuat run berikutnya berhenti pada conflict yang tidak direncanakan.

Solusinya tidak harus kompleks. Implementasi memilih suffix unik per run, mempertahankan volume, dan tetap memakai duplicate conflict sebagai expected observation. Reset volume tidak diperlukan.

Cakupan HTTP dinilai berdasarkan konsep dan workflow penting, bukan rasio jumlah request terhadap seluruh controller action.

### P1 — Segarkan `HANDOFF.md` dan arahkan next action ke mastery

Pada baseline audit, `HANDOFF.md` masih menyatakan repository belum memiliki commit, padahal `main` sudah memiliki commit dan remote. Durable architecture decision juga bercampur dengan state lokal yang mudah basi.

Handoff yang diperbarui memuat:

- baseline commit;
- verifikasi terakhir dan apakah hasilnya current atau historical;
- state lokal sementara;
- pekerjaan tersisa;
- next learning checkpoint.

Prioritas setelah baseline implementation bukan menambah fitur baru, melainkan memperkuat learning loop.

## Setuju dengan Catatan

### Materi teori sekarang berada di repository

Audit awal menemukan review teori dan cheatsheet hanya di vault lokal. Keduanya sekarang dilacak bersama source sehingga clone repository memiliki penjelasan 16 topik, quick reference, walkthrough, dan implementation map.

Perannya dipisahkan agar tidak menjadi tiga walkthrough yang bersaing: review menjelaskan konsep, cheatsheet membantu lookup, dan `TOUR.md` tetap menjadi jalur latihan resmi.

### Urutan implementasi berbeda dari urutan belajar

Ini benar, tetapi `TOUR.md` sudah menyelesaikan sebagian besar masalah. Tidak perlu menyusun kurikulum baru. Cukup tetapkan:

- `PLAN.md` sebagai scope dan keputusan;
- `TODO.md` sebagai status kerja dan completion record batch;
- `TOUR.md` sebagai jalur belajar resmi;
- `CHAPTERS.md` sebagai indeks konsep ke source;
- `HANDOFF.md` sebagai state sesi.

### Core, applied, dan advanced

Label ini dapat membantu mengurangi beban kognitif, tetapi hanya navigasi fleksibel, bukan standar kelulusan.

- **Core:** host, controllers, HTTP, middleware, validation, errors, logging, basic auth, basic EF Core, OpenAPI.
- **Applied:** policies, migration, cache, compression, rate limiting, transaction.
- **Advanced optional:** refresh-token reuse, optimistic concurrency, multi-schema mapping, dan idempotency.

### Trade-off perlu lebih aktif

Module README sudah mencatat trade-off. Perbaikannya cukup dengan pertanyaan keputusan:

1. Masalah apa yang diselesaikan?
2. Konsekuensi apa yang diterima?
3. Sinyal apa yang membuat desain perlu berubah?

Tidak perlu membuat ADR formal untuk setiap keputusan.

## Setuju Sebagian

### Minimal API dan filters memerlukan pembanding

Perbandingan berguna, tetapi tidak wajib berupa endpoint runnable. Tabel, snippet, atau pertanyaan skenario cukup. Endpoint domain tetap memakai Controllers.

### Empat domain meningkatkan beban kognitif

Beban tersebut nyata, tetapi empat domain memang tujuan lab. Solusinya adalah satu jalur utama melalui healthcare, lalu tiga domain lain sebagai comparative labs.

### HTTP harness masih terbatas

Jumlah request lebih sedikit daripada jumlah action bukan masalah dengan sendirinya. Harness perlu mencakup konsep yang dijanjikan, bukan setiap endpoint. Prioritasnya adalah repeatability dan expected observation.

## Kritik yang Tidak Dijadikan Requirement

Hal berikut tidak masuk batch wajib:

- setiap topik harus memiliki eksperimen implementasi;
- wajib membuat API v2;
- wajib memicu concurrency conflict;
- wajib menambah Minimal API atau custom filter ke aplikasi utama;
- mengurangi jumlah domain;
- menyeragamkan seluruh bahasa dokumentasi;
- menyalin materi vault lain yang tidak diperlukan lab;
- mengukur kualitas dari persentase endpoint yang ada di HTTP file;
- menjadikan lab sebagai kurikulum formal;
- menambahkan production infrastructure.

Campuran bahasa adalah preferensi selama istilah teknis konsisten. Eksperimen v2, concurrency, dan filter tetap boleh menjadi latihan opsional.

## Koreksi terhadap Kritik Sebelumnya

- `TODO.md` **mencatat** implementation completion; ia tidak membuktikannya.
- Klaim historis tidak otomatis invalid karena output tidak disimpan. Masalahnya adalah reproducibility dan freshness.
- Tujuh temuan P1 terlalu banyak. Prioritas wajib dipersempit menjadi learning checkpoint, meaningful verification, repeatable walkthrough, dan current handoff.
- Perbandingan 11 request dengan 47 actions hanya menunjukkan harness selektif, bukan kualitas rendah.
- `git diff --check -- CRITICS.md` tidak memeriksa file yang masih untracked. Validasi file untracked harus dilakukan dengan pemeriksaan whitespace terpisah atau setelah file ditambahkan ke index.

## Definition of Better

Batch kritik selesai dengan hasil berikut:

- [x] `PLAN.md` membedakan implementation baseline dan learning-quality batch.
- [x] `TODO.md` mencatat batch selesai dan acceptance evidence yang relevan.
- [x] `TOUR.md` dinyatakan sebagai jalur belajar resmi.
- [x] Review teori dan cheatsheet dilacak di repository dengan peran yang berbeda.
- [x] Walkthrough dapat diulang dengan state yang jelas.
- [x] HTTP example tidak memiliki variable workflow yang tidak didefinisikan.
- [x] Pembelajar diminta melakukan prediksi, observasi, dan penjelasan pada skenario utama.
- [x] `HANDOFF.md` tidak memiliki fakta Git yang basi pada saat refresh.
- [x] Verifikasi current dan historical dibedakan.
- [x] Tidak ada perluasan scope production.

## Kesimpulan

Repository ini sudah oke sebagai lab implementasi. Kritik yang valid harus dipakai untuk memperbaiki feedback loop, bukan menambah kompleksitas.

Batch sudah diselesaikan secara kecil dan terarah:

1. peran dokumen dan checkpoint pemahaman diperjelas;
2. walkthrough dan HTTP examples dibuat repeatable;
3. verification gate mengamati perilaku, bukan keberadaan heading;
4. handoff disegarkan;
5. fitur baru tetap ditunda.

Audit awal dan implementasi batch dilakukan terhadap baseline commit `6d217b0` pada `main`.
