# ADR-0007: Phase 3 closed without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-18

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 3 (CV and
deterministic matching) adds a CV PDF upload, text extraction with a decompression budget, removal of
personal details, a skills dictionary, per-application matching, the CV page and the match panel. Before
asking, the author was told that this phase handles PDF files and personal data, and that a review was
recommended.

What was checked instead:
- 392 .NET tests (unit and integration against real PostgreSQL) and 258 web tests. They cover the upload
  limits, the decompression budget and the race between two first uploads (both shown to fail without
  their protection), user isolation of the CV and of matches, rate limits, and every UI state.
- The author tested the whole flow by hand with their real CV against the real API. That test found that
  skills a project only mentions were counted, which led to "Not my skill" (ADR-0006 and docs/design.md §15).
- The UI was composed from the existing design system and approved by the author from screenshots
  (docs/design.md §15).

## Decision

The author decided on 2026-11-18 to close Phase 3 **without** an independent review.

## Consequences

- No reviewer who had not seen the work looked at the PDF handling, the personal-detail removal or the
  new endpoints. Gaps there may be found later than they otherwise would.
- The parser risk that remains is the one already approved in ADR-0006.
- The rule in CLAUDE.md is unchanged: later phases still end with an independent review unless the author
  approves a skip again.
