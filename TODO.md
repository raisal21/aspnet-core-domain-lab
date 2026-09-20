# TODO: ASP.NET Core Domain Lab

## Foundation

- [x] Create a separate repository for the ASP.NET Core application.
- [x] Create one `net10.0` Web API project.
- [x] Make Controllers the primary endpoint style.
- [x] Record Minimal API as a comparison track only.
- [x] Record the four-domain scope and backend-only boundary.
- [x] Record the local JWT, PostgreSQL, Docker Compose, OpenAPI, and HTTP verification decisions.

## Review and cheatsheet

- [x] Review all 16 ASP.NET Core topics.
- [x] Mark core concepts, supporting features, and out-of-scope concepts.
- [x] Explain the difference between Controllers, Minimal API, Middleware, and Filters.
- [x] Explain request-pipeline placement and HTTP behavior as part of the API contract.
- [x] Record domain concepts not fully covered: audit trail, idempotency, concurrency, secrets, background processing, messaging, and deployment.
- [x] Audit the review against official documentation and the .NET target version.
- [x] Create the ASP.NET Core cheatsheet after the review.
- [x] Record trade-offs and common mistakes for each topic.
- [x] Match the cheatsheet to the theory review.

## Repository and scope decisions

- [x] Use `~/workspace/aspnet-core-domain-lab` as the application repository.
- [x] Use one Web API project with four modules.
- [x] Use Controllers as the primary endpoint style.
- [x] Use `Asp.Versioning.Mvc` with URL-segment API versioning.
- [x] Use one OpenAPI document with tags per module.
- [x] Add Swagger UI during implementation.
- [x] Use one PostgreSQL database with business-module schemas and a shared auth schema.
- [x] Use a complete local JWT issuer with shared identity and domain-specific policies.
- [x] Use domain-specific error response formats.
- [x] Keep caching, compression, and rate limiting policies independent per domain.
- [x] Use HTTP files and `curl` instead of an integration-test project.
- [x] Keep the simulation synthetic and non-production.

## Infrastructure skeleton

- [x] Add a multi-stage `Dockerfile` for the Web API.
- [x] Add `compose.yaml` for the API and PostgreSQL 17.
- [x] Add `.dockerignore` and `.env.example`.
- [x] Add module documentation for healthcare, industrial automation, logistics, and banking.
- [x] Verify `dotnet build --no-restore` with zero warnings.
- [x] Verify `docker compose config --quiet`.
- [x] Build the API image successfully.
- [x] Start the Compose stack and wait for PostgreSQL health.
- [x] Verify `GET /openapi/v1.json` returns HTTP 200 from the API container.
- [x] Stop the stack while preserving the local PostgreSQL volume.

## Persistence and shared infrastructure

- [x] Add Npgsql EF Core for PostgreSQL.
- [x] Decide to start with one schema-mapped context and keep module ownership explicit in table mappings.
- [x] Add shared connection-string configuration.
- [x] Add the schema-aware `DomainDbContext` boundary and schema constants.
- [x] Add the initial migration for business schemas and the auth schema.
- [x] Verify migration and schema creation against Compose PostgreSQL.
- [x] Add development seed data with no real PII or financial data.

## Authentication and authorization

- [x] Add the local JWT issuer.
- [x] Add login and credential validation with synthetic users.
- [x] Add refresh-token rotation.
- [x] Add refresh-token revocation.
- [x] Add signing-key configuration for development.
- [x] Add shared identity claims.
- [x] Add domain-specific roles and policies.
- [x] Verify authentication and authorization behavior through HTTP files and `curl`.

## API platform

- [x] Add Swagger UI.
- [x] Add URL-segment API versioning.
- [x] Add domain-specific validation behavior.
- [x] Add domain-specific error contracts.
- [x] Add logging behavior that is useful for workflow diagnosis.
- [x] Add independent caching policies where relevant.
- [x] Add independent response-compression policies where relevant.
- [x] Add independent rate-limiting policies where relevant.
- [x] Document the final OpenAPI contract by module tags.

## Complete workflows

- [x] Implement the healthcare workflows: patient/appointment, lab result review, and medical inventory.
- [x] Implement the industrial automation workflows: asset/device status, telemetry/alarm, maintenance work order, and device command.
- [x] Implement the logistics workflows: shipment tracking, warehouse inventory, delivery route/stop, and carrier integration boundary.
- [x] Implement the banking workflows: customer/account, payment/transfer, statement/history, and fraud/risk review.
- [x] Verify request and response behavior for every workflow.
- [x] Verify status codes and domain-specific error bodies.
- [x] Verify persistence and schema ownership for every workflow that uses data.
- [x] Verify logging and authorization behavior.
- [x] Document trade-offs and failure modes for every workflow.
- [x] Do not force all 16 topics into every workflow.
- [x] Do not expand the application into four production systems.

## Acceptance check

```bash
test -s TODO.md && rg -q "Infrastructure skeleton" TODO.md && rg -q "Complete workflows" TODO.md
```
