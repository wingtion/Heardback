# ADR-0019: Phase 8 demo PR opened without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-26

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 8 is split
into three pull requests: deployment (merged as #27), this demo PR ("Try the demo", ADR-0018) and a README
showcase PR. This PR adds an anonymous endpoint that creates accounts, a per-IP limit and a global cap,
restrictions on what demo visitors can do, a change to how the current user is resolved in background scopes,
and a rule that keeps members' community data away from demo visitors.

What was checked instead:
- From a clean clone: every commit builds with zero warnings, passes the format check and all .NET tests (785
  on the last commits: unit and integration against real PostgreSQL); the last commit passes the web lint,
  format check, 405 tests and build.
- Integration tests cover: the demo off by default (404), sample data on start, the 20-visitor cap (429 with
  Retry-After) including concurrent requests for the last places, the per-IP limit, demo visitors refused on
  admin pages, invite codes, password change and password sign-in, demo addresses refused at registration,
  demo accounts missing from the Members list, the session ending with the demo, the cleanup removing the
  visitor and their rows while the neighbors stay, demo visitors counting only neighbors, and members never
  counting demo accounts.
- A manual run in the browser against a local API with the demo on (desktop light, phone dark).

## Decision

The author decided on 2026-11-26 to open the Phase 8 demo PR **without** an independent review.

## Consequences

- No reviewer who had not seen the work looked at the anonymous sign-up, the limits or the community audience
  rule. Gaps there may be found later than they otherwise would.
- The rule in CLAUDE.md is unchanged: the end of Phase 8 still gets the question.
