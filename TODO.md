# TODO: ASP.NET Core Domain Lab

This file records the completed implementation baseline and learning-quality hardening batch. Checked items record completed repository work; they do not by themselves prove learner mastery, so the current behavioral evidence is listed separately below.

## Completed implementation baseline

### Foundation

- [x] Create a separate repository for the ASP.NET Core application.
- [x] Create one `net10.0` Web API project.
- [x] Make Controllers the primary endpoint style.
- [x] Record Minimal API as a comparison topic only.
- [x] Record the four-domain scope and backend-only boundary.
- [x] Record the local JWT, PostgreSQL, Docker Compose, OpenAPI, and HTTP verification decisions.

### Review and cheatsheet

- [x] Review all 16 ASP.NET Core topics in `REVIEW-ASP.NET-CORE.md`.
- [x] Mark core concepts, supporting features, and out-of-scope concepts.
- [x] Explain the difference between Controllers, Minimal API, Middleware, and Filters.
- [x] Explain request-pipeline placement and HTTP behavior as part of the API contract.
- [x] Record adjacent domain concepts: audit trail, idempotency, concurrency, secrets, background processing, messaging, and deployment.
- [x] Audit the review against official documentation and the .NET target version.
- [x] Create the ASP.NET Core cheatsheet.
- [x] Record trade-offs and common mistakes for each topic.
- [x] Match the cheatsheet to the theory review.

### Repository and scope decisions

- [x] Use `~/workspace/aspnet-core-domain-lab` as the application repository.
- [x] Use one Web API project with four modules.
- [x] Use Controllers as the primary endpoint style.
- [x] Use `Asp.Versioning.Mvc` with URL-segment API versioning.
- [x] Use one OpenAPI document with tags per module.
- [x] Add Swagger UI.
- [x] Use one PostgreSQL database with business-module schemas and a shared auth schema.
- [x] Use a complete local JWT issuer with shared identity and domain-specific policies.
- [x] Use domain-specific error response formats for comparison.
- [x] Keep caching, compression, and rate-limiting policies independent per domain.
- [x] Use HTTP examples and `curl` instead of an integration-test project for the initial stage.
- [x] Keep the simulation synthetic and non-production.

### Infrastructure skeleton

- [x] Add a multi-stage `Dockerfile` for the Web API.
- [x] Add `compose.yaml` for the API and PostgreSQL 17.
- [x] Add `.dockerignore` and `.env.example`.
- [x] Add module documentation for healthcare, industrial automation, logistics, and banking.
- [x] Record a successful `dotnet build --no-restore` with zero warnings.
- [x] Record a successful `docker compose config --quiet` check.
- [x] Build the API image successfully.
- [x] Start the Compose stack and wait for PostgreSQL health.
- [x] Record HTTP 200 from `GET /openapi/v1.json` in the API container.
- [x] Stop the stack while preserving the local PostgreSQL volume.

### Persistence and shared infrastructure

- [x] Add Npgsql EF Core for PostgreSQL.
- [x] Start with one schema-mapped context and explicit module ownership.
- [x] Add shared connection-string configuration.
- [x] Add the schema-aware `DomainDbContext` boundary and schema constants.
- [x] Add migrations for business schemas and the auth schema.
- [x] Record successful schema creation against Compose PostgreSQL.
- [x] Add development seed data with no real PII or financial data.

### Authentication and authorization

- [x] Add the local JWT issuer.
- [x] Add login and credential validation with synthetic users.
- [x] Add refresh-token rotation.
- [x] Add refresh-token revocation.
- [x] Add signing-key configuration for development.
- [x] Add shared identity claims.
- [x] Add domain-specific roles and policies.
- [x] Record authentication and authorization behavior through HTTP requests.

### API platform

- [x] Add Swagger UI.
- [x] Add URL-segment API versioning.
- [x] Add domain-specific validation behavior.
- [x] Add domain-specific error contracts.
- [x] Add workflow-oriented structured logging.
- [x] Add independent caching policies where relevant.
- [x] Add independent response-compression policies where relevant.
- [x] Add independent rate-limiting policies where relevant.
- [x] Document the OpenAPI contract by module tags.

### Complete workflows

- [x] Implement healthcare workflows: patient/appointment, lab review, and medical inventory.
- [x] Implement industrial workflows: asset/status, telemetry/alarm, maintenance, and simulated command.
- [x] Implement logistics workflows: shipment, warehouse inventory, route/stop, and carrier boundary.
- [x] Implement banking workflows: customer/account, synthetic transfer, transaction history, and risk review.
- [x] Record request and response checks for every workflow family.
- [x] Record status-code and domain-error checks.
- [x] Record persistence and schema-ownership checks.
- [x] Record logging and authorization checks.
- [x] Document trade-offs and failure modes for every workflow family.
- [x] Avoid forcing all 16 topics into every workflow.
- [x] Avoid expanding the application into four production systems.

## Completed batch: learning-loop hardening

### Document roles and learning path

