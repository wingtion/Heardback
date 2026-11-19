# ADR-0010: Phase 4 closed without an independent review

- **Status:** Accepted (approved trade-off)
- **Date:** 2026-11-19

## Context

CLAUDE.md asks for an independent review by a fresh subagent before a phase is declared done. The Phase 4
backend PR was already opened without one (ADR-0009). The second PR adds the UI for the AI features (Job
summary, Skills match grouping, How to improve your CV with consent in place, the CV page consent block) and
an admin-only button for sample applications. Before asking, the author was offered a full review of Phase 4,
a review focused on the riskiest parts of this PR (the consent flow, the admin check on the sample endpoint,
polling that stops), or none, with a recommendation for the focused review.

What was checked instead:
- 625 .NET tests and 291 web tests, passing from a clean clone (every commit built and passed its unit tests;
  integration tests, web lint, format, tests and production build at the last commit). They cover the consent
  flow and its withdrawal, every AI state, polling while a job runs, the 429 limit messages, and the sample
  endpoint for admins, members (403), anonymous callers (401) and with the setting off (404).
- Screenshots of every state on desktop and phone, light and dark, against a mocked API.
- The author's own test against the real API and real AI providers, which led to grouping the sections under
  "About this job" and "Your CV for this job" with an "AI" tag.

## Decision

The author decided on 2026-11-19 to close Phase 4 **without** an independent review, and not to mention it in
the PR description.

## Consequences

- No reviewer who had not seen the work looked at Phase 4 as a whole: the LLM client, job queue, quotas,
  background user, CV text handling or their UI. Gaps there may be found later than they otherwise would.
- The rule in CLAUDE.md is unchanged: later phases end with an independent review unless the author approves a
  skip again.
