# ADR-0017: Phase 7 closed without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-25

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. The Phase 7
backend was opened without one (ADR-0016). This second PR is the UI: Settings (change password, theme, data
export, delete account), the public privacy page, resetting a forgotten password with an admin's code, and
the admin Members page with reset codes. It changes no backend code.

What was checked instead:
- From a clean clone: every commit passes the web lint, format check, the tests (388 at the end) and the
  production build.
- The tests cover the wrong-password and last-admin answers when deleting, the session staying in this browser
  after a password change, the invalid reset code, the rate-limit banners, the reset-code dialog when the
  member was deleted or made an admin meanwhile, and losing admin rights while the Members page is open.
- The author tried every flow against the real API on 2026-11-25.
- A final read of the whole diff by Claude Code, which had written it. It found one bug (the member list did
  not update when the member had become an admin), fixed with a regression test.

## Decision

The author decided on 2026-11-25 to close Phase 7 **without** an independent review.

## Consequences

- No reviewer who had not seen the work looked at Phase 7, backend or UI. Gaps in the deletion, reset or
  password change flows may be found later than they otherwise would.
- The approved trade-offs in ADR-0015 are unchanged.
- The rule in CLAUDE.md is unchanged: the next phase still gets the question.