- [x] State in `README.md` that `TOUR.md` is the official guided learning path.
- [x] Keep `CHAPTERS.md` as the topic-to-source index rather than a second walkthrough.
- [x] Keep `PLAN.md` focused on scope, learning intent, and durable decisions.
- [x] Keep `HANDOFF.md` focused on temporary session state.
- [x] Give the theory review and cheatsheet distinct roles in the learning path.
- [x] Keep the original learning-loop batch usable before the theory files moved into the repository.

### Learning checkpoints

- [x] Add `predict -> run -> observe -> explain` checkpoints to the main host/request walkthrough.
- [x] Add a checkpoint distinguishing model validation from domain invariants.
- [x] Add a checkpoint distinguishing `401`, `403`, and successful authorization.
- [x] Add a checkpoint tracing one request from controller through service to PostgreSQL.
- [x] Add a checkpoint observing correlation logging.
- [x] Add a checkpoint observing cache miss, hit, and invalidation or expiry.
- [x] Add a checkpoint observing compression enabled versus excluded.
- [x] Add a checkpoint observing rate-limit rejection and `Retry-After`.
- [x] Keep code modification, API v2, filters, Minimal API, and concurrency experiments optional.

### Repeatable walkthrough and HTTP examples

- [x] Use length-safe unique suffixes instead of deleting the preserved PostgreSQL volume.
- [x] Verify the walkthrough twice against the same preserved PostgreSQL volume.
- [x] Define `accountId` before it is used in `AspNetCoreDomainLab.http`.
- [x] Document which token and resource variables require manual copy-paste.
- [x] Mark intentional failure requests with their expected status and purpose.
- [x] Cover important concepts and workflow families without requiring one HTTP request per controller action.
- [x] Verify that repeated walkthrough runs do not fail for unexplained state reasons.

### Verification gates

- [x] Define restore, warning-free build, Compose config, migration, and schema gates.
- [x] Define a host gate for OpenAPI and Swagger availability in Development.
- [x] Define an HTTP gate for representative success and failure responses.
- [x] Observe representative `400`, `401`, `403`, `404`, `409`, `422`, and `429` responses.
- [x] Define a state gate for schema ownership and persistence after restart.
- [x] Define an observation gate for correlation logs, application cache, compression, and rate limiting.
- [x] Label old session results as historical until rerun.
- [x] Record the environment and baseline commit for the final batch verification.
- [x] Replace the heading-only acceptance check with `scripts/verify-learning-lab.sh`.

### Handoff refresh

- [x] Remove the stale statement that the repository has no commits.
- [x] Record the current baseline commit and remote branch state.
- [x] Separate durable decisions from temporary Docker/volume state.
- [x] Distinguish current verification from historical verification.
- [x] Point the next concrete action to review/commit and begin the learning path rather than add a feature.
- [x] Keep account lockout, audit entities, API v2, and integration-test automation deferred.

## Completed batch: self-contained theory and quick reference

- [x] Move `REVIEW-ASP.NET-CORE.md` from the private vault into the repository.
- [x] Move `CHEATSHEET-ASP.NET-CORE.md` from the private vault into the repository.
- [x] Reframe the review as theory for this repository instead of the earlier external track.
- [x] Rewrite the cheatsheet as a scan-first lookup rather than a shortened review.
- [x] Include compact API, HTTP, validation, authorization, EF Core, caching, compression, rate-limit, OpenAPI, command, and diagnosis references.
- [x] Apply the Humanizer prose checks without changing code, commands, paths, or technical claims.
- [x] Remove machine-specific review and cheatsheet paths from repository documentation.
- [x] Update `README.md`, `PLAN.md`, `CRITICS.md`, `CHAPTERS.md`, and `HANDOFF.md` for the self-contained document set.

## Batch acceptance evidence

- [x] `TOUR.md` is the single official walkthrough.
- [x] The walkthrough and gate use unique state against a preserved PostgreSQL volume.
- [x] Required HTTP workflow variables are defined before use.
- [x] Main scenarios contain an expected observation and an explanation prompt.
- [x] Static, host, HTTP, state, and operational-observation gates have reproducible commands.
- [x] Verification notes identify their environment, baseline commit, and freshness.
- [x] `HANDOFF.md` contains no known stale Git claim.
- [x] The review and cheatsheet are repository-local and all local Markdown links resolve.
- [x] The review retains all 16 topics; the cheatsheet stays below 300 lines and 1,800 words.
- [x] No machine-specific Markdown path remains.
- [x] The batch adds no production-only infrastructure or real domain data.

Current full gate:

```bash
./scripts/verify-learning-lab.sh
```

The gate passed twice against the same preserved volume with run IDs `1789915358-67215` and `1789915396-69397`. Both runs used .NET SDK `10.0.300`, Docker `29.5.3`, Docker Compose `v5.1.4`, Python `3.12.3`, and baseline commit `6d217b0` plus the working-tree batch changes.

## Deferred optional exercises

These remain outside the completed batch:

- Controller versus Minimal API comparison snippet.
- Middleware versus filter comparison snippet.
- Temporary API v2 contract exercise.
- Optimistic-concurrency conflict exercise.
- Adding `global.json`.
- Creating an integration-test project.
- Adding account lockout or audit entities.
