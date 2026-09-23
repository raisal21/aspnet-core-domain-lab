# ASP.NET Core Domain Lab Plan

This is the application-specific plan for the ASP.NET Core learning lab. The broader historical C# plan remains outside this repository.

## Active goal

Use the completed backend-only `net10.0` Web API as a repeatable learning lab for 16 ASP.NET Core topics across four bounded domains:

- healthcare
- industrial automation
- logistics
- banking

The current baseline includes a hardened learning loop without expanding the application toward production scope.

## Status model

The repository uses three distinct statuses:

1. **Implementation available** — the code or documentation exists.
2. **Behavior verified** — a command or request observed the expected result in a named environment.
3. **Concept understood** — the learner can predict, observe, explain, and trace the behavior.

A checked implementation item does not by itself prove learning mastery. Historical verification remains useful, but it must not be presented as a fresh run.

## Current implementation baseline

- One `net10.0` Web API uses Controllers as the primary endpoint style.
- OpenAPI JSON is available at `/openapi/v1.json` in Development, with Swagger UI at `/swagger`.
- Auth and domain controllers use URL-segment API versioning under `/api/v1`.
- Docker Compose runs the API and PostgreSQL 17 locally.
- One schema-aware `DomainDbContext` maps auth and four business schemas.
- Local JWT authentication includes synthetic users, access tokens, refresh-token rotation/revocation, claims, roles, and policies.
- Healthcare, industrial automation, logistics, and banking workflows are implemented.
- Validation, domain error contracts, request diagnostics, memory caching, response compression, output caching, and named rate limits are implemented.
- `REVIEW-ASP.NET-CORE.md` and `CHEATSHEET-ASP.NET-CORE.md` keep the 16-topic theory and quick reference inside the repository.
- `TOUR.md` provides repeatable learning checkpoints and `scripts/verify-learning-lab.sh` provides the current behavioral gate.
- Historical session claims remain labeled separately from fresh verification.

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

Not every topic requires a dedicated implementation exercise. Minimal API and Filters may be learned through comparison, snippets, and decision questions because the domain application intentionally uses Controllers and no custom MVC filters.

## Official learning path

The repository documents have separate roles:

1. `README.md` — purpose, boundaries, prerequisites, and run commands.
2. `REVIEW-ASP.NET-CORE.md` — theory, trade-offs, and source references for all 16 topics.
3. `CHEATSHEET-ASP.NET-CORE.md` — scan-first API, contract, command, and diagnosis reference.
4. `TOUR.md` — official guided learning path through runtime and source.
5. `CHAPTERS.md` — index from each topic to its implementation points.
6. Module README files — domain workflow, trade-off, and failure-mode notes.
7. `TODO.md` — completed implementation and learning-quality work record.
8. `HANDOFF.md` — temporary session state, not the durable architecture source.

Healthcare is the primary vertical slice. Industrial automation, logistics, and banking are comparative labs after the learner understands one request from HTTP through persistence.

## Learning checkpoint model

Key scenarios use this lightweight loop:

1. **Predict** — state the expected status, response body, header, log, or data change.
2. **Run** — execute the documented request or command.
3. **Observe** — capture the relevant result without requiring committed runtime output.
4. **Explain** — connect the result to the controller, middleware, policy, service, or persistence mapping.

A code modification or break/fix experiment is optional unless a specific exercise calls for it.

The track may use these flexible navigation labels:

- **Core:** host, controllers, HTTP, middleware, validation, errors, logging, basic authentication/authorization, basic EF Core, and OpenAPI.
- **Applied:** policies, migrations, caching, compression, rate limiting, and transactions.
- **Advanced optional:** refresh-token reuse, optimistic concurrency, multi-schema mapping, and idempotency.

These labels guide reading order; they are not formal graduation levels.

## Architecture decisions

- Use one Web API project with physical folders and namespaces for the four modules.
- Use Controllers for domain endpoints. Minimal API remains a comparison topic.
- Use `Asp.Versioning.Mvc` with URL-segment versioning.
- Use one OpenAPI document with tags per module and Swagger UI.
- Use one PostgreSQL database with one schema per business module and a shared auth schema.
- Use a local JWT issuer with synthetic users for this learning environment.
- Share identity data through the auth schema; keep roles and policies domain-specific.
- Keep error response formats intentionally different per domain for comparison.
- Keep caching, response compression, and rate-limiting policies independent per domain.
- Verify behavior with HTTP examples, `curl`, and the shell verification gate; an integration-test project is not required at this stage.
- Run API and PostgreSQL through Docker Compose for local development.

## Bounded cases

### Healthcare — primary vertical slice

Patient and appointment, lab result review, and medical inventory.

Focus: request flow, validation, authorization, errors, logging, EF Core, and documentation.

### Industrial automation — comparative lab

Asset and device status, telemetry and alarms, maintenance work orders, and simulated device commands.

Focus: middleware, logging, caching, rate limiting, response compression, and failure handling.

