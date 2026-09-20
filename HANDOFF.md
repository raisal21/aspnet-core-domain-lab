---
session_id: 2026-09-19-01
parent_session_id: 2026-05-18-02
branch: main
commits_since_parent: 0
date: 2026-09-19T21:50:30+07:00
---

## Active goal

Continue building `aspnet-core-domain-lab` as a backend-only ASP.NET Core `net10.0` learning application. The repository now has the verified API/PostgreSQL Docker boundary, schema-aware EF Core persistence, local JWT authentication, API platform behavior, and all four bounded-domain workflows.

## Where we left off

All implementation and verification sections in `TODO.md` are complete. `Infrastructure/Authentication/` contains synthetic users, password hashing, JWT access tokens, refresh-token rotation/reuse detection/revocation, shared claims, and domain policies. The workflow migration `20260919182752_AddDomainWorkflows.cs` creates 19 business tables across the four owning schemas. Healthcare, industrial automation, logistics, and banking each have versioned controllers, DTOs, services, persistence mappings, authorization policies, domain errors, logging, and module documentation. API platform behavior includes validation contracts, request correlation diagnostics, memory caching, domain-specific response compression, named rate limits, OpenAPI tags/security, and Swagger UI. Compose migration and HTTP verification passed for every workflow, including authorization failures, domain errors, cache hit/miss logging, compression, rate limits, persistence after restart, and disposable migration rollback/reapply. The stack is stopped with `docker compose down`; the PostgreSQL volume remains. Restart it with `docker compose up -d`.

## Next concrete action

No unchecked implementation item remains in `TODO.md`. If extending the lab, choose a deliberate non-goal follow-up such as account-lockout/audit entities, a second API version, or integration-test automation; do not introduce real PII, money, or device actuation.

## Open questions

- Should a module move from the initial shared `DomainDbContext` to its own context as workflows grow?

## Decisions made this session

- 2026-09-19: The app-specific `PLAN.md` and `TODO.md` were created in the new repository; the broader source files in `csharp-playground` were preserved.
- 2026-09-19: The local boundary uses one API container and one PostgreSQL 17 container managed by Docker Compose.
- 2026-09-19: Compose uses development-only defaults, a persistent PostgreSQL volume, and a health-gated API startup.
- 2026-09-19: The Docker image uses .NET 10 SDK/runtime images and runs the final app as the non-root app user.
- 2026-09-19: The template OpenAPI package was upgraded from `10.0.8` to `10.0.12` because the original transitive `Microsoft.OpenApi 2.0.0` triggered `NU1903`.
- 2026-09-19: Persistence starts with one `DomainDbContext`; module tables must map explicitly to `healthcare`, `industrial`, `logistics`, `banking`, or `auth` rather than relying on a default schema.
- 2026-09-19: EF migration history stays in `public`; `Database:ApplyMigrations` is opt-in and Compose enables it for local development.
- 2026-09-19: Local password-only PostgreSQL connection strings disable GSS encryption to avoid a non-fatal Kerberos library warning in the minimal .NET container.
- 2026-09-19: Authentication uses one shared auth schema with `users`, `user_roles`, and hashed `refresh_tokens`; access tokens use HS256 and the development signing key comes from `Jwt:SigningKey`, overridable by Compose `JWT_SIGNING_KEY`.
- 2026-09-19: Refresh-token reuse revokes all active refresh tokens for that user; explicit revocation is scoped to the authenticated user's presented token.
- 2026-09-19: Refresh-token rows use an optimistic concurrency version so simultaneous refresh requests cannot both issue successors.
- 2026-09-20: `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` 8.1.0 provide URL-segment versioning; auth routes use `/api/v1/auth/...` and report supported versions.
- 2026-09-20: `Swashbuckle.AspNetCore.SwaggerUI` 10.0.1 serves `/swagger` over the built-in `/openapi/v1.json` document; a built-in OpenAPI transformer advertises the local JWT bearer scheme only on authorized operations.
- 2026-09-20: Domain workflows use one shared EF context with explicit schema ownership; domain errors intentionally differ by module, while validation is selected from the URL segment.
- 2026-09-20: Logistics and industrial telemetry use short-lived in-memory caches; industrial commands, logistics dispatch, and banking transfers use independent fixed-window rate limits; banking responses opt out of compression.

## Dragons

- Docker base-image download is slow on the current network; the first image build exceeded 120 seconds, but the retry succeeded.
- The repository has no commits yet.
- The OpenAPI document has Auth, Healthcare, Industrial, Logistics, and Banking tags; future controllers must preserve those module boundaries.
- The PostgreSQL volume contains synthetic verification records and refresh tokens from the curl walkthroughs; remove the volume only when a clean local reset is desired.
- Issued access JWTs intentionally remain valid until their short expiry after refresh-token revocation; there is no token blacklist.
- Compose emits the expected development-only ASP.NET Data Protection key persistence warnings because the container has no persistent key volume.

## References

- `PLAN.md`: application-specific architecture and implementation order.
- `TODO.md`: all implementation, workflow, verification, and non-goal checklist items are complete.
- `README.md`: local and Docker run commands.
- `compose.yaml`: API/PostgreSQL local boundary.
- `Dockerfile`: container build pipeline.
- `/home/raisal/workspace/csharp-playground/PLAN.md`: broader historical C# plan.
- `/home/raisal/workspace/csharp-playground/TODO.md`: broader historical C# checklist.
- `napkin.md`: no project-specific file exists.
- `cavemem query`: not used for this session.
