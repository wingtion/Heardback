# ADR-0014: Phase 6 closed without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-20

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 6 adds
community signals (ADR-0013): company and role keys on applications, `ICommunityOutcomes` as the single read
across users, the `/api/community` endpoints with the ghost-listing hint, and the UI for them (badges in the list
and board, the "Stats from other members" section in the application detail, the company page). The same branch
also holds an app-wide plain-language pass over the UI text and a new section order in the application detail,
both asked for by the author after testing a demo.

This is the first feature that reads other users' data, so a full review was recommended. The author chose to
skip it.

What was checked instead:
- 703 .NET tests (377 unit, 326 integration against real Postgres) and 330 web tests passing at the last commit,
  with web lint, format and production build, and a clean clone that builds with zero warnings. Integration tests
  prove the privacy rules: no numbers below three other members, the viewer never counted, fresh applications not
  counted, no other user's ids or text in any response, deletions dropping out at once, and the company page
  refusing companies the viewer does not track with the same answer as unknown ones. Unit tests cover the name
  keys, the grouping and every ghost-listing rule.
- The generated SQL of the cross-user query was read (one query, filtered by the viewer's own groups).
- The author tested a seeded demo (four accounts, every state: High, Medium, Low, company only, not enough data)
  and their own data with AI on, which led to the copy and order changes.

## Decision

The author decided on 2026-11-20 to close Phase 6 **without** an independent review.

## Consequences

- No reviewer who had not seen the work checked the cross-user read, the minimum-size rule or the ghost-listing
  points. A mistake there could expose more about other members than ADR-0013 allows, and may be found later than
  it otherwise would.
- The accepted gap from ADR-0013 (exact numbers can be differenced over time) still stands.
- The rule in CLAUDE.md is unchanged: later phases end with an independent review unless the author approves a
  skip again.
