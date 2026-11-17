# CLAUDE.md — Engineering rules for JobTrail

These rules apply to every change in this repo. Project direction and phases live in `PLAN.md`; this file is about **how** code is written.
If a rule here blocks a clearly better solution, say so and propose the change instead of silently breaking it.

## Repository setup (Phase 0)

- `global.json` pins the .NET 10 SDK.
- `Directory.Build.props` for all projects:
  - `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - `<AnalysisLevel>latest-recommended</AnalysisLevel>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`
- `Directory.Packages.props` — central package management; versions are set in one place only.
- `.editorconfig` at the root defines style and severities. `dotnet format --verify-no-changes` runs in CI.
- Angular: `strict: true` in tsconfig, angular-eslint + Prettier, lint and format check run in CI.

## C# style

- File-scoped namespaces; one public type per file.
- Classes are `sealed` unless designed for inheritance.
- `record` types for DTOs, commands, and value objects.
- `async` all the way down; every async method that does I/O takes a `CancellationToken`. Never `.Result`, `.Wait()`, or `async void` (except event handlers).
- No magic strings or numbers for anything meaningful; use constants, enums, or options.
- Prefer clear names over comments. Comments explain *why*, not *what*.
- Keep methods short and single-purpose; if a method needs a section comment, it probably wants to be split.

## Architecture rules

- Modules never reference another module's internals or its `DbContext`. Cross-module calls go through a public interface in that module's contracts.
- Domain logic lives in the domain/application layer, not in endpoints or EF configurations.
- API never returns EF entities; always map to response DTOs.
- Expected failures (not found, validation, forbidden, quota exceeded) use a `Result` type. Exceptions are for truly unexpected situations.
- External services (LLM providers, clock, email) are always behind interfaces so they can be faked in tests.

## API conventions

- Minimal APIs, grouped per module (`MapGroup`), with consistent route naming (`/api/applications/{id}`).
- All errors are returned as RFC 9457 `ProblemDetails`. A global exception handler turns unexpected errors into a 500 ProblemDetails without leaking internals.
- All request DTOs are validated; invalid input returns 400 with field-level errors.
- Authorization is **on by default** (fallback policy requires an authenticated user). Anonymous endpoints are explicitly marked.
- The current user's id always comes from the token, **never** from the request body or route.
- Pagination for any list that can grow.

## Data & EF Core

- PostgreSQL, one schema per module.
- Every user-owned entity is protected by a global query filter on `UserId`, plus integration tests proving isolation.
- Read queries use `AsNoTracking()` and project to DTOs with `Select`.
- No lazy loading. Avoid N+1 queries; check generated SQL for non-trivial queries.
- Migrations have descriptive names and are reviewed before being committed. Never edit an applied migration.
- All timestamps are UTC (`DateTimeOffset`). Time is read through `TimeProvider`, never `DateTime.Now`.

## Configuration, secrets, logging

- Settings via the Options pattern with `ValidateOnStart()`; the app fails fast on bad config.
- Secrets only from user-secrets locally and environment variables in production. Never committed, never logged.
- Structured logging with message templates (`logger.LogInformation("Application {ApplicationId} moved to {Status}", ...)`), never string interpolation.
- **No PII in logs** (emails, CV text, phone numbers).

## LLM code

- All prompts live in one place per feature, versioned, not scattered inline.
- Responses are requested as JSON and validated against a schema before use.
- Every LLM call logs provider, model, latency, token usage, and outcome (no prompt content containing user data).
- The app must keep working when the LLM is disabled or rate-limited.

## Angular conventions

- Standalone components, signals for state, `OnPush` change detection everywhere.
- Styling with Tailwind; behaviour (drag-drop, dialogs, overlays, menus) with Angular CDK. No third-party component library unless approved.
- Typed reactive forms; no `any` (lint rule enforced).
- Feature-based folders (`features/applications`, `features/auth`, ...), shared UI in `shared/`.
- HTTP access only through services; components do not call `HttpClient` directly.
- API base URL and environment values come from environment config, never hard-coded.
- Every user-visible action has loading, empty, and error states.
- Basic accessibility: labelled inputs, keyboard-usable dialogs and drag-drop alternatives, sufficient contrast.

## Visual design rules

- **Do not invent visual design.** All colors, typography, spacing, radii, and component styles come from `docs/design.md` and the Claude Design handoff (see PLAN.md section 8a). Handoff code may be in another stack; translate it to Angular + Tailwind, never paste it in as-is.
- Until `docs/design.md` exists, do not build real UI screens; scaffolding, services, guards, and interceptors are fine.
- **When work reaches the design checkpoint** (PLAN.md), stop and tell the author it is time to design in Claude Design on claude.ai. Give them a short design brief to paste into Claude Design: the screens needed, the data and actions on each screen (based on the API that actually exists at that point), and the required empty/loading/error states. Then wait for the Claude Design handoff command (or an exported folder/zip) before doing any UI work.
- Use Tailwind theme tokens only; no arbitrary values (`text-[#123456]`, `mt-[13px]`) and no inline styles.
- If a screen needs something the design doesn't cover, stop and ask rather than improvising a new style. Reusable patterns become shared components in `shared/ui`.
- Avoid generic AI-generated aesthetics: no decorative gradients, glassmorphism, emoji as icons, or oversized hero sections.

## Testing

- Test names: `Method_Scenario_ExpectedResult`. Arrange / Act / Assert structure.
- Domain logic: unit tests. Data access, auth, isolation, aggregates: integration tests with Testcontainers (real PostgreSQL).
- Every bug fix comes with a regression test that fails before the fix.
- Tests do not depend on each other or on execution order; no real network calls to LLM providers.

## Git workflow

- Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `ci:`).
- One logical change per commit; the build and tests pass on every commit.
- Work on a branch per feature/phase and merge into `main` via a pull request, even when working solo. Use merge or rebase (not squash) so small commits stay in history.
- Never commit secrets, `.env` files, or generated build output.
- **AI attribution stays on.** Keep Claude Code's default `Co-Authored-By` trailer on commits and the attribution line on PRs; the author wants AI assistance to be visible. Do not disable or strip it.
- PR descriptions state *what* changed and *why*, in plain language, with links to related ADRs when relevant.

## Documentation

- `README.md` stays current: what it is, how to run, architecture overview.
- `README.md` includes a short "How this was built" note: developed with AI assistance (Claude Code); architecture and technical decisions are the author's, and every change is reviewed by the author.
- Significant decisions are recorded as short ADRs in `docs/adr/NNNN-title.md` (context, decision, consequences).
- Public module contracts get XML doc comments.

## Definition of done for any change

1. Builds with zero warnings.
2. Tests added/updated and passing; lint and format checks pass.
3. No secrets, no PII in logs, user isolation preserved.
4. README / ADR updated if behaviour or architecture changed.
