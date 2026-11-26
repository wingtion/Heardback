# ADR-0020: Phase 8 closed without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-26

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. Phase 8 (Deploy
& showcase) was delivered in four pull requests: deployment with the Netlify signed-proxy client IP check
(#27, ADR-0002), "Try the demo" (#28, ADR-0018, opened without a review by ADR-0019), a ReadyToRun image and
quieter logs (#29), and this README showcase.

What was checked instead:
- Every commit of every Phase 8 pull request was built and tested from a clean clone (format, zero warnings,
  all .NET tests; web lint, format, tests and build where the web app changed), and CI passed on `main` after
  each merge.
- The client IP check was verified on the live site after the first deploy: forged client IP headers did not
  reset the sign-in limit, and a phone on mobile data had its own budget.
- The demo was tried on the live site after its deploy, and the README screenshots were taken from it.

## Decision

The author decided on 2026-11-26 to close Phase 8 **without** an independent review.

## Consequences

- No reviewer who had not seen the work looked at the production configuration, the signed-proxy check or the
  demo as a whole. Gaps there may be found later than they otherwise would.
- The approved trade-offs recorded in ADR-0002 (token replay until expiry) and ADR-0018 stand unchanged.
