# Heardback

A web app for job seekers to track every application in one place and get insight from that data:
which channels actually get responses, how long companies take to reply, how well a CV matches a
posting, and community signals such as postings where nobody ever hears back.

> **Status:** Phases 0–1 and the design checkpoint are done (design system in
> [docs/design.md](docs/design.md)); Phase 2 (application tracking) is in progress. See
> [PLAN.md](PLAN.md) for the roadmap and [CLAUDE.md](CLAUDE.md) for the engineering rules.

## Tech stack

- **Backend:** .NET 10, ASP.NET Core minimal APIs, EF Core, PostgreSQL 18
- **Frontend:** Angular 22 (standalone, signals, zoneless), Angular CDK, Tailwind CSS v4
- **Tests:** xUnit v3, Testcontainers (real Postgres), Vitest
- **CI:** GitHub Actions, Dependabot

## Running locally

Prerequisites: .NET 10 SDK, Node 22 (see `.nvmrc`), Docker.

### One-time setup

```bash
# Database credentials for docker compose
cp .env.example .env              # then set POSTGRES_PASSWORD

# API secrets (stored outside the repo with dotnet user-secrets)
cd src/Heardback.Api
dotnet user-secrets set "Database:ConnectionString" \
  "Host=localhost;Port=5432;Database=heardback;Username=heardback;Password=<POSTGRES_PASSWORD>"
dotnet user-secrets set "Identity:Tokens:SigningKey" "$(openssl rand -base64 48)"
# 12 characters, the same shape as an invite code (hex digits are all valid invite characters)
dotnet user-secrets set "Identity:Invites:BootstrapAdminCode" "$(openssl rand -hex 6)"
cd ../..
```

### Run

```bash
docker compose up -d                       # Postgres 18
dotnet run --project src/Heardback.Api      # API on http://localhost:5080 (Scalar at /scalar); migrates on start
cd web && npm ci && npm start              # app on http://localhost:4200, proxies /api to the API
```

### First admin

Registration needs an invite code. The very first account uses the bootstrap code from user-secrets
(`dotnet user-secrets list --project src/Heardback.Api | grep BootstrapAdminCode`): open
`http://localhost:4200/register`, enter it as the invite code, and that account becomes the admin. After
that the bootstrap code stops working; remove the setting. The admin creates invite codes for everyone
else on the **Invite codes** page (`/admin/invite-codes`).

The bootstrap code must look like an invite code: 12 characters from 0-9 and A-Z without I, L, O or U
(dashes are fine). The API refuses to start otherwise, with a message that says how to generate one.

### Adding a database migration

```powershell
./scripts/Add-Migration.ps1 -Module Identity -Name DescriptiveName
```

The script runs `dotnet ef` and makes the generated classes `internal`, so modules keep exposing only
their contracts. Review the migration before committing, and never edit one that has been applied. A
test fails if a module's model has changes that no migration captures yet.

## Architecture

Heardback is a **modular monolith**: one deployable API, split into modules with hard boundaries
([ADR-0001](docs/adr/0001-modular-monolith.md)).

```
src/
  Heardback.Api/                  host: DI, middleware, maps module endpoints
  Heardback.Modules.Identity/     users, invite codes, sessions
  Heardback.Modules.Applications/ applications, status history, notes, reminders
  Heardback.Modules.Postings/     shared job postings, dedup
  Heardback.Modules.Matching/     CV text, skills, match results
  Heardback.Modules.Insights/     personal stats, community aggregates
  Heardback.Infrastructure.Llm/   LLM client abstraction and providers
  Heardback.Shared/               Result type, current user, paging, module DbContext base, validation
tests/
  Heardback.UnitTests/            unit and architecture tests
  Heardback.IntegrationTests/     API and database tests (Testcontainers)
web/                             Angular app
docs/adr/                        architecture decision records
docs/design.md                   design system: tokens, type, components, screen states
```

- **Module boundaries:** each module exposes only its `Contracts` namespace and one registration class;
  everything else is `internal`. Architecture tests fail the build if a module uses another module's
  non-contract types.
- **Data:** each module owns its own Postgres schema (snake_case) and never reads another module's tables.
- **Per-user isolation:** module DbContexts filter every user-owned entity to the current user and
  refuse writes to other users' rows. Architecture tests catch entities that skip this.
- **Auth** ([ADR-0002](docs/adr/0002-authentication-and-sessions.md)): invite-only registration,
  15-minute JWT access tokens kept in memory, and rotating refresh tokens in an httpOnly cookie.
  Sessions have an absolute 60-day limit, reusing a rotated token signs the user out everywhere, and
  the auth endpoints are rate limited per IP. Every endpoint requires sign-in unless marked anonymous.
- **Shared postings** ([ADR-0004](docs/adr/0004-shared-job-postings.md)): the same job is recognised
  across users by its normalized link or a hash of its content, safely under concurrent submissions.
  Postings are never edited and hold no user id; each application keeps the user's own copy of the
  details. The server never fetches posting pages.
- **Applications:** each application has a timeline of dated events; its status is the latest status
  event, and "No response · N days" is derived from dates on every read. Edits carry the row version and
  fail with 409 when the application changed elsewhere. Writes are rate limited per user. The web app
  shows applications as a list or as a board; a board move is a status event dated today, made by
  dragging a card or with "Move to…" (keyboard, touch and screen reader users).
- **CVs** ([ADR-0006](docs/adr/0006-cv-upload-and-text-extraction.md)): a user uploads one CV PDF. The
  server reads its text in memory under strict limits (size, pages, a decompression budget, an hourly upload
  limit), replaces emails, phone numbers, links and address lines with markers, and stores only that text;
  the file itself is never stored.
- **Errors and validation** ([ADR-0003](docs/adr/0003-request-validation.md)): RFC 9457 ProblemDetails
  everywhere, 400 with field-level errors for invalid requests, and configuration validated at startup.

## How this was built

Heardback is developed with AI assistance (Claude Code). The architecture and technical decisions are
the author's, and every change is reviewed by the author before it is merged.
