# ADR-0005: Phase 2 closed without a phase-level independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-18

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done: security,
correctness and test coverage, with findings fixed in the same phase. Phase 2 (applications) shipped in
three pull requests:

- **#11, backend** (postings dedup, applications API, events, board, rate limiting): reviewed
  independently before merging. Every finding was fixed with regression tests (see ADR-0004 and the
  PR history).
- **#12, list, detail, add, delete UI**, and **#13, board, Add update, Edit UI**: no independent
  review. The author tested both by hand against the real API (all manual steps passed), and the
  changes are covered by unit tests (227 web tests at the end of the phase). Every commit was checked
  from a clean clone.

## Decision

The author decided on 2026-11-18 to close Phase 2 **without** a phase-level independent review of the UI
work, and to continue with Phase 3.

## Consequences

- The UI pull requests #12 and #13 had no second look from a reviewer who had not seen the work. A gap
  in, for example, error handling or accessibility may be found later than it otherwise would.
- The backend, where the security protections live (authorization, user isolation, validation, rate
  limits, concurrency), was reviewed independently, so the known risk is limited to the client.
- The rule in CLAUDE.md is unchanged: later phases still end with an independent review unless the
  author approves a skip again.
