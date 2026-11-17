# Heardback — Project Plan

> Named after the question every job seeker asks: "Did I hear back?" (Renamed from the working name
> JobTrail, which an existing job tracker at jobtrail.dev already uses.)
> This file is the source of truth for the project's direction. Claude Code should read it before starting any phase.

## 1. What we're building

A web app where job seekers track their applications in one place and get **insight** from that data instead of guessing.

- Track every application through its lifecycle (applied → response → interview → offer / rejected / ghosted).
- See personal stats: which channels actually get responses, how long companies take to reply.
- Get CV ↔ job posting match analysis and tailoring suggestions (deterministic keywords first, LLM where interpretation is needed).
- Benefit from a small community: anonymized, aggregated signals such as "many members applied to this posting and nobody heard back" (ghost-listing signal) and per-company response behaviour.

**Audience:** a small invite-only community (realistically a handful of people, designed to handle a few dozen). The author is the first and most active user.

**UI language:** English.

## 2. Decisions already made

| Area | Decision |
|---|---|
| Backend | .NET 10 (LTS, supported until Nov 2028), ASP.NET Core Web API, EF Core |
| Architecture | **Modular monolith** — one deployable, clear module boundaries, modules talk through interfaces, not each other's tables |
| Database | PostgreSQL |
| Frontend | **Angular** (latest stable, standalone components, signals, typed reactive forms), **Angular CDK + Tailwind CSS** — no component library look; the visual design is custom (see section 8a) |
| Users | **Multi-user from day one**, invite-code registration, strict per-user data isolation |
| LLM | Provider abstraction; Groq as primary, Gemini as fallback; free tiers expected to be enough at this scale |
| Background work | In-process `BackgroundService` processing a Postgres-backed job table (no message broker needed at this scale) |
| Hosting | API on Fly.io, frontend on Netlify, managed Postgres |
| Job input | User pastes the posting URL and/or text. **No scraping** of LinkedIn or other sites |

## 3. Modules

```
src/
  Heardback.Api/                 # host: DI, endpoints, auth, middleware
  Heardback.Modules.Identity/    # users, invite codes, JWT + refresh tokens
  Heardback.Modules.Applications/# applications, status history, notes, reminders
  Heardback.Modules.Postings/    # shared job postings, dedup, structured fields
  Heardback.Modules.Matching/    # CV text, skills dictionary, match results
  Heardback.Modules.Insights/    # personal stats, community aggregates, ghost signals
  Heardback.Infrastructure.Llm/  # ILlmClient, Groq/Gemini clients, quota, cache
  Heardback.Shared/              # common abstractions (clock, current user, results)
tests/
  Heardback.UnitTests/
  Heardback.IntegrationTests/    # xUnit + Testcontainers (real Postgres)
web/                            # Angular app
```

Each module owns its own EF Core `DbContext` schema (Postgres schema per module) so boundaries stay honest.

## 4. Core data model (first sketch)

- **User** — id, email, display name, created at.
- **InviteCode** — code, created by, used by, expires at.
- **JobPosting** (shared across users) — id, normalized URL, company, title, location, work mode, seniority, required skills, raw text, content hash, first seen at, last seen at, times re-added.
- **Application** (private to one user) — id, user id, job posting id, channel (LinkedIn Easy Apply, company site, referral, direct message, Kariyer.net, other), applied at, current status, notes.
- **ApplicationEvent** — application id, from status, to status, occurred at (full timeline, never overwrite history).
- **Cv** — user id, extracted text (PII stripped), extracted skills, uploaded at. Original file is **not** stored.
- **MatchResult** — application id, matched skills, missing skills, score, LLM suggestions, model used, created at.
- **AnalysisJob** — type, payload, status, attempts, next run at, error.

Postings are deduplicated by normalized URL, then by a hash of company + title + text. Analysis of a posting is done once and shared.

## 5. LLM usage rules

