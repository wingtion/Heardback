# ADR-0016: Phase 7 backend opened without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-22

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 7 is
split into two pull requests: this backend PR (account deletion, data export, account recovery with reset
codes and the owner command, ADR-0015) and a later UI PR. This PR touches deletion of personal data across
every module, a new anonymous endpoint (password reset), admin-issued codes and session revocation.

What was checked instead:
- From a clean clone: every commit builds, passes the format check and the 377 unit tests; the last commit
  passes all 754 .NET tests (unit and integration against real PostgreSQL) and the web lint, tests and build.
  They cover deletion leaving no row with the member's id in any module table (the scan finds new tables
  by itself), another member's data staying untouched, the last admin not being able to delete themselves,
  the posting delete-versus-link race (the test fails without the advisory lock), AI jobs saving nothing
  once their sources are gone, the export containing only the member's own data, reset codes (hashed,
  24 hours, single use, replaced by a new one, admins cannot issue them for admins), every session ending
  after a reset or password change, and the rate limits.
- An audit of all log calls: no interpolated messages and no PII in templates.

## Decision

The author decided on 2026-11-22 to open the Phase 7 backend PR **without** an independent review.

## Consequences

- No reviewer who had not seen the work looked at the deletion paths, the reset flow or the owner command.
  Gaps there may be found later than they otherwise would.
- The approved trade-offs in ADR-0015 (admin takeover of member accounts, the millisecond window for AI
  jobs) are unchanged.
- The rule in CLAUDE.md is unchanged: the Phase 7 UI PR and the end of the phase still get the question.
