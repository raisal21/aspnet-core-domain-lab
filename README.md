# ASP.NET Core Domain Lab

Backend-only ASP.NET Core learning application for comparing four bounded domains:

- healthcare
- industrial automation
- logistics
- banking

This is a realistic simulation for learning, not a production system. It must not use real PII, money, device actuation, compliance claims, or production SLA claims.

## Learning path

`TOUR.md` is the official guided learning path. Start there when studying the repository. It follows one request from the host through a controller and service to PostgreSQL, then compares the four domains and cross-cutting behavior.

The learning loop is:

1. **Predict** the status, response, log, header, or state change.
2. **Run** the documented command or request.
3. **Observe** the actual result.
4. **Explain** the result by tracing the relevant source.

Repository documents have separate roles:

| Document | Role |
| --- | --- |
| `REVIEW-ASP.NET-CORE.md` | Theory, trade-offs, boundaries, and official references for 16 topics |
| `CHEATSHEET-ASP.NET-CORE.md` | Scan-first lookup for APIs, status codes, commands, and diagnosis |
| `TOUR.md` | Official walkthrough and learning checkpoints |
| `CHAPTERS.md` | Reference index from 16 topics to source locations |
| `PLAN.md` | Scope, learning intent, architecture decisions, and non-goals |
| `TODO.md` | Completed implementation and learning-quality work record |
| `HANDOFF.md` | Temporary session state and latest verification evidence |
| `CRITICS.md` | Calibrated learning-quality review |
| Module README files | Domain workflow, trade-offs, and failure modes |

The repository is self-contained for the 16-topic learning path. Use the review when a concept needs explanation, the cheatsheet while coding or debugging, and the tour for hands-on study.

## Status model

Three statuses are intentionally different:

- **Implementation available:** the code or documentation exists.
- **Behavior verified:** a reproducible command observed the expected result in a named environment.
- **Concept understood:** the learner can predict, observe, explain, and trace the behavior.

A checked implementation item is not automatically proof of understanding. `TODO.md` records work completion; `TOUR.md` contains the learning checkpoints.

## Current implementation

The repository contains a `net10.0` Web API, a Docker Compose boundary for the API and PostgreSQL, schema-aware EF Core persistence, a local JWT issuer, URL-segment API versioning, Swagger UI, and complete synthetic workflows for all four domains.

The application intentionally uses one Web API project. Controllers are the primary endpoint style. Minimal API remains a comparison topic rather than the implementation style for domain endpoints.

Implemented shape:

- one PostgreSQL database with a schema per business module and a shared auth schema;
- one schema-aware `DomainDbContext` with migrations for auth and all four workflow schemas;
- local JWT issuer with refresh-token rotation, reuse detection, and revocation;
- synthetic development users and domain-specific authorization policies;
- one OpenAPI document tagged by module;
- Swagger UI at `/swagger` in Development, backed by `/openapi/v1.json`;
- URL-segment API versioning with the current contract under `/api/v1`;
- domain-specific validation/errors, request correlation logging, caching, compression, and rate limits;
- repeatable HTTP and shell verification;
- Docker Compose for the local API and PostgreSQL environment.

Each bounded case has a complete workflow, but irrelevant ASP.NET Core features are not forced into every domain.

## Prerequisites

- .NET SDK 10
- Docker with Docker Compose
- `curl`
- Python 3 for portable JSON parsing in the verification script

`jq` is not required.

## Run

```bash
dotnet restore
dotnet run
```

The development OpenAPI document is available at `http://localhost:5056/openapi/v1.json` when the application uses the generated development profile. Swagger UI is available at `http://localhost:5056/swagger`.

To run the local container boundary:

```bash
docker compose up --build
```

The containerized API is available at `http://localhost:8080`. PostgreSQL is exposed on `localhost:5432` for local tooling. Compose defaults are development-only values and must not be reused for production.

Compose applies migrations and seeds synthetic users. The current versioned authentication endpoints are:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/revoke`
- `GET /api/v1/auth/me`
- `GET /api/v1/auth/probes/healthcare`
- `GET /api/v1/auth/probes/banking-transfer`

The OpenAPI document advertises the local JWT bearer scheme for authenticated operations. The unversioned `/api/auth` routes are intentionally not registered.

## Repeatable verification

Run the full static and runtime gate:

```bash
./scripts/verify-learning-lab.sh
```

The script:

- restores tools/packages and performs a warning-free build;
- validates Compose, migrations, schemas, OpenAPI paths, and module tags;
- verifies representative `400`, `401`, `403`, `404`, `409`, `422`, and `429` responses;
- checks persistence across an API restart;
- checks correlation logs, application-cache miss/hit/invalidation, compression inclusion/exclusion, and `Retry-After`;
- creates length-safe identifiers with a unique run suffix;
- stops Compose services after completion while preserving the PostgreSQL volume.

The gate creates synthetic rows and restarts the API once. It never invokes `docker compose down -v`. Run it twice to confirm that preserved state does not cause duplicate-data failures:

```bash
./scripts/verify-learning-lab.sh
./scripts/verify-learning-lab.sh
```

Set `KEEP_STACK=1` to leave Compose services running after verification. Set `RUN_ID` only when a specific 1–20 character alphanumeric/hyphen suffix is needed.

For interactive requests, use `AspNetCoreDomainLab.http`. Its token and resource ID variables require explicit copy-paste so the file does not depend on one editor's response-handler syntax.

## Synthetic credentials

| User | Password | Roles |
| --- | --- | --- |
| `healthcare.demo` | `HealthDemo!123` | `healthcare.read`, `healthcare.write` |
| `industrial.demo` | `IndustrialDemo!123` | `industrial.operator`, `industrial.maintainer` |
| `logistics.demo` | `LogisticsDemo!123` | `logistics.read`, `logistics.dispatcher` |
| `banking.demo` | `BankingDemo!123` | `banking.read`, `banking.transfer`, `banking.risk` |
| `platform.admin` | `PlatformDemo!123` | `platform.admin` |

These credentials and the signing key are for this learning application only.

## Modules

Module boundaries, workflow endpoints, trade-offs, and failure modes are recorded in `Modules/README.md` and each module README. Healthcare is the primary vertical slice; industrial automation, logistics, and banking are comparative labs after that request flow is understood.