### Logistics — comparative lab

Shipment tracking, warehouse inventory, delivery routes and stops, and a synthetic carrier integration boundary.

Focus: REST, versioning, EF Core, caching, rate limiting, idempotency, and integration boundaries.

### Banking — comparative lab

Customer and account, synthetic transfer, transaction history, and risk review.

Focus: authentication, authorization, validation, errors, transactions, optimistic concurrency, and sensitive-response policy.

Each case remains a simulation. Features are used where they clarify a concept, not to imitate a production system.

## Completed implementation history

1. Add Npgsql EF Core and define persistence boundaries. (complete)
2. Create shared database configuration and a schema-aware `DomainDbContext`. (complete)
3. Add migrations and verify schema creation against PostgreSQL. (complete; historical verification)
4. Add the local JWT issuer, shared identity model, refresh-token rotation, and revocation. (complete)
5. Add validation, domain-specific error contracts, and shared host policies. (complete)
6. Add Swagger UI and version-aware API documentation. (complete)
7. Implement the four workflows one domain at a time. (complete)
8. Verify HTTP behavior, persistence, logging, authorization, trade-offs, and failure modes. (complete; original evidence is historical)
9. Harden the learning loop with repeatable checkpoints and a behavioral verification gate. (complete; current evidence below)

## Completed learning-loop hardening batch

The batch remained documentation- and verification-focused:

1. Clarified the roles of `PLAN.md`, `TODO.md`, `TOUR.md`, `CHAPTERS.md`, and `HANDOFF.md`.
2. Added `predict -> run -> observe -> explain` checkpoints to the main walkthrough scenarios.
3. Made the walkthrough repeatable with length-safe unique suffixes while preserving the PostgreSQL volume.
4. Defined every token and resource variable in `AspNetCoreDomainLab.http` and documented manual copy steps.
5. Kept HTTP coverage concept-focused rather than requiring one request per controller action.
6. Replaced heading-only acceptance with static, migration/schema, host, HTTP, state, and operational-observation gates.
7. Distinguished fresh verification from historical session claims.
8. Refreshed Git and session facts in `HANDOFF.md`.
9. Added no product feature or production infrastructure.

## Learning-loop verification result

- `TOUR.md` is the official learning path and contains nine explicit checkpoints.
- Unique per-run identifiers allow repeated use against a preserved PostgreSQL volume.
- `AspNetCoreDomainLab.http` has no undefined variable and separates tokens by domain persona.
- `scripts/verify-learning-lab.sh` checks build, Compose, four migrations, five schemas, OpenAPI, representative HTTP failures, persistence after restart, correlation logs, cache behavior, compression, rate limiting, and unexpected failures.
- The full gate passed twice against the same preserved volume with run IDs `1789915358-67215` and `1789915396-69397` on baseline commit `6d217b0` plus the working-tree learning-quality changes.
- Both runs built with zero warnings and zero errors, observed `400/401/403/404/409/422/429`, and stopped Compose without deleting the PostgreSQL volume.
- No production-only infrastructure or real domain data was introduced.

## Completed self-contained documentation batch

- Moved the theory review and cheatsheet from the private vault into the repository.
- Reframed the review as repository-local theory for the implemented lab.
- Rebuilt the cheatsheet as a scan-first reference with compact tables, commands, runtime signals, and diagnosis paths.
- Removed machine-specific paths and private-material dependencies from repository documentation.

## Optional follow-ups, not batch requirements

- Add a comparison snippet for Controller versus Minimal API.
- Add a comparison snippet for middleware versus filters.
- Try a temporary v2 contract exercise.
- Try an optimistic-concurrency conflict exercise.
- Pin a verified .NET SDK feature band with `global.json`.
- Add integration-test automation after the manual learning loop is stable.

## Open decisions

- Whether a future optional exercise should move one module from the shared `DomainDbContext` to its own context.
- Whether later auth exercises should cover account lockout or audit records.

Neither decision blocks use of the current learning baseline.

## Non-goals

- Four production-grade systems or deployments.
- A formal course platform or grading system.
- A frontend.
- Real PII, real money, real device actuation, compliance claims, or production SLA claims.
- Mandatory runnable experiments for every one of the 16 topics.
- Mandatory API v2, concurrency, Minimal API, or custom-filter implementation.
- Full endpoint coverage in the HTTP file.
- External identity providers, distributed caching, messaging, Kubernetes, or production observability.
- A new integration-test project for the current learning baseline.

## References

- `REVIEW-ASP.NET-CORE.md`: theory and official references for the 16 topics.
- `CHEATSHEET-ASP.NET-CORE.md`: quick reference for implementation and diagnosis.
- `CRITICS.md`: calibrated learning-quality review and scope boundaries.
- `TODO.md`: completed implementation and learning-quality checklist.
- `TOUR.md`: official learning walkthrough.
- `CHAPTERS.md`: 16-topic source index.
- `README.md`: run commands and synthetic credentials.
