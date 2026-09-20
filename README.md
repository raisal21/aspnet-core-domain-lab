# ASP.NET Core Domain Lab

Backend-only ASP.NET Core learning application for comparing four bounded domains:

- healthcare
- industrial automation
- logistics
- banking

This is a realistic simulation for learning, not a production system. It must not use real PII, money, device actuation, compliance claims, or production SLA claims.

## Current State

The repository contains the `net10.0` Web API, a local Docker Compose boundary for the API and PostgreSQL, schema-aware EF Core persistence, a local JWT issuer, URL-segment API versioning, Swagger UI, and complete synthetic workflows for all four domains.

The application is intentionally one Web API project. Controllers are the primary endpoint style. Minimal API is a comparison track, not the implementation style for domain endpoints.

## Planned Shape

- one Web API project
- one PostgreSQL database with a schema per business module and a shared auth schema
- one schema-aware `DomainDbContext` with migrations for auth and all four workflow schemas
- local JWT issuer with refresh-token rotation, reuse detection, and revocation
- synthetic development users and domain-specific authorization policies
- one OpenAPI document tagged by module
- Swagger UI at `/swagger` in Development, backed by `/openapi/v1.json`
- URL-segment API versioning with the current contract under `/api/v1`
- domain-specific validation/errors, request correlation logging, caching, compression, and rate limits
- HTTP files and `curl` for request verification
- Docker Compose for the local API and PostgreSQL environment

Each bounded case gets a complete workflow, but irrelevant ASP.NET Core features are not forced into every domain.

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

Synthetic development credentials:

| User | Password | Roles |
| --- | --- | --- |
| `healthcare.demo` | `HealthDemo!123` | `healthcare.read`, `healthcare.write` |
| `industrial.demo` | `IndustrialDemo!123` | `industrial.operator`, `industrial.maintainer` |
| `logistics.demo` | `LogisticsDemo!123` | `logistics.read`, `logistics.dispatcher` |
| `banking.demo` | `BankingDemo!123` | `banking.read`, `banking.transfer`, `banking.risk` |
| `platform.admin` | `PlatformDemo!123` | `platform.admin` |

These credentials and the signing key are for this learning application only.

## Modules

Module boundaries, workflow endpoints, trade-offs, and failure modes are recorded in `Modules/README.md` and each module README. The complete 16-chapter code map is in `CHAPTERS.md`; inline source comments use `Bab N` markers. Follow `TOUR.md` for a start-to-finish repository reading and runtime walkthrough.
