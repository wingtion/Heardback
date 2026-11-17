# JobTrail

A web app for job seekers to track every application in one place and get insight from that data:
which channels actually get responses, how long companies take to reply, how well a CV matches a
posting, and community signals such as postings where nobody ever hears back.

> **Status:** Phase 0 (skeleton). See [PLAN.md](PLAN.md) for the roadmap and [CLAUDE.md](CLAUDE.md)
> for the engineering rules.

## Tech stack

- **Backend:** .NET 10, ASP.NET Core minimal APIs, EF Core, PostgreSQL 18
- **Frontend:** Angular 22 (standalone, signals, zoneless), Angular CDK, Tailwind CSS v4
- **Tests:** xUnit v3, Testcontainers (real Postgres), Vitest
- **CI:** GitHub Actions, Dependabot

## Running locally

Prerequisites: .NET 10 SDK, Node 22 (see `.nvmrc`), Docker.

```bash
# 1. Database
cp .env.example .env              # then set POSTGRES_PASSWORD
docker compose up -d

# 2. API on http://localhost:5080 (API reference at /scalar, health at /health)
dotnet run --project src/JobTrail.Api

# 3. Frontend on http://localhost:4200
cd web
npm ci
npm start
```

## Tests and checks

```bash
dotnet test --solution JobTrail.slnx            # unit, architecture and integration tests (needs Docker)
dotnet format JobTrail.slnx --verify-no-changes

cd web
npm test -- --watch=false
npm run lint
npm run format:check
```

## Architecture

JobTrail is a **modular monolith**: one deployable API, split into modules with hard boundaries
([ADR-0001](docs/adr/0001-modular-monolith.md)).

```
src/
  JobTrail.Api/                  host: DI, endpoints, middleware
  JobTrail.Modules.Identity/     users, invite codes, tokens
  JobTrail.Modules.Applications/ applications, status history, notes, reminders
  JobTrail.Modules.Postings/     shared job postings, dedup
  JobTrail.Modules.Matching/     CV text, skills, match results
  JobTrail.Modules.Insights/     personal stats, community aggregates
  JobTrail.Infrastructure.Llm/   LLM client abstraction and providers
  JobTrail.Shared/               common abstractions
tests/
  JobTrail.UnitTests/            unit and architecture tests
  JobTrail.IntegrationTests/     API and database tests (Testcontainers)
web/                             Angular app
```

- Each module exposes only its `Contracts` namespace and one registration class; everything else is
  `internal`. Architecture tests fail the build if a module uses another module's non-contract types.
- Each module will own its own Postgres schema; modules never read each other's tables.
- Errors are returned as RFC 9457 ProblemDetails; configuration is validated at startup.

## How this was built

JobTrail is developed with AI assistance (Claude Code). The architecture and technical decisions are
the author's, and every change is reviewed by the author before it is merged.