- Everything that can be done deterministically is done deterministically (skills dictionary matching, stats, ghost heuristics).
- LLM is used for: turning messy posting text into structured JSON, and writing CV tailoring / follow-up suggestions.
- All LLM output is requested as JSON and **validated against a schema**; invalid output is retried once, then marked failed.
- `ILlmClient` abstraction with Groq primary and Gemini fallback on rate-limit or server errors.
- Per-user daily quota on LLM-backed actions; posting analysis cached by content hash.
- **PII is stripped** (emails, phone numbers, URLs to personal profiles, addresses) before any CV text leaves the server.
- API keys only via environment variables / user secrets. Never committed.

## 6. Ghost-listing signal (v1, explainable)

A score built from visible, explainable signals; the UI shows *why*, never just a number:

- Posting age since first seen.
- Number of times the same posting was re-added / reposted.
- Community response rate for this posting and this company (only shown when at least 3 distinct users contributed, to protect anonymity).
- Average days to first response for the company.

It is presented as a hint ("likely inactive"), not a verdict.

## 7. Privacy baseline

- Users only ever see their own applications. Community data is aggregated and anonymized, with a minimum-group-size threshold.
- Account deletion removes all personal data; aggregates no longer include the user.
- Data export (JSON) for the user's own data.
- Short privacy page explaining what is stored, what is sent to LLM providers, and how to delete.

## 8. Phases

Each phase ends in a working, tested, committed state. Work is split into small commits (one logical change each).

### Phase 0 — Skeleton
- Solution + module projects, Angular app scaffold (with Tailwind and Angular CDK installed, but no real UI yet), docker-compose for local Postgres.
- Health endpoint, Swagger, basic logging.
- GitHub Actions: build + test backend, build + test frontend.
- README skeleton.

**Done when:** CI is green, `docker compose up` + API + Angular dev server run locally.

### Phase 1 — Identity & invites
- Register with invite code, login, JWT access + refresh tokens, logout.
- Current-user abstraction; every query in other modules filtered by user.
- Angular plumbing only: auth service, auth guard, HTTP interceptor (token attach + refresh). **Login/register pages wait for the design checkpoint.**
- Admin (the author) can generate invite codes.
- Auth hardening: rate limiting on login, register and refresh (per client IP); absolute session lifetime on top of the sliding refresh; refresh-token reuse revokes all of the user's sessions.

**Done when:** integration tests prove a user cannot read another user's data.

### Design checkpoint — before any real UI work
Nothing visual is built until this is done (see section 8a). Backend work in later phases may continue in parallel.

- Screens designed by the author in Claude Design (claude.ai) and handed off to Claude Code.
- `docs/design.md` written from the handoff bundle (tokens + component rules) and reviewed by the author.
- Tailwind theme configured from those tokens.
- Then: login/register pages from Phase 1 are built, followed by the UI parts of each later phase.

### Phase 2 — Applications
- Add application: paste URL and/or text, plus manual fields (company, title, channel, date).
- Posting dedup into shared `JobPosting`.
- Status changes recorded as `ApplicationEvent`s; notes.
- Angular: list view with filters + kanban board (CDK drag-drop), detail page with timeline.

**Done when:** adding an application takes under ~10 seconds of user effort; kanban moves persist.

