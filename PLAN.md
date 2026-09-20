# ASP.NET Core Domain Lab Plan

This is the application-specific copy of the ASP.NET Core plan. The broader C# playground plan remains at `/home/raisal/workspace/csharp-playground/PLAN.md`.

## Active goal

Build a backend-only ASP.NET Core learning application that compares four bounded domains in one Web API project:

- healthcare
- industrial automation
- logistics
- banking

This is a realistic simulation for learning, not a production system.

## Current state

- Repository: `~/workspace/aspnet-core-domain-lab`.
- Project: one `net10.0` Web API using `Microsoft.NET.Sdk.Web`.
- Controllers are the primary endpoint style.
- OpenAPI JSON works at `/openapi/v1.json` in Development, and Swagger UI is available at `/swagger`.
- Auth controllers use URL-segment API versioning under `/api/v1`.
- Docker Compose runs the API and PostgreSQL 17 locally.
- Docker and local runtime smoke checks pass.
- EF Core persistence is wired through one schema-aware `DomainDbContext`.
- Migrations create the healthcare, industrial, logistics, banking, and auth schemas plus all workflow tables.
- Local JWT auth, refresh-token rotation/revocation, shared claims, domain policies, and synthetic development users are implemented.
- Healthcare, industrial automation, logistics, and banking workflow entities, controllers, services, and domain documentation are implemented.
- Shared validation, domain error contracts, request diagnostics, memory caching, response compression, and named rate limits are implemented.

## Learning track

The track covers 16 topics:

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

The track uses theory, a cheatsheet, complete API workflows, request/response verification, and trade-off analysis. It does not force every topic into every domain.

## Architecture decisions

- Use one Web API project with physical folders and namespaces for the four modules.
- Use Controllers for domain endpoints. Minimal API is a comparison track only.
- Use `Asp.Versioning.Mvc` with URL-segment versioning.
- Use one OpenAPI document with tags per module and add Swagger UI.
- Use one PostgreSQL database with one schema per business module and a shared auth schema.
- Use a local JWT issuer with login, refresh-token rotation, revocation, claims, and policy support.
- Share identity data through the auth schema; keep roles and policies domain-specific.
- Keep error response formats intentionally different per domain.
- Keep caching, response compression, and rate limiting policies independent per domain.
- Verify behavior with HTTP files and `curl`; do not create an integration-test project at this stage.
- Run API and PostgreSQL through Docker Compose for local development.

## Bounded cases

### Healthcare

Patient and appointment, lab result review, and medical inventory.

Focus: validation, authorization, errors, logging, EF Core, and documentation.

### Industrial automation

Asset and device status, telemetry and alarms, maintenance work orders, and device commands.

Focus: middleware, logging, rate limiting, response compression, EF Core, and failure handling.

### Logistics

Shipment tracking, warehouse inventory, delivery routes and stops, and the carrier integration boundary.

Focus: REST, versioning, EF Core, caching, rate limiting, and integration boundaries.

### Banking

Customer and account, payment and transfer, statement and transaction history, and fraud/risk review.

Focus: authentication, authorization, validation, errors, EF Core, logging, and transaction boundaries.

Each bounded case gets a complete workflow. Irrelevant features are not forced into a domain. Minimal API is intentionally not used as a domain endpoint.

## Implementation order

1. Add Npgsql EF Core and define persistence boundaries. (complete)
2. Create shared database configuration and a schema-aware `DomainDbContext`. (complete)
3. Add migrations and verify schema creation against PostgreSQL. (complete)
4. Add the local JWT issuer, shared identity model, refresh-token rotation, and revocation. (complete)
5. Add validation, domain-specific error contracts, and shared host policies. (complete)
6. Add Swagger UI and version-aware API documentation. (complete)
7. Implement the four complete workflows one domain at a time. (complete)
8. Verify HTTP behavior, persistence, logging, authorization, trade-offs, and failure modes. (complete)

## Open decisions

- The workflow endpoint contracts are now documented in each module README and the HTTP request file.
- Whether a module should move from the initial shared context to its own DbContext as workflows grow.
- Whether the synthetic auth store should gain account-lockout and audit entities as the workflows grow.

## Non-goals

- Four production-grade systems.
- A frontend.
- Real PII, real money, real device actuation, compliance claims, or production SLA claims.
- A forced use of all 16 topics in every domain.
- A new integration-test project during the current learning stage.

## References

- Global source plan: `/home/raisal/workspace/csharp-playground/PLAN.md`.
- Global source checklist: `/home/raisal/workspace/csharp-playground/TODO.md`.
- Theory review: `/mnt/c/Users/PC-Windows/Documents/wsl-vault/REVIEW-ASP.NET-CORE.md`.
- Cheatsheet: `/mnt/c/Users/PC-Windows/Documents/wsl-vault/CHEATSHEET-ASP.NET-CORE.md`.
