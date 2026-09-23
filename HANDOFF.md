---
session_id: 2026-09-20-learning-loop
branch: main
baseline_commit: 6d217b02706d5d43cd8ea0cb7323682dcc1eb276
working_tree_clean: false
date: 2026-09-20T22:07:31+07:00
---

## Active goal

Use the completed backend-only ASP.NET Core `net10.0` application as a repeatable, self-contained learning lab. The current work keeps theory, quick reference, guided practice, and implementation inside one repository without adding production scope.

Durable scope, architecture decisions, and non-goals live in `PLAN.md`. This file records temporary session state and the latest verification evidence.

## Where we left off

The four learning-quality priorities from `CRITICS.md` are implemented in the working tree:

1. `TOUR.md` is the official learning path and now uses nine `predict -> run -> observe -> explain` checkpoints.
2. `scripts/verify-learning-lab.sh` replaces the old heading-only acceptance check with static and behavioral gates.
3. `TOUR.md` and `AspNetCoreDomainLab.http` use per-run identifiers; every HTTP token/resource variable is defined and its copy step is explicit.
4. This handoff now reflects the actual Git baseline, runtime evidence, and next action.
5. `REVIEW-ASP.NET-CORE.md` and `CHEATSHEET-ASP.NET-CORE.md` moved from the private vault into the repository. The review now introduces this lab directly; the cheatsheet was rebuilt as a compact lookup and diagnosis aid.

`RequestDiagnosticsMiddleware` also validates client correlation IDs and includes the accepted ID directly in start/completion log messages. This makes the existing correlation behavior observable with the default Compose console logger.

The changes are not committed. Current baseline `HEAD` and `origin/main` both point to:

```text
6d217b02706d5d43cd8ea0cb7323682dcc1eb276
feat: add ASP.NET Core domain lab
```

## Fresh verification evidence

Environment:

- .NET SDK `10.0.300`
- Docker `29.5.3`
- Docker Compose `v5.1.4`
- Python `3.12.3`
- baseline commit `6d217b0` plus the current working-tree changes

Command executed twice against the same preserved PostgreSQL volume:

```bash
./scripts/verify-learning-lab.sh
./scripts/verify-learning-lab.sh
```

Passing run IDs:

- `1789915358-67215`
- `1789915396-69397`

Both runs verified:

- tool/package restore and a build with zero warnings and zero errors;
- valid Compose configuration;
- all four migrations present and no pending migration;
- non-empty `auth`, `healthcare`, `industrial`, `logistics`, and `banking` schemas;
- OpenAPI/Swagger plus representative paths and all five module tags;
- permitted `200`, unauthenticated `401`, wrong-role `403`, validation `400`, missing resource `404`, duplicate `409`, domain invariant `422`, and rate-limit `429` with `Retry-After: 5`;
- patient persistence across an API restart;
- correlation ID in the matching request-completion log;
- application memory-cache miss, hit, invalidation, and subsequent miss;
- gzip on industrial telemetry and no compression on banking responses;
- no unhandled failure in the bounded verification log window.

The second run passed without deleting the first run's rows, proving the unique-suffix repeatability strategy against preserved state.

After moving and revising the theory documents, static documentation verification also passed:

- all local Markdown links resolved across 14 Markdown files;
- the review retained all 16 numbered topics;
- the cheatsheet is 255 lines and 1,602 words, with scan-first tables and commands;
- no machine-specific Markdown path remained;
- the Humanizer strong-pattern scan found no staged opener, decorative dash, chatbot residue, or `not-only` construction in the review and cheatsheet;
- `git diff --check`, whitespace, code-fence, build, and Compose configuration checks passed.

The runtime gate was not rerun for this documentation-only follow-up. Its two passing runs still cover the unchanged application behavior.

## Temporary local state

- Compose services are stopped.
- The Docker volume `aspnet-core-domain-lab_postgres-data` remains.
- The volume contains only synthetic seed and verification data from current and earlier walkthroughs.
- The verification script creates synthetic rows and restarts the API once per run.
- The script uses `docker compose down`, never `docker compose down -v`.
- Access and refresh tokens printed during interactive use must not be committed.

## Next concrete action

Review the working-tree diff, then commit the learning-loop and self-contained-documentation batches. After commit, use `CHEATSHEET-ASP.NET-CORE.md` during a learning session that starts at `TOUR.md` section 0.

## Durable decisions referenced this session

- Healthcare remains the primary vertical slice; industrial automation, logistics, and banking remain comparative labs.
- Controllers remain the domain endpoint style; Minimal API and custom filters remain conceptual/optional comparisons.
- Verification stays shell/HTTP based for this stage; no integration-test project was added.
- Repeatability uses length-safe unique identifiers and preserves the local PostgreSQL volume.
- HTTP examples remain client-portable by using explicit manual token/ID variables rather than editor-specific response handlers.
- The theory review and cheatsheet are tracked repository documents. The review explains; the cheatsheet supports lookup; `TOUR.md` remains the official guided path.

## Known limitations and deferred work

- The verification gate proves application memory-cache behavior. It does not claim an authenticated output-cache hit.
- `AspNetCoreDomainLab.http` requires manual copy-paste for tokens and returned IDs.
- The shell verification gate assumes Bash, Docker Compose, `curl`, and Python 3.
- API v2, custom filters, Minimal API endpoints, forced concurrency conflicts, account lockout, audit entities, `global.json`, and an integration-test project remain optional follow-ups.

## References

- `REVIEW-ASP.NET-CORE.md`: repository-local theory and official source list.
- `CHEATSHEET-ASP.NET-CORE.md`: scan-first API, contract, command, and diagnosis reference.
- `CRITICS.md`: calibrated review and the four implemented priorities.
- `PLAN.md`: durable scope, status model, architecture decisions, and completed batches.
- `TODO.md`: implementation and learning-quality completion record.
- `TOUR.md`: official learning path and checkpoints.
- `CHAPTERS.md`: 16-topic source index.
- `README.md`: prerequisites, run commands, document roles, and verification entry point.
- `scripts/verify-learning-lab.sh`: reproducible static/runtime gate.
- `AspNetCoreDomainLab.http`: concept-focused interactive request examples.