### Phase 3 — CV & deterministic matching
- CV PDF upload → text extraction → PII stripping → stored text only.
- Skills dictionary (C#, .NET, Angular, React, Docker, SQL, etc., with aliases).
- Per-application matched / missing skills.
- Angular: CV page, match panel on the application detail page.

**Done when:** matching works with zero LLM calls and is covered by unit tests.

### Phase 4 — LLM features
- `AnalysisJob` table + `BackgroundService` worker with retries and backoff.
- Posting text → structured fields (company, title, seniority, skills, work mode).
- CV tailoring suggestions per application.
- Provider fallback, per-user quota, cache by posting hash.
- Angular: pending/processing/done states for analyses.

**Done when:** LLM failures never break the core flow; the app still works with LLM disabled.

### Phase 5 — Personal insights & reminders
- Dashboard: applications per week, response rate per channel, average time to first response, status funnel.
- "No response for N days" reminders with an optional LLM-drafted follow-up message.
- Angular: dashboard with charts.

**Done when:** the author's own real data produces useful numbers.

### Phase 6 — Community signals
- Anonymized aggregates per posting and per company with minimum group size.
- Ghost-listing signal with explanations.
- Angular: signal badges on postings, company page.

**Done when:** aggregates never reveal an individual user's data (tested).

### Phase 7 — Privacy & hardening
- Account deletion, data export, privacy page.
- Rate limiting on LLM endpoints, global error handling, structured logs. (Auth endpoint rate limiting was built in Phase 1.)

### Phase 8 — Deploy & showcase
- API on Fly.io, frontend on Netlify, managed Postgres, environment config.
- Client IP for rate limiting: set `ClientIp` for the real topology, verifying requests came through the proxy (e.g. Netlify signed proxy rewrites) before trusting its forwarded IP. The API refuses to start until this is configured (ADR-0002).
- Demo account with seeded sample data.
- README: what it is, architecture diagram, screenshots, how to run, design decisions.

## 8a. Design process

The UI is designed first, then implemented. The goal is a calm, dense, data-first tool UI (in the spirit of Linear or GitHub Projects), not a generic AI-generated landing-page look.

**Workflow**
0. At the design checkpoint, Claude Code stops, reminds the author, and writes a design brief (screens, data and actions per screen, states) for the author to paste into Claude Design.
1. The author designs the screens in **Claude Design on the claude.ai website** (not from the terminal).
2. When approved, the design is handed off with Claude Design's **Export → Handoff to Claude Code → Send to local coding agent**, and the copied command is run in the Claude Code session in this repo. Fallback if the handoff doesn't work: export the design as a folder/zip into `docs/design/handoff/`.
3. Claude Code reads the handoff bundle and **does not start implementing screens immediately**. First it writes `docs/design.md`, capturing everything code needs: color tokens (light + dark), typography scale, spacing scale, radii, shadows, icon set, and rules for core components (buttons, inputs, tables, cards, badges, dialogs, empty/loading/error states). The author reviews it.
4. Tailwind's theme is configured from those tokens; the handoff is implemented in Angular (the bundle may be in another stack; translate it, don't copy it), screen by screen, using theme values only.
5. Later design changes: edit in Claude Design, hand off again, update `docs/design.md` if new patterns appear.

**Screens expected (will evolve)**
- Login / register (with invite code)
- Applications: list view and kanban board
- Application detail: posting info, status timeline, notes, match panel
- Add application flow
- Dashboard (personal insights)
- CV page
- Company / posting signals (community)
- Settings: account, data export, delete account, privacy

The list is not final. Screens added later go through the same flow: design first, update `docs/design.md` if new patterns appear, then build.

## 9. Testing strategy

- **Backend:** xUnit unit tests for domain logic (status transitions, matching, ghost score); integration tests with Testcontainers against real Postgres, especially user isolation and aggregates.
- **Frontend:** unit tests with the runner the current Angular CLI generates by default; component tests for key forms and the kanban.
- **E2E:** Playwright for the core flow: register → add application → move status → see dashboard update.
- LLM calls are behind interfaces and faked in tests.

## 10. Working agreement for Claude Code

- Follow the engineering rules in `CLAUDE.md` for every change.
- Work **one phase at a time**. At the start of a phase, summarize the steps and wait for confirmation.
- Keep commits small and meaningful (one logical change per commit, conventional commit messages).
- Run the relevant tests before every commit; never leave the build red.
- If something in this plan turns out to be wrong or a better option appears, **say so and propose the fix in the current step** rather than leaving it for later.
- Ask before adding a new dependency, changing the architecture, or deviating from the decisions in section 2.
- Never commit secrets.
- **Protections ship with the feature.** Security and correctness protections are built in the same phase as the feature they protect, never deferred to a later phase. Any trade-off that leaves a known gap needs the author's explicit approval first (see CLAUDE.md).
- **Independent review before a phase is done.** Before declaring a phase done, run an independent review with a fresh subagent that has not seen the work, fix its findings in the same phase, and only then ask the author to open the PR (see CLAUDE.md).

## 11. Open questions (decide when we get there)

- Managed Postgres provider (Fly Postgres vs. Neon vs. Supabase).
- Whether a browser extension for one-click adding is worth a later phase.
