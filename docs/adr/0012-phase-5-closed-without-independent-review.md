# ADR-0012: Phase 5 closed without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-19

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 5 adds the
personal insights backend (ADR-0011, merged as its own PR) and the Overview page: follow-ups, response rate,
time to first response, applications per week, response rate by channel and how far applications got, plus a
"Job posting" section in the application detail and clearer AI copy. Before asking, the author was offered a
full review, a review focused on the definitions and user isolation, or none; the lower risk of this phase was
explained (no new LLM calls, the only new write reuses "Add update").

What was checked instead:
- 645 .NET tests and 307 web tests, passing from a clean clone (every commit built and passed its unit tests;
  integration tests, web lint, format, tests and production build at the last commit). Unit tests cover every
  definition (week boundaries, same-day answers, medians, the funnel); integration tests cover real timelines,
  isolation between users, paging and anonymous access; web tests cover every state of the Overview page.
- An ASCII layout approved by the author before coding, screenshots of the built page on desktop and phone,
  light and dark, and the author's own test against the real API.

## Decision

The author decided on 2026-11-19 to close Phase 5 **without** an independent review, and not to mention it in
the PR description.

## Consequences

- No reviewer who had not seen the work checked the insights definitions, the history contract or the Overview
  page. Mistakes there may be found later than they otherwise would.
- The rule in CLAUDE.md is unchanged: later phases end with an independent review unless the author approves a
  skip again.
